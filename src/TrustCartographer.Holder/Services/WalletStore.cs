using System.Collections.Concurrent;

namespace TrustCartographer.Holder.Services;

/// <summary>
/// Holds credentials the wallet has received from any Issuer. In-memory, per-process.
/// </summary>
public sealed class WalletStore
{
    private readonly ConcurrentDictionary<string, StoredCredential> _credentials = new();

    public StoredCredential Store(string credentialJwt, string credentialIssuer, string credentialConfigurationId)
    {
        var id = Guid.NewGuid().ToString("N");
        var record = new StoredCredential(
            Id: id,
            Jwt: credentialJwt,
            CredentialIssuer: credentialIssuer,
            CredentialConfigurationId: credentialConfigurationId,
            ReceivedAt: DateTimeOffset.UtcNow);
        _credentials[id] = record;
        return record;
    }

    public IReadOnlyCollection<StoredCredential> All() => _credentials.Values.ToArray();
}

public record StoredCredential(
    string Id,
    string Jwt,
    string CredentialIssuer,
    string CredentialConfigurationId,
    DateTimeOffset ReceivedAt);
