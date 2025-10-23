using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTreeEditor.Importers;
using System.ComponentModel;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class AppliedMitigationViewModel : FactsContainerViewModel, INotifyPropertyChanged
    {
        public static AppliedMitigationViewModel? GetAppliedMitigationViewModel(MitigationCostViewModel mitigationCostVM, 
            MitigatedRiskViewModel riskVM, RiskModelViewModel model) 
        {
            AppliedMitigationViewModel? result = null;

            if (riskVM?.Node is MitigatedRisk mitigatedRisk &&
                mitigationCostVM?.Node is MitigationCost mitigationCost &&
                mitigatedRisk.ApplyMitigation(mitigationCost, out var applied) &&
                applied != null)
            {
                result = new AppliedMitigationViewModel(mitigationCostVM, applied, riskVM, model);
            }

            return result;
        }

        public static AppliedMitigationViewModel? GetAppliedMitigationViewModel(AppliedMitigation appliedMitigation, RiskModelViewModel model)
        {
            AppliedMitigationViewModel? result = null;

            if (model != null && appliedMitigation != null && appliedMitigation.MitigationCost != null)
            {
                var mitigationCostVM = model?.Mitigations?.OfType<MitigationCostViewModel>()
                    .FirstOrDefault(m => m.Id == appliedMitigation.MitigationCostId);
                var riskVM = model?.Risks?.OfType<MitigatedRiskViewModel>()
                    .FirstOrDefault(x => x.Node.Children?.OfType<AppliedMitigation>()
                        .Any(m => m.Id == appliedMitigation.Id) ?? false);
                if (mitigationCostVM != null && riskVM != null)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    result = new AppliedMitigationViewModel(mitigationCostVM, appliedMitigation, riskVM, model);
#pragma warning restore CS8604 // Possible null reference argument.
                }
            }
            return result;
        }

        private AppliedMitigationViewModel(MitigationCostViewModel mitigationCostVM, AppliedMitigation node, 
            MitigatedRiskViewModel parent, RiskModelViewModel model) : base(node, parent, model)
        {
            mitigationCostVM.PropertyChanged += OnMitigationCostPropertyChanged;
        }

        private void OnMitigationCostPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MitigationCostViewModel mitigationCostVM &&
                (e.PropertyName == "Name" || e.PropertyName == "Description" || e.PropertyName == "ControlType"))
            {
                if (e.PropertyName == "Name")
                {
                    _node.Name = mitigationCostVM.Name;
                }
                else if (e.PropertyName == "Description")
                {
                    _node.Description = mitigationCostVM.Description;
                }
                OnPropertyChanged(e.PropertyName);
            }
        }

        protected override void RaiseUpdateEvent(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public void Delete()
        {
            if (_parent != null)
            {
                _model.Mitigations?.OfType<MitigationCostViewModel>()
                    .FirstOrDefault(m => m.Id == MitigationCostId)?.RemoveRelated(_parent);
                _parent.RemoveChild(this);
            }
        }

        // Expose the underlying object if needed
        [Browsable(false)]
        public NodeWithFacts Node => _node;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            OnPropertyChanged(nameof(IsAuxiliary));
        }

        #region Properties.
        [Browsable(false)]
        public Guid MitigationCostId => (_node as AppliedMitigation)?.MitigationCostId ?? Guid.Empty;

        [Browsable(false)]
        public MitigationCost? MitigationCost => (_node as AppliedMitigation)?.MitigationCost;

        [Category("General")]
        public bool IsEnabled => (_node as AppliedMitigation)?.IsEnabled ?? false;

        [Category("General")]
        public string? Name => _node.Name;

        [Category("General")]
        public string? Description => _node.Description;

        [Category("Mitigation")]
        [DisplayName("Control Type")]
        public ControlType ControlType => MitigationCost?.ControlType ?? ControlType.Unknown;

        [Category("Mitigation")]
        [DisplayName("Is Auxiliary")]
        public bool IsAuxiliary
        {
            get
            {
                return (_node as AppliedMitigation)?.IsAuxiliary ?? false;
            }

            set
            {
                if (_node is AppliedMitigation appliedMitigation)
                {
                    if (appliedMitigation.IsAuxiliary != value)
                    {
                        try
                        {
                            appliedMitigation.IsAuxiliary = value;
                            OnPropertyChanged(nameof(IsAuxiliary));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
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
                if (FormattedMin.TryChangeValue(value, 
                    _model.Properties.CurrencySymbol, _model.Properties.MonetaryScale, out var calculated))
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
                if (FormattedMode.TryChangeValue(value, 
                    _model.Properties.CurrencySymbol, _model.Properties.MonetaryScale, out var calculated))
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
                if (FormattedMax.TryChangeValue(value, 
                    _model.Properties.CurrencySymbol, _model.Properties.MonetaryScale, out var calculated))
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
    }
}