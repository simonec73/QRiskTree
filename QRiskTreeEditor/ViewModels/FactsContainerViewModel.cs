using QRiskTree.Engine.Facts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace QRiskTreeEditor.ViewModels
{
    internal abstract class FactsContainerViewModel
    {
        protected readonly NodeWithFacts _node;
        protected readonly NodeViewModel? _parent;
        protected readonly RiskModelViewModel _model;

        public FactsContainerViewModel(NodeWithFacts node, NodeViewModel? parent, RiskModelViewModel model)
        {
            _node = node;
            _parent = parent;
            _model = model;

            _facts = new ObservableCollection<LinkedFactViewModel>();
            Facts = CollectionViewSource.GetDefaultView(_facts);
            Facts.SortDescriptions.Add(new SortDescription(nameof(FactViewModel.Context), ListSortDirection.Ascending));
            Facts.SortDescriptions.Add(new SortDescription(nameof(FactViewModel.Name), ListSortDirection.Ascending));
        }

        protected abstract void RaiseUpdateEvent(string propertyName);

        protected ObservableCollection<LinkedFactViewModel> _facts { get; }

        [Browsable(false)]
        public Guid Id => _node.Id;

        [Category("General")]
        public string NodeType => _node.GetType().Name.AddSpacesToCamelCase();

        [Browsable(false)]
        public ICollectionView? Facts { get; private set; }

        [Browsable(false)]
        public virtual bool HasChildren => _facts.Any();

        [Browsable(false)]
        public bool HasFacts => _facts.Any();

        public void InitializeFacts()
        {
            var facts = _node.Facts?.ToArray();
            if (facts?.Any() ?? false)
            {
                var modelFacts = _model.Facts?.ToArray();
                foreach (var fact in facts)
                {
                    var modelFact = modelFacts?.FirstOrDefault(x => x.Id == fact.Id);
                    if (modelFact == null)
                    {
                        modelFact = _model.AddFact(fact);
                    }

                    if (modelFact != null)
                    {
                        _facts.Add(new LinkedFactViewModel(modelFact, this));
                    }
                }
            }
        }

        internal bool AddFact(FactViewModel fact)
        {
            if (_node.Add(fact.Fact))
            {
                _facts.Add(new LinkedFactViewModel(fact, this));
                fact.AddRelated(this);
                RaiseUpdateEvent(nameof(Facts));
                RaiseUpdateEvent(nameof(HasFacts));
                RaiseUpdateEvent(nameof(HasChildren));
                return true;
            }

            return false;
        }

        internal bool RemoveFact(FactViewModel fact)
        {
            if (_node.Remove(fact.Fact))
            {
                var linkedFact = _facts.FirstOrDefault(x => x.LinkedFact.Id == fact.Id);
                if (linkedFact != null)
                {
                    _facts.Remove(linkedFact);
                    fact.RemoveRelated(this);
                }
                RaiseUpdateEvent(nameof(Facts));
                RaiseUpdateEvent(nameof(HasFacts));
                RaiseUpdateEvent(nameof(HasChildren));
                return true;
            }

            return false;
        }
    }
}
