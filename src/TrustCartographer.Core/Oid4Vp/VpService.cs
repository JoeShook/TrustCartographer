using System.Text.Json;
using System.Text.Json.Nodes;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Core.Oid4Vp;

/// <summary>
/// Builds the Holder's signed Verifiable Presentation (a JWT wrapping the JWT-VC,
/// bound to the Verifier's audience + nonce) and verifies a presentation
/// end-to-end. The verification is the heart of the trust triangle: it proves
/// (1) the presenter controls the holder key, (2) the credential was signed by
/// its issuer — resolved purely from the issuer's <c>did:key</c>, no pre-shared
/// secret — and (3) the presenter is the credential subject.
/// </summary>
public sealed class VpService
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    private readonly CryptoService _crypto;
    private readonly DidKeyService _didKey;
    private readonly VcService _vc;

    public VpService(CryptoService crypto, DidKeyService didKey, VcService vc)
    {
        _crypto = crypto;
        _didKey = didKey;
        _vc = vc;
    }

    /// <summary>
    /// Build a holder-signed VP JWT (typ=vp+jwt) embedding <paramref name="vcJwt"/>,
    /// bound to the Verifier's <paramref name="audience"/> (client_id) and
    /// <paramref name="nonce"/>. The header carries the holder's public key as a
    /// <c>jwk</c> so the Verifier needs nothing pre-shared.
    /// </summary>
    public string BuildHolderVpJwt(
        byte[] holderPrivateKey,
        byte[] holderPublicKey,
        string holderDid,
        string audience,
        string nonce,
        string vcJwt,
        DateTimeOffset? issuedAt = null)
    {
        var iat = (issuedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        var header = new JsonObject
        {
            ["alg"] = "EdDSA",
            ["typ"] = Oid4VpConstants.VpJwtType,
            ["jwk"] = JsonSerializer.SerializeToNode(Jwk.FromEd25519PublicKey(holderPublicKey))
        };

        var vp = new JsonObject
        {
            ["@context"] = new JsonArray { Oid4VpConstants.VpContext },
            ["type"] = new JsonArray { Oid4VpConstants.VerifiablePresentationType },
            ["holder"] = holderDid,
            ["verifiableCredential"] = new JsonArray { vcJwt }
        };

        var payload = new JsonObject
        {
            ["iss"] = holderDid,
            ["aud"] = audience,
            ["nonce"] = nonce,
            ["iat"] = iat,
            ["vp"] = vp
        };

        var headerSeg = Base64Url.Encode(header.ToJsonString());
        var payloadSeg = Base64Url.Encode(payload.ToJsonString());
        var signingInput = $"{headerSeg}.{payloadSeg}";

        var sig = _crypto.SignEd25519(holderPrivateKey, signingInput);
        return $"{signingInput}.{Base64Url.Encode(sig.Signature!)}";
    }

    /// <summary>
    /// Verify a presented VP JWT against the expected audience/nonce and required
    /// credential type. Each sub-check is reported so the UI can show exactly what
    /// passed or failed. NOTE: this confirms the issuer *signed* the credential —
    /// it does NOT decide whether the issuer is *trusted* (any did:key is accepted).
    /// That gap is what trust frameworks (e.g. UDAP communities) exist to close.
    /// </summary>
    public PresentationVerifyResult VerifyPresentation(
        string vpJwt,
        string expectedAudience,
        string expectedNonce,
        string requiredCredentialType)
    {
        var parts = vpJwt.Split('.');
        if (parts.Length != 3)
            return PresentationVerifyResult.Fail("VP JWT must have 3 segments.");

        JsonNode? header, payload;
        try
        {
            header = JsonNode.Parse(Base64Url.DecodeToString(parts[0]));
            payload = JsonNode.Parse(Base64Url.DecodeToString(parts[1]));
        }
        catch (Exception ex)
        {
            return PresentationVerifyResult.Fail($"Could not parse VP segments: {ex.Message}");
        }
        if (header is null || payload is null)
            return PresentationVerifyResult.Fail("VP header or payload was null.");

        var headerJson = header.ToJsonString(Pretty);
        var payloadJson = payload.ToJsonString(Pretty);

        if ((string?)header["typ"] != Oid4VpConstants.VpJwtType)
            return PresentationVerifyResult.Fail($"typ must be {Oid4VpConstants.VpJwtType}.", headerJson, payloadJson);
        if ((string?)header["alg"] != "EdDSA")
            return PresentationVerifyResult.Fail("Only alg=EdDSA is supported.", headerJson, payloadJson);

        // 1. Holder proof — pull the holder public key from the header jwk, verify the VP signature.
        var jwkNode = header["jwk"];
        if (jwkNode is null)
            return PresentationVerifyResult.Fail("VP header is missing the holder jwk.", headerJson, payloadJson);
        var jwk = JsonSerializer.Deserialize<Jwk>(jwkNode.ToJsonString());
        if (jwk is null || jwk.Crv != "Ed25519" || string.IsNullOrEmpty(jwk.X))
            return PresentationVerifyResult.Fail("Holder jwk must be an Ed25519 OKP key with x.", headerJson, payloadJson);

        var holderPublicKey = Base64Url.Decode(jwk.X!);
        if (holderPublicKey.Length != 32)
            return PresentationVerifyResult.Fail("Decoded holder x is not 32 bytes.", headerJson, payloadJson);

        var signingInput = $"{parts[0]}.{parts[1]}";
        var holderProofValid = _crypto.VerifyEd25519(holderPublicKey, signingInput, Base64Url.Decode(parts[2]));
        if (!holderProofValid)
            return PresentationVerifyResult.Fail("Holder proof signature did not verify.", headerJson, payloadJson);

        var holderDid = _didKey.EncodeEd25519(holderPublicKey);

        // 2. Audience + nonce binding (anti-replay, intended-recipient).
        if ((string?)payload["aud"] != expectedAudience)
            return PresentationVerifyResult.Fail($"aud mismatch (expected {expectedAudience}).", headerJson, payloadJson, holderDid);
        if ((string?)payload["nonce"] != expectedNonce)
            return PresentationVerifyResult.Fail("nonce mismatch (replay or stale presentation).", headerJson, payloadJson, holderDid);

        // 3. Extract the embedded credential.
        var vcJwt = payload["vp"]?["verifiableCredential"]?.AsArray().FirstOrDefault()?.GetValue<string>();
        if (string.IsNullOrEmpty(vcJwt))
            return PresentationVerifyResult.Fail("VP carried no verifiableCredential.", headerJson, payloadJson, holderDid);

        // 4. Resolve the issuer DID from the credential and verify its signature.
        string issuerDid;
        string subjectDid;
        string credentialType;
        try
        {
            var vcParts = vcJwt.Split('.');
            using var vcPayloadDoc = JsonDocument.Parse(Base64Url.DecodeToString(vcParts[1]));
            var root = vcPayloadDoc.RootElement;
            issuerDid = root.GetProperty("iss").GetString() ?? "";
            subjectDid = root.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" : "";
            credentialType = ExtractCredentialType(root);
        }
        catch (Exception ex)
        {
            return PresentationVerifyResult.Fail($"Could not read embedded credential: {ex.Message}", headerJson, payloadJson, holderDid);
        }

        byte[] issuerPublicKey;
        try
        {
            issuerPublicKey = _didKey.DecodeEd25519(issuerDid);
        }
        catch (Exception ex)
        {
            return PresentationVerifyResult.Fail($"Could not resolve issuer DID {issuerDid}: {ex.Message}", headerJson, payloadJson, holderDid);
        }

        var vcResult = _vc.VerifyJwtVc(vcJwt, issuerPublicKey);
        var notExpired = vcResult.Expired != true;
        if (!vcResult.Valid)
            return PresentationVerifyResult.Fail(
                $"Credential failed verification: {vcResult.Message}", headerJson, payloadJson, holderDid)
                with
            { IssuerDid = issuerDid, CredentialType = credentialType, VcJwt = vcJwt, VcPayloadJson = vcResult.DecodedPayloadJson,
              HolderProofValid = true, IssuerSignatureValid = vcResult.SignatureValid, NotExpired = notExpired };

        // 5. Type matches what the Verifier asked for.
        var typeMatches = string.Equals(credentialType, requiredCredentialType, StringComparison.Ordinal);
        if (!typeMatches)
            return PresentationVerifyResult.Fail(
                $"Credential type {credentialType} does not satisfy required {requiredCredentialType}.",
                headerJson, payloadJson, holderDid)
                with
            { IssuerDid = issuerDid, CredentialType = credentialType, VcJwt = vcJwt, VcPayloadJson = vcResult.DecodedPayloadJson,
              HolderProofValid = true, IssuerSignatureValid = true, NotExpired = notExpired };

        // 6. Holder binding — the presenter must be the credential subject.
        var holderBound = string.Equals(holderDid, subjectDid, StringComparison.Ordinal);
        if (!holderBound)
            return PresentationVerifyResult.Fail(
                "Holder binding failed: presenter is not the credential subject.",
                headerJson, payloadJson, holderDid)
                with
            { IssuerDid = issuerDid, CredentialType = credentialType, VcJwt = vcJwt, VcPayloadJson = vcResult.DecodedPayloadJson,
              HolderProofValid = true, IssuerSignatureValid = true, NotExpired = notExpired };

        return new PresentationVerifyResult(
            Valid: true,
            Message: "Presentation verified.",
            HolderProofValid: true,
            IssuerSignatureValid: true,
            NotExpired: notExpired,
            TypeMatches: true,
            HolderBound: true,
            HolderDid: holderDid,
            IssuerDid: issuerDid,
            CredentialType: credentialType,
            VpHeaderJson: headerJson,
            VpPayloadJson: payloadJson,
            VcJwt: vcJwt,
            VcPayloadJson: vcResult.DecodedPayloadJson);
    }

    private static string ExtractCredentialType(JsonElement vcPayloadRoot)
    {
        // Prefer the most specific type from vc.type (last non-"VerifiableCredential" entry).
        if (vcPayloadRoot.TryGetProperty("vc", out var vc) &&
            vc.TryGetProperty("type", out var types) &&
            types.ValueKind == JsonValueKind.Array)
        {
            string? specific = null;
            foreach (var t in types.EnumerateArray())
            {
                var s = t.GetString();
                if (!string.IsNullOrEmpty(s) && s != "VerifiableCredential") specific = s;
            }
            if (specific is not null) return specific;
        }
        return "VerifiableCredential";
    }
}

public record PresentationVerifyResult(
    bool Valid,
    string Message,
    bool HolderProofValid,
    bool IssuerSignatureValid,
    bool NotExpired,
    bool TypeMatches,
    bool HolderBound,
    string? HolderDid,
    string? IssuerDid,
    string? CredentialType,
    string? VpHeaderJson,
    string? VpPayloadJson,
    string? VcJwt,
    string? VcPayloadJson)
{
    public static PresentationVerifyResult Fail(
        string msg, string? vpHeaderJson = null, string? vpPayloadJson = null, string? holderDid = null) =>
        new(false, msg, false, false, false, false, false,
            holderDid, null, null, vpHeaderJson, vpPayloadJson, null, null);
}
