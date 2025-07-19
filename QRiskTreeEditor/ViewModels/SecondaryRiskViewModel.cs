using QRiskTree.Engine.Model;

namespace QRiskTreeEditor.ViewModels
{
    internal class SecondaryRiskViewModel : NodeViewModel
    {
        public SecondaryRiskViewModel(SecondaryRisk node, NodeViewModel parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Properties.
        public LossForm Form
        {
            get => (_node as SecondaryRisk)?.Form ?? LossForm.Undetermined;
            set
            {
                if (_node is SecondaryRisk secondaryRisk && secondaryRisk.Form != value)
                {
                    secondaryRisk.Set(value);
                    OnPropertyChanged(nameof(Form));
                }
            }
        }
        #endregion

        #region Children management.
        public SecondaryLossEventFrequencyViewModel? AddSecondaryLossEventFrequency(string name)
        {
            SecondaryLossEventFrequencyViewModel? result = null;

            if (_node is SecondaryRisk secondaryRisk)
            {
                var slef = secondaryRisk.AddSecondaryLossEventFrequency(name);
                if (slef != null)
                {
                    result = new SecondaryLossEventFrequencyViewModel(slef, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(_components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }

        public SecondaryLossMagnitudeViewModel? AddSecondaryLossMagnitude(string name)
        {
            SecondaryLossMagnitudeViewModel? result = null;

            if (_node is SecondaryRisk secondaryRisk)
            {
                var slm = secondaryRisk.AddSecondaryLossMagnitude(name);
                if (slm != null)
                {
                    result = new SecondaryLossMagnitudeViewModel(slm, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(_components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }
        #endregion
    }
}