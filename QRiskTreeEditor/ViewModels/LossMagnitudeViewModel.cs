using QRiskTree.Engine.Model;

namespace QRiskTreeEditor.ViewModels
{
    internal class LossMagnitudeViewModel : NodeViewModel
    {
        public LossMagnitudeViewModel(LossMagnitude node, NodeViewModel parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Children management.
        public PrimaryLossViewModel? AddPrimaryLoss(string name)
        {
            PrimaryLossViewModel? result = null;

            if (_node is LossMagnitude lossMagnitude)
            {
                var pl = lossMagnitude.AddPrimaryLoss(name);
                if (pl != null)
                {
                    result = new PrimaryLossViewModel(pl, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(Components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }

        public SecondaryRiskViewModel? AddSecondaryRisk(string name)
        {
            SecondaryRiskViewModel? result = null;

            if (_node is LossMagnitude lossMagnitude)
            {
                var sr = lossMagnitude.AddSecondaryRisk(name);
                if (sr != null)
                {
                    result = new SecondaryRiskViewModel(sr, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(Components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }
        #endregion
    }
}