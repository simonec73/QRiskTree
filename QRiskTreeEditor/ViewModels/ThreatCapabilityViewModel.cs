using QRiskTree.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTreeEditor.ViewModels
{
    internal class ThreatCapabilityViewModel : NodeViewModel
    {
        public ThreatCapabilityViewModel(ThreatCapability node, NodeViewModel parent, RiskModelViewModel model)
            : base(node, parent, model)
        {
        }
    }
}
