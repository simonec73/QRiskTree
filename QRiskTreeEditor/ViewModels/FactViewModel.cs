using PT = PropertyTools.DataAnnotations;
using QRiskTree.Engine.Facts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using QRiskTree.Engine;
using System.Windows.Data;

namespace QRiskTreeEditor.ViewModels
{
    internal class FactViewModel : INotifyPropertyChanged
    {
        private readonly Fact _fact;
        private readonly RiskModelViewModel _model;

        public FactViewModel(Fact fact, RiskModelViewModel model)
        {
            _fact = fact ?? throw new ArgumentNullException(nameof(fact));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _related = new ObservableCollection<LinkedNodeViewModel>();
            Related = CollectionViewSource.GetDefaultView(_related);
            Related.SortDescriptions.Add(new SortDescription(nameof(NodeViewModel.NodeType), ListSortDirection.Ascending));
            Related.SortDescriptions.Add(new SortDescription(nameof(NodeViewModel.Name), ListSortDirection.Ascending));
        }

        // Expose the underlying object if needed
        [Browsable(false)]
        public Fact Fact => _fact;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public FactViewModel? Clone()
        {
            FactViewModel? result = null;

            if (_fact is FactHardNumber hardNumber)
            {
                result = _model.AddFact(new FactHardNumber(hardNumber.Context, hardNumber.Source, 
                    hardNumber.Name, hardNumber.Value));
            }
            else if (_fact is FactRange factRange)
            {
                var r = factRange.Range;
                result = _model.AddFact(new FactRange(factRange.Context, factRange.Source, factRange.Name, 
                    new QRiskTree.Engine.Range(r.RangeType, r.Min, r.Mode, r.Max, r.Confidence)));
            }

            if (result != null)
            {
                result.Details = Details;
                result.ReferenceDate = ReferenceDate;
            }

            return result;
        }

        #region Properties.
        [Browsable(false)]
        public Guid Id => _fact.Id;

        [Category("Fact")]
        public string Context
        {
            get => _fact.Context;
            set
            {
                if (_fact.Context != value)
                {
                    _fact.Context = value;
                    OnPropertyChanged(nameof(Context));
                }
            }
        }

        [Category("Fact")]
        public string Source
        {
            get => _fact.Source;
            set
            {
                if (_fact.Source != value)
                {
                    _fact.Source = value;
                    OnPropertyChanged(nameof(Source));
                }
            }
        }

        [Browsable(false)]
        public DateTime ReferenceDate
        {
            get => _fact.ReferenceDate;
            set
            {
                if (_fact.ReferenceDate != value)
                {
                    _fact.ReferenceDate = value;
                    OnPropertyChanged(nameof(ReferenceDate));
                }
            }
        }

        [Category("Fact")]
        [DisplayName("Reference Date")]
        public string FormattedReferenceDate
        {
            get => _fact.ReferenceDate.ToString("yyyy-MM-dd");
            set
            {
                if (DateTime.TryParse(value, out var parsedDate) && _fact.ReferenceDate != parsedDate)
                {
                    _fact.ReferenceDate = parsedDate;
                    OnPropertyChanged(nameof(ReferenceDate));
                    OnPropertyChanged(nameof(FormattedReferenceDate));
                }
            }
        }

