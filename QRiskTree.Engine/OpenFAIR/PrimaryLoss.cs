using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PrimaryLoss : NodeWithFacts
    {
        public PrimaryLoss() : base(RangeType.Money)
        {
        }

        public PrimaryLoss(string name) : base(name, RangeType.Money)
        {
        }

        [JsonProperty("form")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PrimaryLossForm Form { get; set; } = PrimaryLossForm.Undetermined;

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
    }
}
