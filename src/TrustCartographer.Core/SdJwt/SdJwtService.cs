using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Core.SdJwt;

/// <summary>
/// Selective-disclosure JWTs (SD-JWT / SD-JWT VC). Built by hand so every step is
/// inspectable: the issuer commits to salted digests of each claim (<c>_sd</c>);
/// the holder later reveals only the disclosures it chooses, plus a Key Binding JWT
/// (signed with the holder key the issuer bound via <c>cnf</c>) that ties the
/// presentation to a specific verifier (<c>aud</c>) and challenge (<c>nonce</c>).
///
/// This is an SDK-grade primitive: pure, no UI/transport dependencies.
/// </summary>
public sealed class SdJwtService
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly CryptoService _crypto;
    private readonly DidKeyService _didKey;

    public SdJwtService(CryptoService crypto, DidKeyService didKey)
    {
        _crypto = crypto;
        _didKey = didKey;
    }

    // ───────────────────────── Issue ─────────────────────────

    /// <summary>
    /// Issue an SD-JWT VC. Every claim in <paramref name="selectivelyDisclosableClaims"/>
    /// becomes a salted disclosure whose digest is committed in <c>_sd</c>; the holder
    /// public key (<paramref name="holderCnfJwk"/>) is bound via <c>cnf</c> for later
    /// key binding. Claims in <paramref name="alwaysVisibleClaims"/> appear in clear.
    /// </summary>
    public IssuedSdJwt IssueSdJwtVc(
        string issuerDid,
        string issuerVerificationMethodId,
        byte[] issuerPrivateKey,
        string vct,
        IReadOnlyDictionary<string, object?> selectivelyDisclosableClaims,
        Jwk holderCnfJwk,
        IReadOnlyDictionary<string, object?>? alwaysVisibleClaims = null,
        TimeSpan? validity = null)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var disclosures = new List<Disclosure>();
        var digests = new JsonArray();
        foreach (var (name, value) in selectivelyDisclosableClaims)
        {
            var salt = Base64Url.Encode(RandomBytes(16));
            var valueNode = ToNode(value);
            var discArray = new JsonArray(JsonValue.Create(salt), JsonValue.Create(name), valueNode?.DeepClone());
            var encoded = Base64Url.Encode(discArray.ToJsonString(Compact));
            var digest = Base64Url.Encode(_crypto.ComputeSha256(encoded));

            disclosures.Add(new Disclosure(name, valueNode?.ToJsonString(Compact) ?? "null", salt, encoded, digest));
            digests.Add(JsonValue.Create(digest));
        }

        var payload = new JsonObject
        {
            ["iss"] = issuerDid,
            ["iat"] = iat,
            ["vct"] = vct,
            ["_sd_alg"] = SdJwtConstants.SdAlg,
            ["_sd"] = digests,
            ["cnf"] = new JsonObject { ["jwk"] = JsonSerializer.SerializeToNode(holderCnfJwk) }
        };
        if (validity is { } v)
            payload["exp"] = iat + (long)v.TotalSeconds;
        if (alwaysVisibleClaims is not null)
            foreach (var (k, val) in alwaysVisibleClaims)
                payload[k] = ToNode(val);

        var header = new JsonObject
        {
            ["alg"] = "EdDSA",
            ["kid"] = issuerVerificationMethodId,
            ["typ"] = SdJwtConstants.SdJwtVcType
        };

        var headerSeg = Base64Url.Encode(header.ToJsonString(Compact));
        var payloadSeg = Base64Url.Encode(payload.ToJsonString(Compact));
        var signingInput = $"{headerSeg}.{payloadSeg}";
        var sig = _crypto.SignEd25519(issuerPrivateKey, signingInput);
        var issuerJwt = $"{signingInput}.{Base64Url.Encode(sig.Signature!)}";

        var combined = issuerJwt + SdJwtConstants.Delimiter +
                       string.Concat(disclosures.Select(d => d.Encoded + SdJwtConstants.Delimiter));

        return new IssuedSdJwt(
            issuerJwt,
            disclosures,
            combined,
            header.ToJsonString(Pretty),
            payload.ToJsonString(Pretty));
    }

    /// <summary>
    /// Parse a stored combined SD-JWT (<c>jwt~d1~d2~…~</c>) back into its issuer JWT and
    /// decoded disclosures so a holder can choose which to present. A trailing Key Binding
    /// JWT, if present, is ignored.
    /// </summary>
    public (string IssuerJwt, IReadOnlyList<Disclosure> Disclosures) ParseIssued(string combined)
    {
        var segs = combined.Split(SdJwtConstants.Delimiter);
        var issuerJwt = segs[0];
        var list = new List<Disclosure>();
        for (var i = 1; i < segs.Length; i++)
        {
            var seg = segs[i];
            if (seg.Length == 0) continue;
            try
            {
                if (JsonNode.Parse(Base64Url.DecodeToString(seg)) is JsonArray a && a.Count == 3)
                {
                    var salt = a[0]!.GetValue<string>();
                    var name = a[1]!.GetValue<string>();
                    var digest = Base64Url.Encode(_crypto.ComputeSha256(seg));
                    list.Add(new Disclosure(name, a[2]?.ToJsonString(Compact) ?? "null", salt, seg, digest));
                }
            }
            catch { /* not a disclosure (e.g. a KB-JWT) — skip */ }
        }
        return (issuerJwt, list);
    }

    // ───────────────────────── Present ─────────────────────────

    /// <summary>
    /// Build a presentation that reveals only <paramref name="selected"/> disclosures,
    /// appending a Key Binding JWT signed by the holder key and bound to the verifier's
    /// <paramref name="audience"/> + <paramref name="nonce"/>. Result form:
    /// <c>issuerJwt~sel1~sel2~&lt;kb-jwt&gt;</c>.
    /// </summary>
    public string Present(
        string issuerJwt,
        IEnumerable<Disclosure> selected,
        byte[] holderPrivateKey,
        string audience,
        string nonce)
    {
        var sdForm = issuerJwt + SdJwtConstants.Delimiter +
                     string.Concat(selected.Select(d => d.Encoded + SdJwtConstants.Delimiter));

        var sdHash = Base64Url.Encode(_crypto.ComputeSha256(sdForm));

        var kbHeader = new JsonObject { ["alg"] = "EdDSA", ["typ"] = SdJwtConstants.KbJwtType };
        var kbPayload = new JsonObject
        {
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["aud"] = audience,
            ["nonce"] = nonce,
            ["sd_hash"] = sdHash
        };
        var kbSigningInput = $"{Base64Url.Encode(kbHeader.ToJsonString(Compact))}.{Base64Url.Encode(kbPayload.ToJsonString(Compact))}";
        var kbSig = _crypto.SignEd25519(holderPrivateKey, kbSigningInput);
        var kbJwt = $"{kbSigningInput}.{Base64Url.Encode(kbSig.Signature!)}";

        return sdForm + kbJwt;
    }

    // ───────────────────────── Verify ─────────────────────────

    /// <summary>
    /// Verify a presented SD-JWT: issuer signature, that every revealed disclosure's
    /// digest was committed in <c>_sd</c>, the Key Binding JWT (holder key from
    /// <c>cnf</c>, audience, nonce, sd_hash), and that any
    /// <paramref name="requiredClaims"/> were actually disclosed.
    /// </summary>
    public SdJwtVerifyResult Verify(
        string presentation,
        byte[] issuerPublicKey,
        string expectedAudience,
        string expectedNonce,
        IEnumerable<string>? requiredClaims = null)
    {
        if (string.IsNullOrWhiteSpace(presentation) || !presentation.Contains(SdJwtConstants.Delimiter))
            return SdJwtVerifyResult.Fail("Not an SD-JWT (no '~' segments).");

        var segs = presentation.Split(SdJwtConstants.Delimiter);
        var issuerJwt = segs[0];
        var lastSeg = segs[^1];
        var hasKb = lastSeg.Length > 0;
        var kbJwt = hasKb ? lastSeg : null;
        var disclosureSegs = segs[1..^1]; // between issuer JWT and the KB/empty tail

        // 1. Issuer signature.
        var issuerParts = issuerJwt.Split('.');
        if (issuerParts.Length != 3)
            return SdJwtVerifyResult.Fail("Issuer JWT must have 3 segments.");
        JsonNode? issuerHeader, issuerPayload;
        try
        {
            issuerHeader = JsonNode.Parse(Base64Url.DecodeToString(issuerParts[0]));
            issuerPayload = JsonNode.Parse(Base64Url.DecodeToString(issuerParts[1]));
        }
        catch (Exception ex)
        {
            return SdJwtVerifyResult.Fail($"Could not parse issuer JWT: {ex.Message}");
        }
        if (issuerHeader is null || issuerPayload is null)
            return SdJwtVerifyResult.Fail("Issuer JWT header/payload was null.");

        var issuerHeaderJson = issuerHeader.ToJsonString(Pretty);
        var issuerPayloadJson = issuerPayload.ToJsonString(Pretty);
        var issuerSigValid = _crypto.VerifyEd25519(
            issuerPublicKey, $"{issuerParts[0]}.{issuerParts[1]}", Base64Url.Decode(issuerParts[2]));

        var issuerDid = (string?)issuerPayload["iss"];
        var vct = (string?)issuerPayload["vct"];

        // Committed digests.
        var committed = new HashSet<string>();
        if (issuerPayload["_sd"] is JsonArray sdArr)
            foreach (var n in sdArr)
                if (n is not null) committed.Add(n.GetValue<string>());

        // 2. Disclosures — each revealed digest must be in _sd.
        var disclosed = new Dictionary<string, JsonNode?>();
        var disclosuresValid = true;
        foreach (var seg in disclosureSegs)
        {
            var digest = Base64Url.Encode(_crypto.ComputeSha256(seg));
            if (!committed.Contains(digest)) { disclosuresValid = false; continue; }
            try
            {
                if (JsonNode.Parse(Base64Url.DecodeToString(seg)) is JsonArray a && a.Count == 3)
                    disclosed[a[1]!.GetValue<string>()] = a[2]?.DeepClone();
                else
                    disclosuresValid = false;
            }
            catch { disclosuresValid = false; }
        }
        var withheld = Math.Max(0, committed.Count - disclosureSegs.Length);

        // 3. Holder key from cnf.jwk → KB-JWT verification.
        var holderJwk = issuerPayload["cnf"]?["jwk"];
        byte[]? holderPub = null;
        string? holderDid = null;
        if (holderJwk?["x"] is { } x)
        {
            try { holderPub = Base64Url.Decode(x.GetValue<string>()); holderDid = _didKey.EncodeEd25519(holderPub); }
            catch { /* leave null */ }
        }

        bool kbSigValid = false, audOk = false, nonceOk = false, sdHashOk = false;
        string? kbHeaderJson = null, kbPayloadJson = null;
        if (hasKb && holderPub is not null)
        {
            var kbParts = kbJwt!.Split('.');
            if (kbParts.Length == 3)
            {
                try
                {
                    var kbHeader = JsonNode.Parse(Base64Url.DecodeToString(kbParts[0]));
                    var kbPayload = JsonNode.Parse(Base64Url.DecodeToString(kbParts[1]));
                    kbHeaderJson = kbHeader?.ToJsonString(Pretty);
                    kbPayloadJson = kbPayload?.ToJsonString(Pretty);

                    kbSigValid = (string?)kbHeader?["typ"] == SdJwtConstants.KbJwtType
                                 && _crypto.VerifyEd25519(holderPub, $"{kbParts[0]}.{kbParts[1]}", Base64Url.Decode(kbParts[2]));
                    audOk = (string?)kbPayload?["aud"] == expectedAudience;
                    nonceOk = (string?)kbPayload?["nonce"] == expectedNonce;

                    var sdForm = presentation.Substring(0, presentation.Length - kbJwt!.Length);
                    var expectedSdHash = Base64Url.Encode(_crypto.ComputeSha256(sdForm));
                    sdHashOk = (string?)kbPayload?["sd_hash"] == expectedSdHash;
                }
                catch { /* leave checks false */ }
            }
        }

        // 4. Required claims must have been disclosed.
        var missing = requiredClaims?.Where(c => !disclosed.ContainsKey(c)).ToList() ?? new List<string>();

        var keyBindingValid = kbSigValid;
        var valid = issuerSigValid && disclosuresValid && keyBindingValid && audOk && nonceOk && sdHashOk && missing.Count == 0;

        var message = valid ? "Presentation verified."
            : !issuerSigValid ? "Issuer signature did not verify."
            : !disclosuresValid ? "A revealed disclosure was not committed in _sd (forged/tampered)."
            : !hasKb ? "No Key Binding JWT — holder did not prove possession."
            : !kbSigValid ? "Key Binding JWT signature/typ invalid."
            : !audOk ? $"Key binding aud mismatch (expected {expectedAudience})."
            : !nonceOk ? "Key binding nonce mismatch (replay or stale)."
            : !sdHashOk ? "sd_hash mismatch — presented disclosures don't match what was signed over."
            : missing.Count > 0 ? $"Required claim(s) not disclosed: {string.Join(", ", missing)}."
            : "Verification failed.";

        return new SdJwtVerifyResult(
            valid, message,
            issuerSigValid, disclosuresValid, keyBindingValid, audOk, nonceOk, sdHashOk,
            disclosed, withheld,
            holderDid, issuerDid, vct,
            issuerHeaderJson, issuerPayloadJson, kbHeaderJson, kbPayloadJson);
    }

    // ───────────────────────── helpers ─────────────────────────

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node.DeepClone(),
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        _ => JsonValue.Create(value.ToString())
    };
}
