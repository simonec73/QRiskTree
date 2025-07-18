using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.ExtendedModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AppliedMitigation : NodeWithFacts
    {
        internal AppliedMitigation() : base(RangeType.Percentage)
        {
            // Default constructor for serialization purposes.
        }

        internal AppliedMitigation(MitigationCost mitigation) : base(RangeType.Percentage)
        {
            _mitigationCostId = mitigation.Id;
            Name = mitigation.Name;
            Description = mitigation.Description;
        }

        #region Properties.
        [JsonProperty("mitigationCostId", Order = 9)]
        private Guid _mitigationCostId { get; set; }

        public Guid MitigationCostId => _mitigationCostId;

        public MitigationCost? MitigationCost => RiskModel.Instance.GetMitigation(_mitigationCostId);

        public bool IsEnabled => MitigationCost?.IsEnabled ?? false;
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