        [Category("Fact")]
        public string Name
        {
            get => _fact.Name;
            set
            {
                if (_fact.Name != value)
                {
                    _fact.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [Category("Fact")]
        public string? Details
        {
            get => _fact.Details;
            set
            {
                if (_fact.Details != value)
                {
                    _fact.Details = value;
                    OnPropertyChanged(nameof(Details));
                }
            }
        }

        [Browsable(false)]
        public string FormattedValue
        {
            get
            {
                var result = string.Empty;

                if (_fact is FactHardNumber hardNumber)
                {
                    result = hardNumber.Value.ToString();
                }
                else if (_fact is FactRange factRange && factRange.Range is QRiskTree.Engine.Range range)
                {
                    result = $"Min: {range.GetMin()} - Mode: {range.GetMode()} - Max: {range.GetMax()} - Confidence: {range.Confidence}.";
                }

                return result;
            }
        }

        [Browsable(false)]
        public bool IsHardNumber => _fact is FactHardNumber;

        [Browsable(false)]
        public bool IsRange => _fact is FactRange;

        [Category("Fact")]
        [PT.VisibleBy("IsHardNumber")]
        public string Value
        {
            get => (_fact as FactHardNumber)?.Value.ToString() ?? string.Empty;

            set
            {
                if (_fact is FactHardNumber hardNumber && string.CompareOrdinal(Value, value) != 0 &&
                    Value.TryChangeValue(value, out var calculated))
                {
                    hardNumber.Value = calculated;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(FormattedValue));
                }
            }
        }

        [Category("Fact")]
        [PT.VisibleBy("IsRange")]
        public string Min
        {
            get => (_fact as FactRange)?.Range?.GetMin() ?? string.Empty;

            set
            {
                if ((_fact as FactRange)?.Range is QRiskTree.Engine.Range range && 
                    Min.TryChangeValue(value, out var calculated))
                {
                    try
                    {
                        range.Min = calculated;
                        OnPropertyChanged(nameof(Min));
                        OnPropertyChanged(nameof(FormattedValue));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }

            }
        }

        [Category("Fact")]
        [PT.VisibleBy("IsRange")]
        public string Mode
        {
            get => (_fact as FactRange)?.Range?.GetMode() ?? string.Empty;

            set
            {
                if ((_fact as FactRange)?.Range is QRiskTree.Engine.Range range &&
                    Mode.TryChangeValue(value, out var calculated))
                {
                    try
                    {
                        range.Mode = calculated;
                        OnPropertyChanged(nameof(Mode));
                        OnPropertyChanged(nameof(FormattedValue));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }

            }
        }

        [Category("Fact")]
        [PT.VisibleBy("IsRange")]
        public string Max
        {
            get => (_fact as FactRange)?.Range?.GetMax() ?? string.Empty;

            set
            {
                if ((_fact as FactRange)?.Range is QRiskTree.Engine.Range range &&
                    Max.TryChangeValue(value, out var calculated))
                {
                    try
                    {
                        range.Max = calculated;
                        OnPropertyChanged(nameof(Max));
                        OnPropertyChanged(nameof(FormattedValue));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }

            }
        }

        [Category("Fact")]
        [PT.VisibleBy("IsRange")]
        [PT.SelectorStyle(PT.SelectorStyle.ComboBox)]
        public Confidence Confidence
        {
            get => (_fact as FactRange)?.Range?.Confidence ?? Confidence.Low;

            set
            {
                if ((_fact as FactRange)?.Range is QRiskTree.Engine.Range range)
                {
                    try
                    {
                        range.Confidence = value;
                        OnPropertyChanged(nameof(Confidence));
                        OnPropertyChanged(nameof(FormattedValue));
                    }
                    catch
                    {
                        // Ignore exceptions for invalid values.
                    }
                }

            }
        }
        #endregion

        #region References.
        protected ObservableCollection<LinkedNodeViewModel> _related { get; }

        [Browsable(false)]
        public ICollectionView Related { get; }

        [Browsable(false)]
        public bool HasRelated => _related.Any();

        internal void AddRelated(FactsContainerViewModel node)
        {
            _related.Add(new LinkedNodeViewModel(node, this));
            OnPropertyChanged(nameof(Related));
            OnPropertyChanged(nameof(HasRelated));
        }

        internal void RemoveRelated(FactsContainerViewModel node)
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
                foreach (NodeViewModel risk in risks)
                {
                    RecursivelyAnalyze(risk);
                }
            }
        }

        private void RecursivelyAnalyze(NodeViewModel node)
        {
            if (node.Node?.Facts?.Any(x => x.Id == _fact.Id) ?? false)
            {
                _related.Add(new LinkedNodeViewModel(node, this));
            }

            var children = node.Components;

            foreach (NodeViewModel child in children)
            {
                RecursivelyAnalyze(child);
            }
        }
        #endregion
    }
}
