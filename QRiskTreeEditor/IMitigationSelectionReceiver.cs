using QRiskTreeEditor.ViewModels;

namespace QRiskTreeEditor
{
    internal interface IMitigationSelectionReceiver
    {
        object? SelectedMitigation { get; set; }
    }

}
