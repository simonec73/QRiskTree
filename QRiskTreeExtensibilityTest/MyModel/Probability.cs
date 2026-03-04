using QRiskTree.Engine;

namespace QRiskTreeExtensibilityTest.MyModel
{
    public class Probability : Node
    {
        public Probability() : base(RangeType.Percentage)
        {
        }

        public Probability(string name) : base(name, RangeType.Percentage)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return false;
        }

        protected override bool? CanBeSimulated()
        {
            return true;
        }

        protected override bool Simulate(int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;

            return false;
        }
    }
}
