using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecondaryLossEventFrequency : NodeWithFacts
    {
        public SecondaryLossEventFrequency() : base(RangeType.Frequency)
        {
        }

        public SecondaryLossEventFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

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
