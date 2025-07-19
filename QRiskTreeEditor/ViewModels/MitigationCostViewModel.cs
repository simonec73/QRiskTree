using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Xml.Linq;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class MitigationCostViewModel : NodeViewModel
    {
        public MitigationCostViewModel(MitigationCost node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
            _related = new ObservableCollection<LinkedNodeViewModel>();
            Related = CollectionViewSource.GetDefaultView(_related);
            Related.SortDescriptions.Add(new SortDescription(nameof(NodeType), ListSortDirection.Ascending));
            Related.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        #region Properties.
        [Category("Mitigation")]
        [DisplayName("Enabled")]
        public bool IsEnabled
        {
            get => (_node as MitigationCost)?.IsEnabled ?? false;
            set
            {
                if (_node is MitigationCost mitigationCost && mitigationCost.IsEnabled != value)
                {
                    mitigationCost.IsEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        private QRiskTree.Engine.Range? OperationCosts => (_node as MitigationCost)?.OperationCosts;

        [Category("Range")]
        [DisplayName("Operation Costs set by User")]
        public bool IsOperationCostSetByUser => !(OperationCosts?.Calculated ?? true);


        [Browsable(false)]
        public double OperationMin
        {
            get => OperationCosts?.Min ?? 0.0;
            set
            {
                if (_node is MitigationCost mitigation)
                {
                    mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);

                    if (mitigation.OperationCosts.Min != value)
                    {
                        try
                        {
                            mitigation.OperationCosts.Min = value;
                            OnPropertyChanged(nameof(OperationMin));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Operation Minimum Value")]
        public string FormattedOperationMin
        {
            get
            {
                if (OperationCosts == null)
                    return OperationMin.ToString();
                else
                    return OperationMin.ToString(OperationCosts?.GetFormat());
            }

            set
            {
                if (FormattedOperationMin.TryChangeValue(value, out var calculated))
                {
                    if (_node is MitigationCost mitigation)
                    {
                        try
                        {
                            mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);
                            mitigation.OperationCosts.Min = calculated;
                            OnPropertyChanged(nameof(OperationMin));
                            OnPropertyChanged(nameof(FormattedOperationMin));
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
        public double OperationMode
        {
            get => OperationCosts?.Mode ?? 0.0;
            set
            {
                if (_node is MitigationCost mitigation)
                {
                    mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);

                    if (mitigation.OperationCosts.Mode != value)
                    {
                        try
                        {
                            mitigation.OperationCosts.Mode = value;
                            OnPropertyChanged(nameof(OperationMode));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Operation Most Likely Value")]
        public string FormattedOperationMode
        {
            get
            {
                if (OperationCosts == null)
                    return OperationMode.ToString();
                else
                    return OperationMode.ToString(OperationCosts?.GetFormat());
            }

            set
            {
                if (FormattedOperationMode.TryChangeValue(value, out var calculated))
                {
                    if (_node is MitigationCost mitigation)
                    {
                        try
                        {
                            mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);
                            mitigation.OperationCosts.Mode = calculated;
                            OnPropertyChanged(nameof(OperationMode));
                            OnPropertyChanged(nameof(FormattedOperationMode));
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
        public double OperationMax
        {
            get => OperationCosts?.Max ?? 0.0;
            set
            {
                if (_node is MitigationCost mitigation)
                {
                    mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);

                    if (mitigation.OperationCosts.Max != value)
                    {
                        try
                        {
                            mitigation.OperationCosts.Max = value;
                            OnPropertyChanged(nameof(OperationMax));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Operation Maximum Value")]
        public string FormattedOperationMax
        {
            get
            {
                if (OperationCosts == null)
                    return OperationMax.ToString();
                else
                    return OperationMax.ToString(OperationCosts?.GetFormat());
            }

            set
            {
                if (FormattedOperationMax.TryChangeValue(value, out var calculated))
                {
                    if (_node is MitigationCost mitigation)
                    {
                        try
                        {
                            mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);
                            mitigation.OperationCosts.Max = calculated;
                            OnPropertyChanged(nameof(OperationMax));
                            OnPropertyChanged(nameof(FormattedOperationMax));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
                }
            }
        }

        [Category("Range")]
        [DisplayName("Operation Confidence")]
        [PT.SelectorStyle(PT.SelectorStyle.ComboBox)]
        public Confidence OperationConfidence
        {
            get => OperationCosts?.Confidence ?? Confidence.Low;
            set
            {
                if (_node is MitigationCost mitigation)
                {
                    mitigation.OperationCosts ??= new QRiskTree.Engine.Range(RangeType.Money);

                    if (mitigation.OperationCosts.Confidence != value)
                    {
                        try
                        {
                            mitigation.OperationCosts.Confidence = value;
                            OnPropertyChanged(nameof(OperationConfidence));
                        }
                        catch
                        {
                            // Ignore exceptions for invalid values.
                        }
                    }
                }
            }
        }
        #endregion

        #region Public methods.
        public void ResetOperationCosts()
        {
            if (_node is MitigationCost mitigation)
            {
                mitigation.OperationCosts?.Reset();
                OnPropertyChanged(nameof(OperationMin));
                OnPropertyChanged(nameof(OperationMode));
                OnPropertyChanged(nameof(OperationMax));
                OnPropertyChanged(nameof(FormattedOperationMin));
                OnPropertyChanged(nameof(FormattedOperationMode));
                OnPropertyChanged(nameof(FormattedOperationMax));
                OnPropertyChanged(nameof(OperationConfidence));
                OnPropertyChanged(nameof(IsOperationCostSetByUser));
            }
        }
        #endregion

        #region References.
        protected ObservableCollection<LinkedNodeViewModel> _related { get; }

        [Browsable(false)]
        public ICollectionView Related { get; }

        [Browsable(false)]
        public bool HasRelated => _related.Any();

        [Browsable(false)]
        public override bool HasChildren => base.HasChildren | _related.Any();

        internal void AddRelated(MitigatedRiskViewModel node)
        {
            _related.Add(new LinkedNodeViewModel(node, this));
            OnPropertyChanged(nameof(Related));
            OnPropertyChanged(nameof(HasRelated));
        }

        internal void RemoveRelated(NodeViewModel node)
        {
            var related = _related.FirstOrDefault(x => x.LinkedNode.Id == node.Id);
            if (related != null)
            {
                RemoveRelated(related);
            }
        }

        internal void RemoveRelated(LinkedNodeViewModel related)
        {
            _related.Remove(related);
            OnPropertyChanged(nameof(Related));
            OnPropertyChanged(nameof(HasRelated));
        }

        internal void InitializeRelated()
        {
            var risks = _model.Risks;

            if (risks != null)
            {
                foreach (MitigatedRiskViewModel risk in risks)
                {
                    var appliedMitigations = risk.Mitigations?.OfType<AppliedMitigationViewModel>()
                        .Where(x => x.MitigationCostId == Id);
                    if (appliedMitigations?.Any() ?? false)
                    {
                        _related.Add(new LinkedNodeViewModel(risk, this));
                    }
                }
            }
        }
        #endregion
    }
}