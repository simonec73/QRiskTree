using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace QRiskTreeEditor.ViewModels
{
    internal class MitigatedRiskViewModel : NodeViewModel
    {
        public MitigatedRiskViewModel(MitigatedRisk node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
            _mitigations = new ObservableCollection<AppliedMitigationViewModel>();
            Mitigations = CollectionViewSource.GetDefaultView(_mitigations);
            Mitigations.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
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

        #region Overrides.
        public override NodeViewModel? Clone(NodeViewModel? parent = null)
        {
            NodeViewModel? result = null;

            var mrVM = _model.AddRisk($"{Name} (copy)");
            if (mrVM != null)
            {
                mrVM.Description = Description;
                mrVM.IsEnabled = IsEnabled;

                var lefVM = _components.OfType<LossEventFrequencyViewModel>().FirstOrDefault();
                if (lefVM != null)
                {
                    lefVM.Clone(mrVM);
                }

                var lmVM = _components.OfType<LossMagnitudeViewModel>().FirstOrDefault();
                if (lmVM != null)
                {
                    lmVM.Clone(mrVM);
                }

                CloneMitigations(mrVM);

                if (IsSetByUser)
                {
                    mrVM.Min = Min;
                    mrVM.Mode = Mode;
                    mrVM.Max = Max;
                    mrVM.Confidence = Confidence;
                }

                if (HasFacts)
                {
                    foreach (var fact in _facts)
                    {
                        if (fact?.LinkedFact != null)
                            mrVM.AddFact(fact.LinkedFact);
                    }
                }

                result = mrVM;
            }

            return result;
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

        [Browsable(false)]
        public override bool HasChildren => _mitigations.Any() || base.HasChildren;
        #endregion

        #region Mitigations management.
        protected ObservableCollection<AppliedMitigationViewModel> _mitigations { get; }

        [Browsable(false)]
        public ICollectionView Mitigations { get; }

        [Browsable(false)]
        public bool HasMitigations => _mitigations.Any();

        public void AddChild(AppliedMitigationViewModel child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (_node.Add(child.Node))
            {
                _mitigations.Add(child);
                OnPropertyChanged(nameof(_mitigations));
                OnPropertyChanged(nameof(HasMitigations));
                OnPropertyChanged(nameof(HasChildren));
            }
        }

        public void RemoveChild(AppliedMitigationViewModel child)
        {
            if (_node.Remove(child.Node))
            {
                _mitigations.Remove(child);
                OnPropertyChanged(nameof(_mitigations));
                OnPropertyChanged(nameof(HasMitigations));
                OnPropertyChanged(nameof(HasChildren));
            }
        }

        private void CloneMitigations(MitigatedRiskViewModel target)
        {
            var mitigations = Mitigations?.OfType<AppliedMitigationViewModel>()?.ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                {
                    var mitigationCost = _model.Mitigations?.OfType<MitigationCostViewModel>()
                        .FirstOrDefault(x => x.Id == mitigation.MitigationCostId);
                    if (mitigationCost != null)
                    {
                        target.ApplyMitigation(mitigationCost, out var appliedMitigation);
                        if (appliedMitigation != null)
                        {
                            appliedMitigation.Min = mitigation.Min;
                            appliedMitigation.Mode = mitigation.Mode;
                            appliedMitigation.Max = mitigation.Max;
                            appliedMitigation.Confidence = mitigation.Confidence;
                        }
                    }
                }
            }
        }

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
