using QRiskTree.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QRiskTreeEditor.ViewModels
{
    internal class ThreatEventFrequencyViewModel : NodeViewModel
    {
        public ThreatEventFrequencyViewModel(ThreatEventFrequency node, NodeViewModel parent, RiskModelViewModel model)
            : base(node, parent, model)
        {
        }

        #region Children management.
        public ContactFrequencyViewModel? AddContactFrequency(string name)
        {
            ContactFrequencyViewModel? result = null;

            if (_node is ThreatEventFrequency threatEventFrequency)
            {
                var cf = threatEventFrequency.AddContactFrequency(name);
                if (cf != null)
                {
                    result = new ContactFrequencyViewModel(cf, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(_components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }
            
            return result;
        }

        public ProbabilityOfActionViewModel? AddProbabilityOfAction(string name)
        {
            ProbabilityOfActionViewModel? result = null;

            if (_node is ThreatEventFrequency threatEventFrequency)
            {
                var poa = threatEventFrequency.AddProbabilityOfAction(name);
                if (poa != null)
                {
                    result = new ProbabilityOfActionViewModel(poa, this, _model);
                    _components.Add(result);
                    result.InitializeFacts();
                    OnPropertyChanged(nameof(_components));
                    OnPropertyChanged(nameof(HasComponents));
                    OnPropertyChanged(nameof(HasChildren));
                }
            }
            
            return result;
        }
        #endregion
    }
}
