using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.ImportExport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace QRiskTreeEditor.ViewModels
{
    internal class RiskModelViewModel : INotifyPropertyChanged,
        IRiskSelectionReceiver, IMitigationSelectionReceiver, IFactSelectionReceiver, IFactAnalyzerSelectionReceiver,
        IRiskModel<MitigatedRiskViewModel, MitigationCostViewModel>
    {
        private readonly RiskModel _model;
        private readonly RiskModelProperties _properties;

        public RiskModelViewModel(RiskModel model)
        {
            _model = model;
            _properties = new RiskModelProperties(model);
            _properties.PropertyChanged += (s, e) => {
                if (!string.IsNullOrEmpty(e?.PropertyName))
                    OnPropertyChanged(e.PropertyName);
            };

            #region Load the whole list of the Facts.
            var facts = model.AvailableFacts?.Select(x => new FactViewModel(x, this)).ToArray();
            if (facts?.Any() ?? false)
            {
                Facts = new ObservableCollection<FactViewModel>(facts);
            }
            else
            {
                Facts = new ObservableCollection<FactViewModel>();
            }
            #endregion

            #region Initialize the Fact Analyzers.
            _factAnalyzers = new ObservableCollection<FactAnalyzerViewModel>();
            var factAnalyzers = _model.FactAnalyzers.OfType<FactAnalyzerNode>().ToArray();
            if (factAnalyzers.Any())
            {
                foreach (var factAnalyzer in factAnalyzers)
                {
                    var factAnalyzerViewModel = new FactAnalyzerViewModel(factAnalyzer, null, this);
                    _factAnalyzers.Add(factAnalyzerViewModel);
                }
            }
            FactAnalyzers = CollectionViewSource.GetDefaultView(_factAnalyzers);
            FactAnalyzers.SortDescriptions.Add(new SortDescription(nameof(FactAnalyzerViewModel.Name), ListSortDirection.Ascending));
            foreach (var factAnalyzer in _factAnalyzers)
            {
                factAnalyzer.InitializeFacts();
            }
            #endregion

            #region Loads the Mitigation.
            _mitigations = new ObservableCollection<MitigationCostViewModel>();
            var mitigations = _model.Mitigations.OfType<MitigationCost>().ToArray();
            if (mitigations.Any())
            {
                foreach (var mitigation in mitigations)
                {
                    var mitigationViewModel = new MitigationCostViewModel(mitigation, null, this);
                    _mitigations.Add(mitigationViewModel);
                }
            }
            Mitigations = CollectionViewSource.GetDefaultView(_mitigations);
            Mitigations.SortDescriptions.Add(new SortDescription(nameof(MitigatedRiskViewModel.Name), ListSortDirection.Ascending));
            foreach (var mitigation in _mitigations)
            {
                mitigation.InitializeFacts();
            }
            #endregion

            #region Loads the Risks.
            _risks = new ObservableCollection<MitigatedRiskViewModel>();
            var mitigatedRisks = _model.Risks.OfType<MitigatedRisk>().ToArray();
            if (mitigatedRisks.Any())
            {
                foreach (var risk in mitigatedRisks)
                {
                    var riskViewModel = new MitigatedRiskViewModel(risk, null, this);
                    _risks.Add(riskViewModel);
                }
            }
            Risks = CollectionViewSource.GetDefaultView(_risks);
            Risks.SortDescriptions.Add(new SortDescription(nameof(MitigatedRiskViewModel.Name), ListSortDirection.Ascending));
            foreach (var risk in _risks)
            {
                risk.InitializeFacts();
            }
            #endregion

            #region Finalizes the loading.
            foreach (FactViewModel fact in Facts)
            {
                fact.InitializeRelated();
            }

            foreach (MitigatedRiskViewModel risk in Risks)
            {
                risk.InitializeMitigations();
            }

            foreach (MitigationCostViewModel mitigation in Mitigations)
            {
                mitigation.InitializeRelated();
            }
            #endregion
        }

        // Expose the underlying object if needed
        public RiskModel Model => _model;

        public RiskModelProperties Properties => _properties;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Risks management.
        private ObservableCollection<MitigatedRiskViewModel> _risks;

        public ICollectionView Risks { get; }

        public bool HasRisks => _risks.Any();

        public MitigatedRiskViewModel AddRisk(string name)
        {
            var risk = _model.AddRisk(name);
            var result = new MitigatedRiskViewModel(risk, null, this);
            _risks.Add(result);
            OnPropertyChanged(nameof(_risks));
            OnPropertyChanged(nameof(HasRisks));

            return result;
        }
        
        public void RemoveRisk(MitigatedRiskViewModel risk)
        {
            // Remove all components of the Risk.
            var components = risk.Components.OfType<NodeViewModel>().ToArray();
            if (components.Any())
            {
                foreach (var child in components)
                {
                    child.Delete();
                }
            }
            var facts = risk.Facts?.OfType<LinkedFactViewModel>().ToArray();
            if (facts?.Any() ?? false)
            {
                foreach (var fact in facts)
                {
                    risk.RemoveFact(fact.LinkedFact);
                }
            }
            var mitigations = risk.Mitigations?.OfType<AppliedMitigationViewModel>().ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                {
                    mitigation.Delete();
                }
            }

            _model.RemoveRisk(risk.Id);
            _risks.Remove(risk);
            OnPropertyChanged(nameof(_risks));
            OnPropertyChanged(nameof(HasRisks));

            if (risk == _selectedRisk)
            {
                 SelectedRisk = null;
            }
        }

        public MitigatedRiskViewModel? GetRisk(string name)
        {
            return _risks.FirstOrDefault(x => string.CompareOrdinal(x.Name, name) == 0);
        }

        private object? _selectedRisk;

        public object? SelectedRisk
        {
            get => _selectedRisk;
            set
            {
                if (_selectedRisk != value)
                {
                    _selectedRisk = value;
                    OnPropertyChanged(nameof(SelectedRisk));
                }
            }
        }
        #endregion

        #region Mitigations management.
        private ObservableCollection<MitigationCostViewModel> _mitigations;

        public ICollectionView Mitigations { get; }

        public bool HasMitigations => _mitigations.Any();

        public MitigationCostViewModel AddMitigation(string name)
        {
            var mitigation = _model.AddMitigation(name);
            var result = new MitigationCostViewModel(mitigation, null, this);
            _mitigations.Add(result);
            OnPropertyChanged(nameof(_mitigations)); 
            OnPropertyChanged(nameof(HasMitigations));

            return result;
        }

        public void RemoveMitigation(MitigationCostViewModel mitigation)
        {
            _model.RemoveMitigation(mitigation.Id);
            _mitigations.Remove(mitigation);
            OnPropertyChanged(nameof(_mitigations));
            OnPropertyChanged(nameof(HasMitigations));

            if (mitigation == _selectedMitigation)
            {
                SelectedMitigation = null;
            }
        }

        public MitigationCostViewModel? GetMitigation(string name)
        {
            return _mitigations.FirstOrDefault(x => string.CompareOrdinal(x.Name, name) == 0);
        }

        private object? _selectedMitigation;

        public object? SelectedMitigation
        {
            get => _selectedMitigation;
            set
            {
                if (_selectedMitigation != value)
                {
                    _selectedMitigation = value;
                    OnPropertyChanged(nameof(SelectedMitigation));
                }
            }
        }
        #endregion

        #region Facts management.
        public ObservableCollection<FactViewModel> Facts { get; }

        public FactViewModel? AddFact(Fact fact)
        {
            FactViewModel? result = null;

            if (Model.AddFact(fact))
            {
                result = new FactViewModel(fact, this);
                Facts.Add(result);
                OnPropertyChanged(nameof(Facts));
            }

            return result;
        }

        public void RemoveFact(FactViewModel fact)
        {
            Model.RemoveFact(fact.Fact);
            Facts.Remove(fact);
            OnPropertyChanged(nameof(Facts));

            if (fact == _selectedFact)
            {
                SelectedFact = null;
            }
        }

        private object? _selectedFact;

        public object? SelectedFact
        {
            get => _selectedFact;
            set
            {
                if (_selectedFact != value)
                {
                    _selectedFact = value;
                    OnPropertyChanged(nameof(SelectedFact));
                }
            }
        }
        #endregion

        #region Fact Analyzer management.
        private ObservableCollection<FactAnalyzerViewModel> _factAnalyzers { get; }

        public ICollectionView FactAnalyzers { get; }

        public bool HasFactAnalyzers => _factAnalyzers.Any();

        public FactAnalyzerViewModel? AddFactAnalyzer(string name)
        {
            var factAnalyzer = _model.AddFactAnalyzer(name);
            var result = new FactAnalyzerViewModel(factAnalyzer, null, this);
            _factAnalyzers.Add(result);
            OnPropertyChanged(nameof(FactAnalyzers));
            OnPropertyChanged(nameof(HasFactAnalyzers));

            return result;
        }

        public void RemoveFactAnalyzer(FactAnalyzerViewModel factAnalyzer)
        {
            _model.RemoveFactAnalyzer(factAnalyzer.Node.Id);
            _factAnalyzers.Remove(factAnalyzer);
            OnPropertyChanged(nameof(FactAnalyzers));
            OnPropertyChanged(nameof(HasFactAnalyzers));

            if (factAnalyzer == _selectedFactAnalyzer)
            {
                SelectedFactAnalyzer = null;
            }
        }

        private object? _selectedFactAnalyzer;

        public object? SelectedFactAnalyzer
        {
            get => _selectedFactAnalyzer;
            set
            {
                if (_selectedFactAnalyzer != value)
                {
                    _selectedFactAnalyzer = value;
                    OnPropertyChanged(nameof(SelectedFactAnalyzer));
                }
            }
        }
        #endregion

    }
}
