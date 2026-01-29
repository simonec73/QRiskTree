using QRiskTreeEditor.Importers;
using System.ComponentModel;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class LinkedNodeViewModel : INotifyPropertyChanged
    {
        private readonly FactsContainerViewModel _node;
        private readonly FactViewModel? _fact;
        private readonly MitigationCostViewModel? _mitigationCost;

        public LinkedNodeViewModel(FactsContainerViewModel node, FactViewModel parent)
        {
            _node = node;
            if (node is INotifyPropertyChanged notifyNode)
            {
                notifyNode.PropertyChanged += OnPropertyChanged;
            }
            _fact = parent;
        }

        public LinkedNodeViewModel(MitigatedRiskViewModel node, MitigationCostViewModel parent)
        {
            _node = node;
            node.PropertyChanged += OnPropertyChanged;
            _mitigationCost = parent;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NodeType" ||  e.PropertyName == "Name" ||
                e.PropertyName == "Description" || e.PropertyName == "Min" ||
                e.PropertyName == "Mode" || e.PropertyName == "Max" ||
                e.PropertyName == "Confidence" || e.PropertyName == "CreatedBy" ||
                e.PropertyName == "CreatedOn" || e.PropertyName == "ModifiedBy" ||
                e.PropertyName == "ModifiedOn" || e.PropertyName == string.Empty)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
        }

        // Expose the underlying object if needed
        [Browsable(false)]
        public FactsContainerViewModel LinkedNode => _node;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Delete()
        {
            if (_node is MitigatedRiskViewModel mitigatedRisk && _mitigationCost != null)
            {
                var appliedMitigation = mitigatedRisk.Mitigations.OfType<AppliedMitigationViewModel>()
                    .FirstOrDefault(x => x.MitigationCostId == _mitigationCost.Id);
                appliedMitigation?.Delete();
            }
            else if (_node is FactsContainerViewModel factsContainer && _fact != null)
            {
                var linkedFact = factsContainer?.Facts?.OfType<LinkedFactViewModel>()
                    .FirstOrDefault(x => x.LinkedFact.Id == _fact.Id);
                linkedFact?.Delete();
            }

            //_fact?.RemoveRelated(this);
            //_mitigationCost?.RemoveRelated(this);
        }

        #region Properties.
        [Browsable(false)]
        public Guid Id => _node.Id;

        [Category("General")]
        [PT.SortIndex(2)]
        public string NodeType => _node.NodeType;

        [Category("General")]
        [PT.SortIndex(0)]
        public string? Name
        {
            get
            {
                var result = string.Empty;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.Name;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.Name;
                }

                return result;
            }
        }

        [Category("General")]
        [PT.SortIndex(2)]
        public string? Description
        {
            get
            {
                var result = string.Empty;
                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.Description;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.Description;
                }

                return result;
            }
        }

        [Category("Range")]
        [DisplayName("Minimum Value")]
        [PT.SortIndex(100)]
        public string Min
        {
            get
            {
                var result = string.Empty;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.FormattedMin;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.FormattedMin;
                }

                return result;
            }
        }

        [Category("Range")]
        [DisplayName("Mode Value")]
        [PT.SortIndex(101)]
        public string Mode
        {
            get
            {
                var result = string.Empty;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.FormattedMode;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.FormattedMode;
                }

                return result;
            }
        }

        [Category("Range")]
        [DisplayName("Maximum Value")]
        [PT.SortIndex(102)]
        public string Max
        {
            get
            {
                var result = string.Empty;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.FormattedMax;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.FormattedMax;
                }

                return result;
            }
        }

        [Category("Range")]
        [PT.SortIndex(103)]
        public string Confidence
        {
            get
            {
                var result = string.Empty;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.Confidence.ToString();
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.Confidence.ToString();
                }

                return result;
            }
        }

        [Category("Range")]
        [DisplayName("Range set by User")]
        [PT.SortIndex(104)]
        public bool IsSetByUser
        {
            get
            {
                var result = false;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.IsSetByUser;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.IsSetByUser;
                }

                return result;
            }
        }


        [Category("Update")]
        [PT.SortIndex(10000)]
        public string? CreatedBy
        {
            get
            {
                string? result = null;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.CreatedBy;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.CreatedBy;
                }

                return result;
            }
        }

        [Category("Update")]
        [PT.SortIndex(10001)]
        public DateTime CreatedOn
        {
            get
            {
                var result = DateTime.Today;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.CreatedOn;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.CreatedOn;
                }

                return result;
            }
        }

        [Category("Update")]
        [PT.SortIndex(10002)]
        public string? ModifiedBy
        {
            get
            {
                string? result = null;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.ModifiedBy;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.ModifiedBy;
                }

                return result;
            }
        }


        [Category("Update")]
        [PT.SortIndex(10004)]
        public DateTime ModifiedOn
        {
            get
            {
                var result = DateTime.Today;

                if (_node is NodeViewModel nodeViewModel)
                {
                    result = nodeViewModel.ModifiedOn;
                }
                else if (_node is AppliedMitigationViewModel appliedMitigationViewModel)
                {
                    result = appliedMitigationViewModel.ModifiedOn;
                }

                return result;
            }
        }
        #endregion
    }
}
