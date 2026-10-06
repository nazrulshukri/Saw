namespace Acms.Web;

/// <summary>Bound from "Acms:Ui".</summary>
public sealed class UiOptions
{
    public const string SectionName = "Acms:Ui";

    /// <summary>Attributes shown as columns on the equipment list, e.g. WSTYPE, STATE.</summary>
    public string[] SummaryAttributes { get; set; } = ["WSTYPE", "STATE"];
}
