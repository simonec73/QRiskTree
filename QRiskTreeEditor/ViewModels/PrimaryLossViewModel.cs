using QRiskTree.Engine.Model;

namespace QRiskTreeEditor.ViewModels
{
    internal class PrimaryLossViewModel : NodeViewModel
    {
        public PrimaryLossViewModel(PrimaryLoss node, NodeViewModel parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Properties.
        public LossForm Form
        {
            get => (_node as PrimaryLoss)?.Form ?? LossForm.Undetermined;
            set
            {
                if (_node is PrimaryLoss primaryLoss && primaryLoss.Form != value)
                {
                    primaryLoss.Set(value);
                    OnPropertyChanged(nameof(Form));
                }
            }
        }
        #endregion
    }
}