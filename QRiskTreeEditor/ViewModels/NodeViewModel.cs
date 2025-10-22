using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class NodeViewModel : FactsContainerViewModel, INotifyPropertyChanged
    {
        public NodeViewModel(NodeWithFacts node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
            _model.PropertyChanged += OnModelPropertyChanged;
            _node.Changed += OnNodeChanged;

            _components = new ObservableCollection<NodeViewModel>();
            var children = node.Children?.OfType<NodeWithFacts>().Where(x => !(x is AppliedMitigation)).ToArray();
            if (children?.Any() ?? false)
            {
                foreach (var child in children)
                {
                    NodeViewModel? childViewModel = null;
                    if (child is ContactFrequency cf)
                    {
                        childViewModel = new ContactFrequencyViewModel(cf, this, _model);
                    }
                    else if (child is LossEventFrequency lef)
                    {
                        childViewModel = new LossEventFrequencyViewModel(lef, this, _model);
                    }
                    else if (child is LossMagnitude lm)
                    {
                        childViewModel = new LossMagnitudeViewModel(lm, this, _model);
                    }
                    else if (child is PrimaryLoss pl)
                    {
                        childViewModel = new PrimaryLossViewModel(pl, this, _model);
                    }
                    else if (child is ProbabilityOfAction poa)
                    {
                        childViewModel = new ProbabilityOfActionViewModel(poa, this, _model);
                    }
                    else if (child is ResistenceStrength rs)
                    {
                        childViewModel = new ResistenceStrengthViewModel(rs, this, _model);
                    }
                    else if (child is SecondaryLossEventFrequency slef)
                    {
                        childViewModel = new SecondaryLossEventFrequencyViewModel(slef, this, _model);
                    }
                    else if (child is SecondaryLossMagnitude slm)
                    {
                        childViewModel = new SecondaryLossMagnitudeViewModel(slm, this, _model);
                    }
                    else if (child is SecondaryRisk sr)
                    {
                        childViewModel = new SecondaryRiskViewModel(sr, this, _model);
                    }
                    else if (child is ThreatCapability tc)
                    {
                        childViewModel = new ThreatCapabilityViewModel(tc, this, _model);
                    }
                    else if (child is ThreatEventFrequency tef)
                    {
                        childViewModel = new ThreatEventFrequencyViewModel(tef, this, _model);
                    }
                    else if (child is Vulnerability v)
                    {
                        childViewModel = new VulnerabilityViewModel(v, this, _model);
                    }

                    if (childViewModel != null)
                    {
                        _components.Add(childViewModel);
                        childViewModel.InitializeFacts();
                    }
                }
            }
            Components = CollectionViewSource.GetDefaultView(_components);
            Components.SortDescriptions.Add(new SortDescription(nameof(NodeType), ListSortDirection.Ascending));
            Components.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));

            _mitigations = new ObservableCollection<AppliedMitigationViewModel>();
            Mitigations = CollectionViewSource.GetDefaultView(_mitigations);
            Mitigations.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        // Expose the underlying object if needed
        [Browsable(false)]
        public NodeWithFacts Node => _node;

        #region Events management.
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.CompareOrdinal(e.PropertyName, nameof(RiskModelProperties.CurrencySymbol)) == 0)
            {
                OnPropertyChanged(nameof(FormattedMin));
                OnPropertyChanged(nameof(FormattedMode));
                OnPropertyChanged(nameof(FormattedMax));
            }
            else if (string.CompareOrdinal(e.PropertyName, nameof(RiskModelProperties.MonetaryScale)) == 0)
            {
                OnPropertyChanged(nameof(FormattedMin));
                OnPropertyChanged(nameof(FormattedMode));
                OnPropertyChanged(nameof(FormattedMax));
            }
        }

        protected override void RaiseUpdateEvent(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        private void OnNodeChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(string.Empty); // Notify all properties have changed
        }
        #endregion

        #region Public member functions.
        public void Delete()
        {
            if (_parent != null)
            {
                var components = Components.OfType<NodeViewModel>().ToArray();
                if (components.Any())
                {
                    foreach (var child in components)
                    {
                        child.Delete();
                    }
                }
                var facts = Facts?.OfType<LinkedFactViewModel>().ToArray();
                if (facts?.Any() ?? false)
                {
                    foreach (var fact in facts)
                    {
                        RemoveFact(fact.LinkedFact);
                    }
                }

                _parent.RemoveChild(this);
                _model.PropertyChanged -= OnModelPropertyChanged;
                _node.Changed -= OnNodeChanged;
            }
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public void Reset()
        {
            _node.Reset();
            OnPropertyChanged(nameof(Min));
            OnPropertyChanged(nameof(FormattedMin));
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(FormattedMode));
            OnPropertyChanged(nameof(Max));
            OnPropertyChanged(nameof(FormattedMax));
            OnPropertyChanged(nameof(Confidence));
            OnPropertyChanged(nameof(IsSetByUser)); 
        }

        public NodeViewModel? Clone(NodeViewModel? parent = null)
        {
            NodeViewModel? result = null;

            if (_node is ContactFrequency cf && parent is ThreatEventFrequencyViewModel tf)
            {
                var cfVM = tf.AddContactFrequency(cf.Name ?? string.Empty);
                if (cfVM != null)
                {
                    cfVM.ContactType = cf.ContactType;
                    result = cfVM;
                }
            }
            else if (_node is LossEventFrequency lef && parent is MitigatedRiskViewModel mr)
            {
                var lefVM = mr.AddLossEventFrequency(lef.Name ?? string.Empty);
                if (lefVM != null)
                {
                    var tef = _components.OfType<ThreatEventFrequencyViewModel>().FirstOrDefault();
                    if (tef != null)
                    {
                        tef.Clone(lefVM);
                    }

                    var v = _components.OfType<VulnerabilityViewModel>().FirstOrDefault();
                    if (v != null)
                    {
                        v.Clone(lefVM);
                    }

                    result = lefVM;
                }
            }
            else if (_node is LossMagnitude lm && parent is MitigatedRiskViewModel mr2)
            {
                var lmVM = mr2.AddLossMagnitude(lm.Name ?? string.Empty);
                if (lmVM != null)
                {
                    var pls = _components.OfType<PrimaryLossViewModel>().ToArray();
                    if (pls?.Any() ?? false)
                    {
                        foreach (var pl in pls)
                        {
                            pl.Clone(lmVM);
                        }
                    }

                    var srs = _components.OfType<SecondaryRiskViewModel>().ToArray();
                    if (srs?.Any() ?? false)
                    {
                        foreach (var sr in srs)
                        {
                            sr.Clone(lmVM);
                        }
                    }

                    result = lmVM;
                }

            }
            else if (_node is PrimaryLoss pl && parent is LossMagnitudeViewModel lm2)
            {
                var plVM = lm2.AddPrimaryLoss(pl.Name ?? string.Empty);
                if (plVM != null)
                {
                    plVM.Form = pl.Form;
                    result = plVM;
                }
            }
            else if (_node is ProbabilityOfAction poa && parent is ThreatEventFrequencyViewModel tef)
            {
                var poaVM = tef.AddProbabilityOfAction(poa.Name ?? string.Empty);
                if (poaVM != null)
                {
                    result = poaVM;
                }
            }
            else if (_node is ResistenceStrength rs && parent is VulnerabilityViewModel v)
            {
                var rsVM = v.AddResistenceStrength(rs.Name ?? string.Empty);
                if (rsVM != null)
                {
                    result = rsVM;
                }
            }
            else if (_node is MitigatedRisk mitigatedRisk)
            {
                var mrVM = _model.AddRisk($"{mitigatedRisk.Name} (copy)");
                if (mrVM != null)
                {
                    mrVM.IsEnabled = mitigatedRisk.IsEnabled;

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

                    result = mrVM;
                }
            }
            else if (_node is SecondaryLossEventFrequency slef && parent is SecondaryRiskViewModel sr)
            {
                var slefVM = sr.AddSecondaryLossEventFrequency(slef.Name ?? string.Empty);
                if (slefVM != null)
                {
                    result = slefVM;
                }
            }
            else if (_node is SecondaryLossMagnitude slm && parent is SecondaryRiskViewModel sr2)
            {
                var slmVM = sr2.AddSecondaryLossMagnitude(slm.Name ?? string.Empty);
                if (slmVM != null)
                {
                    result = slmVM;
                }
            }
            else if (_node is SecondaryRisk sr3 && parent is LossMagnitudeViewModel lm3)
            {
                var srVM = lm3.AddSecondaryRisk(sr3.Name ?? string.Empty);
                if (srVM != null)
                {
                    srVM.Form = sr3.Form;

                    var slef2 = _components.OfType<SecondaryLossEventFrequencyViewModel>().FirstOrDefault();
                    if (slef2 != null)
                    {
                        slef2.Clone(srVM);
                    }

                    var slm2 = _components.OfType<SecondaryLossMagnitudeViewModel>().FirstOrDefault();
                    if (slm2 != null)
                    {
                        slm2.Clone(srVM);
                    }

                    result = srVM;
                }
            }
            else if (_node is ThreatCapability tc && parent is VulnerabilityViewModel v2)
            {
                var tcVM = v2.AddThreatCapability(tc.Name ?? string.Empty);
                if (tcVM != null)
                {
                    result = tcVM;
                }
            }
            else if (_node is ThreatEventFrequency tef2 && parent is LossEventFrequencyViewModel lef2)
            {
                var tefVM = lef2.AddThreatEventFrequency(tef2.Name ?? string.Empty);
                if (tefVM != null)
                {
                    var cf2 = _components.OfType<ContactFrequencyViewModel>().FirstOrDefault();
                    if (cf2 != null)
                    {
                        cf2.Clone(tefVM);
                    }

                    var poa2 = _components.OfType<ProbabilityOfActionViewModel>().FirstOrDefault();
                    if (poa2 != null)
                    {
                        poa2.Clone(tefVM);
                    }

                    result = tefVM;
                }
            }
            else if (_node is Vulnerability v3 && parent is LossEventFrequencyViewModel lef3)
            {
                var vVM = lef3.AddVulnerability(v3.Name ?? string.Empty);
                if (vVM != null)
                {
                    var rs2 = _components.OfType<ResistenceStrengthViewModel>().FirstOrDefault();
                    if (rs2 != null)
                    {
                        rs2.Clone(vVM);
                    }

                    var tc2 = _components.OfType<ThreatCapabilityViewModel>().FirstOrDefault();
                    if (tc2 != null)
                    {
                        tc2.Clone(vVM);
                    }

                    result = vVM;
                }
            }

            if (result != null)
            {
                result.Description = Description;
                if (IsSetByUser)
                {
                    result.Min = Min;
                    result.Mode = Mode;
                    result.Max = Max;
                    result.Confidence = Confidence;
                }

                if (HasFacts)
                {
                    foreach (var fact in _facts)
                    {
                        if (fact?.LinkedFact != null)
                            result.AddFact(fact.LinkedFact);
                    }
                }
            }

            return result;
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
        #endregion

        #region Properties.
        [Category("General")]
        public string? Name
        {
            get => _node.Name;
            set
            {
                if (_node.Name != value)
                {
                    _node.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [Category("General")]
        public string? Description
        {
            get => _node.Description;
            set
            {
                if (_node.Description != value)
                {
                    _node.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        [Browsable(false)]
        public RangeType RangeType => _node.RangeType;

        [Browsable(false)]
        public double Min
        {
            get => _node.Min;
            set
            {
                if (_node.Min != value)
                {
                    try
                    {
                        _node.Min = value;
                        OnPropertyChanged(nameof(Min));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Minimum Value")]
        public string FormattedMin
        {
            get
            {
                return _node.GetMin(_model.Properties.CurrencySymbol, _model.Properties.MonetaryScale);
            }

            set
            {
                if (FormattedMin.TryChangeValue(value, _model.Properties.CurrencySymbol, 
                    _model.Properties.MonetaryScale, out var calculated))
                {
                    try
                    {
                        _node.Min = calculated;
                        OnPropertyChanged(nameof(Min));
                        OnPropertyChanged(nameof(FormattedMin));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Browsable(false)]
        public double Mode
        {
            get => _node.Mode;
            set
            {
                if (_node.Mode != value)
                {
                    try
                    {
                        _node.Mode = value;
                        OnPropertyChanged(nameof(Mode));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Mode Value")]
        public string FormattedMode
        {
            get
            {
                return _node.GetMode(_model.Properties.CurrencySymbol, _model.Properties.MonetaryScale);
            }

            set
            {
                if (FormattedMode.TryChangeValue(value, _model.Properties.CurrencySymbol, 
                    _model.Properties.MonetaryScale, out var calculated))
                {
                    try
                    {
                        _node.Mode = calculated;
                        OnPropertyChanged(nameof(Mode));
                        OnPropertyChanged(nameof(FormattedMode));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Browsable(false)]
        public double Max
        {
            get => _node.Max;
            set
            {
                if (_node.Max != value)
                {
                    try
                    {
                        _node.Max = value;
                        OnPropertyChanged(nameof(Max));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Maximum Value")]
        public string FormattedMax
        {
            get
            {
                return _node.GetMax(_model.Properties.CurrencySymbol, _model.Properties.MonetaryScale);
            }

            set
            {
                if (FormattedMax.TryChangeValue(value, _model.Properties.CurrencySymbol, 
                    _model.Properties.MonetaryScale, out var calculated))
                {
                    try
                    {
                        _node.Max = calculated;
                        OnPropertyChanged(nameof(Max));
                        OnPropertyChanged(nameof(FormattedMax));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }
            }
        }

        [Category("Range")]
        [PT.SelectorStyle(PT.SelectorStyle.ComboBox)]
        public Confidence Confidence
        {
            get => _node.Confidence;
            set
            {
                if (_node.Confidence != value)
                {
                    _node.Confidence = value;
                    OnPropertyChanged(nameof(Confidence));
                }
            }
        }

        [Category("Range")]
        [DisplayName("Range set by User")]
        public bool IsSetByUser => !(_node.Calculated ?? true);

        [Category("Update")]
        public string? CreatedBy => _node.CreatedBy;

        [Category("Update")]
        public DateTime CreatedOn => _node.CreatedOn;

        [Category("Update")]
        public string? ModifiedBy => _node.ModifiedBy;

        [Category("Update")]
        public DateTime ModifiedOn => _node.ModifiedOn;
        #endregion

        #region Children management.
        protected ObservableCollection<NodeViewModel> _components { get; }

        protected ObservableCollection<AppliedMitigationViewModel> _mitigations { get; }

        [Browsable(false)]
        public ICollectionView Components { get; }

        [Browsable(false)]
        public ICollectionView Mitigations { get; }

        [Browsable(false)]
        public override bool HasChildren => _components.Any() || _mitigations.Any() || base.HasChildren;

        [Browsable(false)]
        public bool HasComponents => _components.Any();

        [Browsable(false)]
        public bool HasMitigations => _mitigations.Any();

        public void AddChild(NodeViewModel child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (_node.Add(child.Node))
            {
                _components.Add(child);
                OnPropertyChanged(nameof(_components));
                OnPropertyChanged(nameof(HasComponents));
                OnPropertyChanged(nameof(HasChildren));
            }
        }

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

        public void RemoveChild(NodeViewModel child)
        {
            if (_node.Remove(child.Node))
            {
                _components.Remove(child);
                OnPropertyChanged(nameof(_components));
                OnPropertyChanged(nameof(HasComponents));
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
        #endregion
    }
}
