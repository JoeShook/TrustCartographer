using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.SdJwt;

namespace TrustCartographer.UI.Shared;

/// <summary>
/// Renders an SD-JWT VC: the issuer-signed JWT (header/payload), the disclosures
/// (with each claim's value, status, and committed digest), and — when given a
/// <see cref="Presentation"/> string — which disclosures were revealed vs withheld
/// plus the holder's Key Binding JWT. The teaching centerpiece for selective disclosure.
/// </summary>
public partial class SdJwtViewer : ComponentBase
{
    /// <summary>The full issued credential (all disclosures + issuer header/payload).</summary>
    [Parameter] public IssuedSdJwt? Credential { get; set; }

    /// <summary>Optional presented form (<c>jwt~sel~…~kb</c>); drives revealed/withheld + KB display.</summary>
    [Parameter] public string? Presentation { get; set; }

    [Parameter] public bool ShowKeyBinding { get; set; } = true;

    private sealed record DisclosureRow(Disclosure D, bool Revealed);

    private List<DisclosureRow> rows = new();
    private bool hasPresentation;
    private string? kbHeader;
    private string? kbPayload;

    protected override void OnParametersSet()
    {
        rows = new();
        hasPresentation = false;
        kbHeader = null;
        kbPayload = null;

        if (Credential is null) return;

        var revealed = new HashSet<string>();
        if (!string.IsNullOrEmpty(Presentation))
        {
            hasPresentation = true;
            var segs = Presentation.Split(SdJwtConstants.Delimiter);
            for (var i = 1; i < segs.Length - 1; i++)
                if (segs[i].Length > 0) revealed.Add(segs[i]);

            var last = segs[^1];
            if (ShowKeyBinding && last.Length > 0)
            {
                var p = last.Split('.');
                if (p.Length == 3)
                {
                    try
                    {
                        kbHeader = Pretty(Base64Url.DecodeToString(p[0]));
                        kbPayload = JwtViewer.AddEpochAnnotations(Pretty(Base64Url.DecodeToString(p[1])));
                    }
                    catch { /* leave null */ }
                }
            }
        }

        foreach (var d in Credential.Disclosures)
            rows.Add(new DisclosureRow(d, !hasPresentation || revealed.Contains(d.Encoded)));
    }

    private string IssuerPayloadAnnotated =>
        Credential is null ? "" : JwtViewer.AddEpochAnnotations(Credential.PayloadJson);

    private static string Trunc(string s) => s.Length <= 24 ? s : $"{s[..16]}…{s[^4..]}";

    private static string Pretty(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, JwtViewer.IndentedOptions);
        }
        catch
        {
            return json;
        }
    }
}
