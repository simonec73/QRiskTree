using QRiskTree.Engine;
using QRiskTree.Engine.Facts;
using System.ComponentModel;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class FactAnalyzerViewModel : NodeViewModel
    {
        public FactAnalyzerViewModel(FactAnalyzerNode node, NodeViewModel? parent, RiskModelViewModel model) : base(node, parent, model)
        {
        }

        #region Properties.
        [Category("Fact Analysis")]
        [DisplayName("Operation")]
        [PT.SortIndex(50)]
        public FactAnalyzerOperation Operation
        {
            get => (_node as FactAnalyzerNode)?.Operation ?? FactAnalyzerOperation.Sum;
            set
            {
                if (_node is FactAnalyzerNode factAnalyzer && factAnalyzer.Operation != value)
                {
                    factAnalyzer.Operation = value;
                    OnPropertyChanged(nameof(Operation));
                }
            }
        }

        [Category("Fact Analysis")]
        [PT.SortIndex(51)]
        public bool Opposite
        {
            get => (_node as FactAnalyzerNode)?.Opposite ?? false;
            set
            {
                if (_node is FactAnalyzerNode factAnalyzer && factAnalyzer.Opposite != value)
                {
                    factAnalyzer.Opposite = value;
                    OnPropertyChanged(nameof(Opposite));
                }
            }
        }

        [Category("Fact Analysis")]
        [PT.SortIndex(52)]
        public bool Reciprocal
        {
            get => (_node as FactAnalyzerNode)?.Reciprocal ?? false;
            set
            {
                if (_node is FactAnalyzerNode factAnalyzer && factAnalyzer.Reciprocal != value)
                {
                    factAnalyzer.Reciprocal = value;
                    OnPropertyChanged(nameof(Reciprocal));
                }
            }
        }

        [Category("Range")]
        [DisplayName("Minimum Value")]
        [ReadOnly(true)]
        [PT.SortIndex(100)]
        public new string FormattedMin => base.FormattedMin;

        [Category("Range")]
        [DisplayName("Mode Value")]
        [ReadOnly(true)]
        [PT.SortIndex(101)]
        public new string FormattedMode => base.FormattedMode;

        [Category("Range")]
        [DisplayName("Maximum Value")]
        [ReadOnly(true)]
        [PT.SortIndex(102)]
        public new string FormattedMax => base.FormattedMax;

        [Browsable(false)]
        public new Confidence Confidence => base.Confidence;

        [Category("Range")]
        [DisplayName("Confidence")]
        [ReadOnly(true)]
        [PT.SortIndex(103)]
        public string ConfidenceText => base.Confidence.ToString();
        #endregion

        #region Public members.
        public FactAnalyzerViewModel? Clone()
        {
            FactAnalyzerViewModel? result = null;

            result = _model.AddFactAnalyzer($"{Name} (copy)");

            if (result != null)
            {
                result.Description = Description;
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

        public FactAnalyzerViewModel? AddFactAnalyzer(string name)
        {
            var factAnalyzer = new FactAnalyzerNode(name);
            var result = new FactAnalyzerViewModel(factAnalyzer, this, _model);
            AddChild(result);

            return result;
        }
        #endregion
    }
}