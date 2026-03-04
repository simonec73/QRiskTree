using QRiskTree.Engine;

namespace QRiskTreeExtensibilityTest.MyModel
{
    public class Impact : Node
    {
        public Impact() : base(RangeType.Money)
        {
        }

        public Impact(string name) : base(name, RangeType.Money)
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
