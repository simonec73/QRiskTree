using QRiskTree.Engine.Facts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace QRiskTreeEditor.ViewModels
{
    internal class LinkedFactViewModel : INotifyPropertyChanged
    {
        private readonly FactViewModel _fact;
        private readonly FactsContainerViewModel _node;

        public LinkedFactViewModel(FactViewModel fact, FactsContainerViewModel node)
        {
            _fact = fact;
            fact.PropertyChanged += OnPropertyChanged;
            _node = node;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Context" || e.PropertyName == "Source" ||
                e.PropertyName == "ReferenceDate" || e.PropertyName == "Name" ||
                e.PropertyName == "Details" || e.PropertyName == "FormattedValue")
            {
                if (e.PropertyName == "FormattedValue")
                    OnPropertyChanged(nameof(Value));
                else
                    OnPropertyChanged(e.PropertyName);
            }
        }

        // Expose the underlying object if needed
        [Browsable(false)]
        public FactViewModel LinkedFact => _fact;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Delete()
        {
            _node.RemoveFact(_fact);
        }

        #region Properties.
        [Category("Fact")]
        public string Context => _fact.Context;

        [Category("Fact")]
        public string Source => _fact.Source;

        [Category("Fact")]
        public string ReferenceDate => _fact.FormattedReferenceDate;

        [Category("Fact")]
        public string? Name => _fact.Name;

        [Category("Fact")]
        public string? Details => _fact.Details;

        [Category("Fact")]
        public string Value => _fact.FormattedValue;
        #endregion
    }
}
