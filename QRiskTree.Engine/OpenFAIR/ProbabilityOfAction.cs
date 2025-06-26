using MathNet.Numerics.Financial;
using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProbabilityOfAction : NodeWithFacts
    {
        public ProbabilityOfAction() : base(RangeType.Percentage)
        {
        }

        public ProbabilityOfAction(string name) : base(name, RangeType.Percentage)
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
