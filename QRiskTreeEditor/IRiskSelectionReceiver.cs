using QRiskTreeEditor.ViewModels;

namespace QRiskTreeEditor
{
    internal interface IRiskSelectionReceiver
    {
        object? SelectedRisk { get; set; }
    }

}
