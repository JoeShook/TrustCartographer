using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrustCartographer.Core.Services;

/// <summary>
/// Builds and verifies JWT-VC envelopes by hand so each step is visible.
/// Compact JWT format: base64url(header).base64url(payload).base64url(signature)
/// </summary>
public class VcService
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false,
        // Otherwise vc+jwt serializes as vc+jwt. Valid JSON, hostile to humans.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly CryptoService _crypto;

    public VcService(CryptoService crypto)
    {
        _crypto = crypto;
    }

    /// <summary>
    /// Issues a JWT-VC signed with Ed25519. Returns the compact JWT plus the pretty-printed
    /// header and payload (so the UI can show what was signed without re-decoding).
    /// </summary>
    public IssuedJwtVc IssueJwtVc(
        string issuerDid,
        string issuerVerificationMethodId,
        byte[] issuerPrivateKey,
        string subjectDid,
        string credentialType,
        IReadOnlyDictionary<string, object?> claims,
        TimeSpan? validity = null)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = validity.HasValue ? iat + (long)validity.Value.TotalSeconds : (long?)null;
        var credentialId = $"urn:uuid:{Guid.NewGuid()}";

        var credentialSubject = new JsonObject { ["id"] = subjectDid };
        foreach (var (k, v) in claims)
            credentialSubject[k] = ToJsonNode(v);

        var vc = new JsonObject
        {
            ["@context"] = new JsonArray { "https://www.w3.org/ns/credentials/v2" },
            ["type"] = new JsonArray { "VerifiableCredential", credentialType },
            ["id"] = credentialId,
            ["issuer"] = issuerDid,
            ["validFrom"] = DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime.ToString("O"),
            ["credentialSubject"] = credentialSubject
        };
        if (exp.HasValue)
            vc["validUntil"] = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime.ToString("O");

        var header = new JsonObject
        {
            ["alg"] = "EdDSA",
            ["kid"] = issuerVerificationMethodId,
            ["typ"] = "vc+jwt"
        };

        var payload = new JsonObject
        {
            ["iss"] = issuerDid,
            ["sub"] = subjectDid,
            ["jti"] = credentialId,
            ["iat"] = iat,
            ["nbf"] = iat,
            ["vc"] = vc
        };
        if (exp.HasValue)
            payload["exp"] = exp.Value;

        var headerCompact = header.ToJsonString(CompactOptions);
        var payloadCompact = payload.ToJsonString(CompactOptions);

        var headerSegment = Base64Url(headerCompact);
        var payloadSegment = Base64Url(payloadCompact);
        var signingInput = $"{headerSegment}.{payloadSegment}";

        var signResult = _crypto.SignEd25519(issuerPrivateKey, signingInput);
        var signatureSegment = Base64Url(signResult.Signature!);

        var jwt = $"{signingInput}.{signatureSegment}";

        return new IssuedJwtVc(
            Jwt: jwt,
            CredentialId: credentialId,
            HeaderPretty: header.ToJsonString(PrettyOptions),
            PayloadPretty: payload.ToJsonString(PrettyOptions),
            VcBodyPretty: vc.ToJsonString(PrettyOptions));
    }

    /// <summary>
    /// Verifies a JWT-VC against an issuer's Ed25519 public key. Pure crypto: doesn't
    /// resolve DIDs or check status lists. Returns structured result describing each check.
    /// </summary>
    public VerifyResult VerifyJwtVc(string jwt, byte[] expectedIssuerPublicKey)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return VerifyResult.Fail("JWT must have 3 segments.");

        string headerJson, payloadJson;
        try
        {
            headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        }
        catch (Exception ex)
        {
            return VerifyResult.Fail($"Could not base64url-decode segments: {ex.Message}");
        }

        var signature = Base64UrlDecode(parts[2]);
        var signingInput = $"{parts[0]}.{parts[1]}";

        var sigValid = _crypto.VerifyEd25519(expectedIssuerPublicKey, signingInput, signature);
        if (!sigValid)
            return new VerifyResult(false, "Signature did not verify.", headerJson, payloadJson, false, null);

        // Check exp if present.
        bool? expired = null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("exp", out var expEl) && expEl.ValueKind == JsonValueKind.Number)
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64());
                expired = exp < DateTimeOffset.UtcNow;
                if (expired == true)
                    return new VerifyResult(false, $"Credential expired at {exp:O}", headerJson, payloadJson, true, true);
            }
        }
        catch { /* tolerate; payload issues are signature-orthogonal */ }

        return new VerifyResult(true, "Verified.", headerJson, payloadJson, true, expired);
    }

    private static string Base64Url(string utf8) => Base64Url(Encoding.UTF8.GetBytes(utf8));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        JsonNode node => node.DeepClone(),
        _ => JsonValue.Create(value.ToString())
    };
}

public record IssuedJwtVc(
    string Jwt,
    string CredentialId,
    string HeaderPretty,
    string PayloadPretty,
    string VcBodyPretty);

public record VerifyResult(
    bool Valid,
    string Message,
    string? DecodedHeaderJson,
    string? DecodedPayloadJson,
    bool SignatureValid,
    bool? Expired)
{
    public static VerifyResult Fail(string msg) => new(false, msg, null, null, false, null);
}
