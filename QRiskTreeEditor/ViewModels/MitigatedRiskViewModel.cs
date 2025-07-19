using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using System.ComponentModel;

namespace QRiskTreeEditor.ViewModels
{
    internal class MitigatedRiskViewModel : NodeViewModel
    {
        public MitigatedRiskViewModel(MitigatedRisk node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Properties.
        [Category("Mitigated Risk")]
        [DisplayName("Enabled")]
        public bool IsEnabled
        {
            get => (_node as MitigatedRisk)?.IsEnabled ?? false;
            set
            {
                if (_node is MitigatedRisk mitigatedRisk && mitigatedRisk.IsEnabled != value)
                {
                    mitigatedRisk.IsEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }
        #endregion

        #region Child management.
        public LossEventFrequencyViewModel? AddLossEventFrequency(string name)
        {
            LossEventFrequencyViewModel? result = null;

            if (_node is MitigatedRisk mitigatedRisk)
            {
                var lef = mitigatedRisk.AddLossEventFrequency(name);
                if (lef != null)
                {
                    result = new LossEventFrequencyViewModel(lef, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(Components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }

            return result;
        }

        public LossMagnitudeViewModel? AddLossMagnitude(string name)
        {
            LossMagnitudeViewModel? result = null;

            if (_node is MitigatedRisk mitigatedRisk)
            {
                var lm = mitigatedRisk.AddLossMagnitude(name);
                if (lm != null)
                {
                    result = new LossMagnitudeViewModel(lm, this, _model);
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

        #region Mitigations management.
        public void InitializeMitigations()
        {
            var mitigations = _node.Children?.OfType<AppliedMitigation>().ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                {
                    if (mitigation != null)
                    {
                        var mitigationViewModel = AppliedMitigationViewModel
                            .GetAppliedMitigationViewModel(mitigation, _model);
                        if (mitigationViewModel != null)
                        {
                            _mitigations.Add(mitigationViewModel);
                            mitigationViewModel.InitializeFacts();
                        }
                    }
                }
            }
        }

        public bool ApplyMitigation(MitigationCostViewModel mitigation, out AppliedMitigationViewModel? appliedMitigation)
        {
            var result = false;

            appliedMitigation = AppliedMitigationViewModel.GetAppliedMitigationViewModel(mitigation, this, _model);
            if (appliedMitigation != null)
            {
                _mitigations.Add(appliedMitigation);
                appliedMitigation.InitializeFacts();
                mitigation.AddRelated(this);
                OnPropertyChanged(nameof(Mitigations));
                OnPropertyChanged(nameof(HasMitigations));
                OnPropertyChanged(nameof(HasChildren));
            }

            return result;
        }

        public bool RemoveMitigation(MitigationCostViewModel mitigation)
        {
            if (_node is MitigatedRisk mitigatedRisk && mitigation.Node is MitigationCost mitigationCost)
            {
                var result = mitigatedRisk.RemoveMitigation(mitigationCost);
                if (result)
                {
                    var appliedMitigations = _mitigations.OfType<AppliedMitigationViewModel>()
                        .Where(x => x.MitigationCostId == mitigationCost.Id).ToArray();
                    foreach (var applied in appliedMitigations)
                    {
                        _mitigations.Remove(applied);
                    }
                    OnPropertyChanged(nameof(Mitigations));
                    OnPropertyChanged(nameof(HasMitigations));
                    OnPropertyChanged(nameof(HasChildren));
                }
                return result;
            }
            return false;
        }

        public void RemoveMitigations()
        {
            if (_node is MitigatedRisk mitigatedRisk)
            {
                mitigatedRisk.RemoveMitigations();
                var appliedMitigations = _mitigations.OfType<AppliedMitigationViewModel>().ToArray();
                foreach (var applied in appliedMitigations)
                {
                    _mitigations.Remove(applied);
                }
                OnPropertyChanged(nameof(Components));
                OnPropertyChanged(nameof(HasComponents));
                OnPropertyChanged(nameof(HasChildren));
            }
        }
        #endregion
    }
}
