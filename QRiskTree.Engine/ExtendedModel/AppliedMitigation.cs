using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System.Runtime.Serialization;

namespace QRiskTree.Engine.ExtendedModel
{
    /// <summary>
    /// Represents a mitigation that has been applied to a risk model, including its associated cost and state.
    /// </summary>
    /// <remarks>This class provides information about a specific mitigation applied to a risk model, such as
    /// its cost,  whether it is enabled, and its relationship to the risk model. Instances of this class are typically 
    /// created internally and are not intended to be instantiated directly by external callers.</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class AppliedMitigation : NodeWithFacts
    {
        internal AppliedMitigation() : base(RangeType.Percentage)
        {
            // Default constructor for serialization purposes.
        }

        internal AppliedMitigation(RiskModel model, MitigationCost mitigation) : base(RangeType.Percentage)
        {
            _riskModelId = model.Id;
            _mitigationCostId = mitigation.Id;
            Name = mitigation.Name;
            Description = mitigation.Description;
        }

        #region Properties.
        [JsonProperty("riskModelId", Order = 8)]
        private Guid _riskModelId { get; set; }

        [JsonProperty("mitigationCostId", Order = 9)]
        private Guid _mitigationCostId { get; set; }

        /// <summary>
        /// Identifier of the Mitigation Cost this Applied Mitigation refers to.
        /// </summary>
        public Guid MitigationCostId => _mitigationCostId;

        /// <summary>
        /// Mitigation Cost this Applied Mitigation refers to.
        /// </summary>
        public MitigationCost? MitigationCost => RiskModel.Get(_riskModelId)?.GetMitigation(_mitigationCostId);

        /// <summary>
        /// Indicates whether the mitigation is enabled.
        /// </summary>
        public bool IsEnabled => MitigationCost?.IsEnabled ?? false;

        [JsonProperty("auxiliary")]
        private bool _auxiliary { get; set; } = false;

        /// <summary>
        /// Flags the Applied Mitigation as auxiliary.
        /// </summary>
        /// <remarks>Auxiliary mitigations have no effect on the mitigation, but they increase the cost.</remarks>
        public bool IsAuxiliary
        {
            get => _auxiliary;
            set
            {
                if (_auxiliary != value)
                {
                    _auxiliary = value;
                    Update();
                }
            }
        }
        #endregion

        #region Baseline management.
        private double[]? _baseline;

        /// <summary>
        /// Flags indicating whether this risk has a defined baseline.
        /// </summary>
        public bool HasBaseline => _baseline?.Any() ?? false;

        /// <summary>
        /// Gets the baseline values.
        /// </summary>
        public double[]? Baseline => _baseline?.ToArray();

        /// <summary>
        /// Set the baseline values and confidence.
        /// </summary>
        /// <param name="values">Baseline values.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> is null or empty.</exception>
        public void SetBaseline(double[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Baseline values cannot be null or empty.", nameof(values));

            _baseline = values.ToArray();
        }

        /// <summary>
        /// Clear the baseline values and confidence.
        /// </summary>
        public void ClearBaseline()
        {
            _baseline = null;
        }
        #endregion

        #region Internal Auxiliary Methods.
        internal void AssignModel(RiskModel model)
        {
            if (model != null)
            {
                _riskModelId = model.Id;
            }
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return false; // No children allowed for Applied Mitigation nodes
        }

        protected override bool Simulate(int minPercentile, int maxPercentile, uint iterations, 
            ISimulationContainer? container, out double[]? samples)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;

            return false;
        }

        protected override bool? CanBeSimulated()
        {
            return _auxiliary ? true : null;
        }
        #endregion
    }
}
