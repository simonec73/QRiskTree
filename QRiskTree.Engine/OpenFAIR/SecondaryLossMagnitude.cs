using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecondaryLossMagnitude : NodeWithFacts
    {
        public SecondaryLossMagnitude() : base(RangeType.Money)
        {
        }

        public SecondaryLossMagnitude(string name) : base(name, RangeType.Money)
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
