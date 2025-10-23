using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using System.ComponentModel;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class RiskModelProperties : INotifyPropertyChanged
    {
        private readonly RiskModel _model;

        public RiskModelProperties(RiskModel model)
        {
            _model = model;
        }

        #region Properties.
        [Category("Model")]
        public string Name
        {
            get => _model.Name;
            set
            {
                if (_model.Name != value)
                {
                    _model.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [Category("Model")]
        public string Description
        {
            get => _model.Description ?? string.Empty;
            set
            {
                if (_model.Description != value)
                {
                    _model.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        [Category("Range Definition")]
        [DisplayName("Min Percentile")]
        public int MinPercentile
        {
            get => _model.MinPercentile;
            set
            {
                if (_model.MinPercentile != value)
                {
                    _model.MinPercentile = value;
                    OnPropertyChanged(nameof(MinPercentile));
                }
            }
        }

        [Category("Range Definition")]
        [DisplayName("Max Percentile")]
        public int MaxPercentile
        {
            get => _model.MaxPercentile;
            set
            {
                if (_model.MaxPercentile != value)
                {
                    _model.MaxPercentile = value;
                    OnPropertyChanged(nameof(MaxPercentile));
                }
            }
        }

        [Category("Range Definition")]
        [DisplayName("Currency Symbol")]
        public string CurrencySymbol
        {
            get => _model.CurrencySymbol;
            set
            {
                if (_model.CurrencySymbol != value)
                {
                    _model.CurrencySymbol = value;
                    OnPropertyChanged(nameof(CurrencySymbol));
                }
            }
        }

        [Category("Range Definition")]
        [DisplayName("Monetary Scale")]
        public string? MonetaryScale
        {
            get => _model.MonetaryScale;
            set
            {
                if (_model.MonetaryScale != value)
                {
                    _model.MonetaryScale = value;
                    OnPropertyChanged(nameof(MonetaryScale));
                }
            }
        }

        [Category("Simulation Parameters")]
        public uint Iterations { get; set; } = Node.DefaultIterations;

        [Category("Simulation Parameters")]
        [DisplayName("Optimize for")]
        [PT.SelectorStyle(PT.SelectorStyle.ComboBox)]
        public OptimizationParameter OptimizationParameter { get; set; } = OptimizationParameter.Mode;

        [Category("Simulation Parameters")]
        [DisplayName("Ignore implementation costs")]
        public bool IgnoreImplementationCosts { get; set; } = false;
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
