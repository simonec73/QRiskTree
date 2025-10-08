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

        #region Baselines management.
        private double[]? _implementationBaseline;
        private double[]? _operationBaseline;

        /// <summary>
        /// Flags indicating whether this risk has defined baselines.
        /// </summary>
        public bool HasBaselines => (_implementationBaseline?.Any() ?? false) && (_operationBaseline?.Any() ?? false);

        /// <summary>
        /// Gets the implementation baseline values.
        /// </summary>
        public double[]? ImplementationBaseline => _implementationBaseline?.ToArray();

        /// <summary>
        /// Gets the operation baseline values.
        /// </summary>
        public double[]? OperationBaseline => _operationBaseline?.ToArray();

        /// <summary>
        /// Gets the confidence level of the implementation baseline.
        /// </summary>
        public Confidence ImplementationBaselineConfidence { get; private set; } = Confidence.Low;

        /// <summary>
        /// Gets the confidence level of the operation baseline.
        /// </summary>
        public Confidence OperationBaselineConfidence { get; private set; } = Confidence.Low;

        /// <summary>
        /// Sets the baseline values and their associated confidence levels for implementation and operation metrics.
        /// </summary>
        /// <param name="implementationBaseline">An array of doubles representing the baseline values for implementation metrics. Cannot be null or empty.</param>
        /// <param name="implementationConfidence">The confidence level associated with the implementation baseline values.</param>
        /// <param name="operationBaseline">An array of doubles representing the baseline values for operation metrics. Cannot be null or empty.</param>
        /// <param name="operationConfidence">The confidence level associated with the operation baseline values.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="implementationBaseline"/> or <paramref name="operationBaseline"/> is null or
        /// empty.</exception>
        public void SetBaselines(double[] implementationBaseline, Confidence implementationConfidence, double[] operationBaseline, Confidence operationConfidence)
        {
            if (implementationBaseline == null || implementationBaseline.Length == 0)
                throw new ArgumentException("Implementation baseline cannot be null or empty.", nameof(implementationBaseline));
            if (operationBaseline == null || operationBaseline.Length == 0)
                throw new ArgumentException("Operation baseline cannot be null or empty.", nameof(operationBaseline));

            _implementationBaseline = implementationBaseline.ToArray();
            ImplementationBaselineConfidence = implementationConfidence;
            _operationBaseline = operationBaseline.ToArray();
            OperationBaselineConfidence = operationConfidence;
        }

        /// <summary>
        /// Clear the baselines values and confidence.
        /// </summary>
        public void ClearBaselines()
        {
            _implementationBaseline = null;
            _operationBaseline = null;
            ImplementationBaselineConfidence = Confidence.Low;
            OperationBaselineConfidence = Confidence.Low;
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return false; // No children allowed for Mitigation nodes
        }

        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;
            confidence = Confidence;

            return false;
        }
        #endregion
    }
}
