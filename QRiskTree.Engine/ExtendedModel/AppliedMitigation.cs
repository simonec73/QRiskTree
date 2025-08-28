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
