using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;

namespace QRiskTreeEditor.ViewModels
{
    internal class LossEventFrequencyViewModel : NodeViewModel
    {
        public LossEventFrequencyViewModel(LossEventFrequency node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Children management.
        public ThreatEventFrequencyViewModel? AddThreatEventFrequency(string name)
        {
            ThreatEventFrequencyViewModel? result = null;

            if (_node is LossEventFrequency lossEventFrequency)
            {
                var lef = lossEventFrequency.AddThreatEventFrequency(name);
                if (lef != null)
                {
                    result = new ThreatEventFrequencyViewModel(lef, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(Components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }

        public VulnerabilityViewModel? AddVulnerability(string name)
        {
            VulnerabilityViewModel? result = null;

            if (_node is LossEventFrequency lossEventFrequency)
            {
                var v = lossEventFrequency.AddVulnerability(name);
                if (v != null)
                {
                    result = new VulnerabilityViewModel(v, this, _model);
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