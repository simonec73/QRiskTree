using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ExtendedModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MitigationCost : NodeWithFacts
    {
        internal MitigationCost() : base(RangeType.Money)
        {
        }

        internal MitigationCost(string name) : base(name, RangeType.Money)
        {
        }

        #region Properties.
        [JsonProperty("enabled", Order = 5)]
        private bool _isEnabled { get; set; } = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    Update();
                }
            }
        }

        [JsonProperty("operationalCosts", Order = 40)]
        private Range? _operationalCosts { get; set; }

        public Range? OperationCosts
        {
            get => _operationalCosts;
            set
            {
                if (value != null)
                {
                    _operationalCosts = value;
                    Update();
                }
            }
        }
        #endregion

        #region Public methods.
        public MitigationCost SetOperationCosts(double min, double mode, double max, Confidence confidence)
        {
            if (_operationalCosts == null)
                _operationalCosts = new Range(RangeType.Money);
            _operationalCosts.Set(min, mode, max, confidence);
            Update();
            return this;
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return false; // No children allowed for Mitigation nodes
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;
            return false;
        }
        #endregion
    }
}
