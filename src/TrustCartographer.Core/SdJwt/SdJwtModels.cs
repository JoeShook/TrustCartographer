using System.Text.Json.Nodes;

namespace TrustCartographer.Core.SdJwt;

/// <summary>
/// One selectively-disclosable claim. The wire artifact is <see cref="Encoded"/> —
/// base64url of the JSON array <c>[salt, claimName, claimValue]</c>. Its SHA-256
/// digest (<see cref="Digest"/>) is what the issuer commits to in the credential's
/// <c>_sd</c> array; the salt makes the digest unguessable for withheld claims.
/// </summary>
public sealed record Disclosure(
    string ClaimName,
    string ValueJson,
    string Salt,
    string Encoded,
    string Digest);

/// <summary>
/// An issued SD-JWT VC: the issuer-signed JWT, the full set of disclosures, and the
/// combined issuance form (<c>jwt~d1~d2~…~</c>). The holder stores all of this and
/// later presents only a chosen subset.
/// </summary>
public sealed record IssuedSdJwt(
    string IssuerJwt,
    IReadOnlyList<Disclosure> Disclosures,
    string Combined,
    string HeaderJson,
    string PayloadJson);

/// <summary>
/// Result of verifying a presented SD-JWT (issuer JWT + chosen disclosures + KB-JWT).
/// Each sub-check is reported so the UI can show precisely what passed or failed.
/// </summary>
public sealed record SdJwtVerifyResult(
    bool Valid,
    string Message,
    bool IssuerSignatureValid,
    bool DisclosuresValid,
    bool KeyBindingValid,
    bool AudienceValid,
    bool NonceValid,
    bool SdHashValid,
    IReadOnlyDictionary<string, JsonNode?> DisclosedClaims,
    int WithheldCount,
    string? HolderDid,
    string? IssuerDid,
    string? Vct,
    string? IssuerHeaderJson,
    string? IssuerPayloadJson,
    string? KbJwtHeaderJson,
    string? KbJwtPayloadJson)
{
    public static SdJwtVerifyResult Fail(string message) => new(
        false, message, false, false, false, false, false, false,
        new Dictionary<string, JsonNode?>(), 0,
        null, null, null, null, null, null, null);
}
