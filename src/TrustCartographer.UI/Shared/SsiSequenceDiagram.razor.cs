using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace TrustCartographer.UI.Shared;

public partial class SsiSequenceDiagram : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<SsiActor> Actors { get; set; } = Array.Empty<SsiActor>();
    [Parameter, EditorRequired] public IReadOnlyList<SsiStep> Steps { get; set; } = Array.Empty<SsiStep>();

    /// <summary>-1 = nothing visible yet; N = steps 0..N visible.</summary>
    [Parameter] public int CurrentIndex { get; set; } = -1;

    [Parameter] public bool Complete { get; set; }
    [Parameter] public bool Success { get; set; } = true;
    [Parameter] public string? FailureReason { get; set; }

    /// <summary>If set, the corresponding step's row gets a visual highlight to mark it as selected.</summary>
    [Parameter] public int? SelectedStepIndex { get; set; }

    private readonly HashSet<int> openPayloadIndices = new();

    private string ColumnPercent(int actorIndex)
    {
        if (Actors.Count == 0) return "50%";
        var pct = (actorIndex + 0.5) / Actors.Count * 100.0;
        return pct.ToString("F4", CultureInfo.InvariantCulture) + "%";
    }

    private int ActorIndex(string key)
    {
        for (var i = 0; i < Actors.Count; i++)
            if (Actors[i].Key == key)
                return i;
        return 0;
    }

    private static bool HasPayload(SsiStep step) =>
        !string.IsNullOrEmpty(step.PayloadJson) || !string.IsNullOrEmpty(step.PayloadJwt);

    private void TogglePayload(int stepIndex)
    {
        if (!openPayloadIndices.Add(stepIndex))
            openPayloadIndices.Remove(stepIndex);
    }

    private void ClosePayload(int stepIndex) => openPayloadIndices.Remove(stepIndex);

    private static string OutcomeClass(StepOutcome outcome) => outcome switch
    {
        StepOutcome.Active => "tc-seq-hop-active",
        StepOutcome.Success => "tc-seq-hop-success",
        StepOutcome.Fail => "tc-seq-hop-fail",
        _ => "tc-seq-hop-pending"
    };
}

public sealed class SsiActor
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Sublabel { get; init; }
    /// <summary>Longer description shown as a hover tooltip on the actor header.</summary>
    public string? Tooltip { get; init; }
}

public sealed class SsiStep
{
    public string FromKey { get; set; } = string.Empty;
    public string ToKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Sublabel { get; set; }
    public StepOutcome Outcome { get; set; } = StepOutcome.Success;

    /// <summary>True if the action happens within a single actor (e.g. "mint key locally"). Rendered as a note.</summary>
    public bool IsLocalAction { get; set; }

    public string? PayloadJson { get; set; }
    public string? PayloadJwt { get; set; }
    public string PayloadLabel { get; set; } = "View payload";

    /// <summary>Optional HTTP verb ("GET", "POST", ...) shown next to the arrow as a colored chip.</summary>
    public string? HttpMethod { get; set; }
    /// <summary>Optional HTTP path shown alongside the verb (e.g. "/vp_token").</summary>
    public string? HttpPath { get; set; }
}

public enum StepOutcome
{
    Pending,
    Active,
    Success,
    Fail
}
