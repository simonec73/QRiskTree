using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using System.ComponentModel;

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

        public string Description
        {
            get => _model.Description;
            set
            {
                if (_model.Description != value)
                {
                    _model.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

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

        public uint Iterations { get; set; } = Node.DefaultIterations;
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
