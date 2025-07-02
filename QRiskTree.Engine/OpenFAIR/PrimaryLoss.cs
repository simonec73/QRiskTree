using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PrimaryLoss : NodeWithFacts
    {
        internal PrimaryLoss() : base(RangeType.Money)
        {
        }

        internal PrimaryLoss(string name) : base(name, RangeType.Money)
        {
        }

        #region Loss Form management.
        [JsonProperty("form")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LossForm Form { get; set; } = LossForm.Undetermined;

        public PrimaryLoss Set(LossForm form)
        {
            Form = form;

            return this;
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return false;
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
