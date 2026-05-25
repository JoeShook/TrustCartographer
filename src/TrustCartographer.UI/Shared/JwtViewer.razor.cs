using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;

namespace TrustCartographer.UI.Shared;

public partial class JwtViewer : ComponentBase
{
    [Parameter] public string? Token { get; set; }
    [Parameter] public bool ShowSignature { get; set; } = true;

    private string? formattedHeader;
    private string? formattedPayload;
    private string? rawSignature;
    private string? parseError;

    protected override void OnParametersSet()
    {
        formattedHeader = null;
        formattedPayload = null;
        rawSignature = null;
        parseError = null;

        if (string.IsNullOrWhiteSpace(Token))
            return;

        try
        {
            var jwt = new JwtSecurityToken(Token);

            var headerJson = Base64UrlEncoder.Decode(jwt.EncodedHeader);
            using var headerDoc = JsonDocument.Parse(headerJson);
            formattedHeader = JsonSerializer.Serialize(headerDoc, IndentedOptions);

            using var payloadDoc = JsonDocument.Parse(jwt.Payload.SerializeToJson());
            formattedPayload = AddEpochAnnotations(JsonSerializer.Serialize(payloadDoc, IndentedOptions));

            if (ShowSignature)
            {
                var parts = Token.Split('.');
                if (parts.Length == 3)
                    rawSignature = parts[2];
            }
        }
        catch (Exception ex)
        {
            parseError = ex.Message;
        }
    }

    internal static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Regex EpochClaimRegex = new(
        @"(&quot;|"")(nbf|iat|exp)(&quot;|""):\s*(\d+)",
        RegexOptions.Compiled);

    internal static string AddEpochAnnotations(string text)
    {
        return EpochClaimRegex.Replace(text, match =>
        {
            var quoteOpen = match.Groups[1].Value;
            var claimName = match.Groups[2].Value;
            var quoteClose = match.Groups[3].Value;
            var epochStr = match.Groups[4].Value;

            if (!long.TryParse(epochStr, out var epoch))
                return match.Value;

            var utc = DateTimeOffset.FromUnixTimeSeconds(epoch);
            var local = utc.ToLocalTime();
            var expired = claimName == "exp" && utc < DateTimeOffset.UtcNow;
            var fullName = claimName switch
            {
                "nbf" => "Not Before",
                "iat" => "Issued At",
                "exp" => "Expiration Time",
                _ => claimName
            };

            var expiredMarker = expired ? " <span style=\"color:#e94560;\">EXPIRED</span>" : "";
            return $"{quoteOpen}{claimName}{quoteClose}: {epochStr}" +
                   $"\n<span style=\"color:#78909c;font-size:0.68rem;\">         ↳ {fullName}: {utc:yyyy-MM-dd HH:mm:ss} UTC ({local:h:mm:ss tt} local){expiredMarker}</span>";
        });
    }
}
