using QRiskTree.Engine;

namespace QRiskTreeExtensibilityTest.MyModel
{
    public class Risk : Node
    {
        public Risk() : base(RangeType.Money)
        {
        }

        public Risk(string name) : base(name, RangeType.Money)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return (node is Probability && !(_children?.OfType<Probability>().Any() ?? false)) ||
                (node is Impact && !(_children?.OfType<Impact>().Any() ?? false));
        }

        protected override bool Simulate(int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples)
        {
            var result = false;
            samples = null;

            var probability = _children?.OfType<Probability>().FirstOrDefault();
            var impact = _children?.OfType<Impact>().FirstOrDefault();

            if (probability != null && impact != null)
            {
                if (probability.SimulateAndGetSamples(out var pSamples, minPercentile, maxPercentile, iterations, container) &&
                    pSamples != null && pSamples.Length == iterations &&
                    impact.SimulateAndGetSamples(out var iSamples, minPercentile, maxPercentile, iterations, container) &&
                    iSamples != null && iSamples.Length == iterations)
                {
                    samples = new double[iterations];
                    for (int i = 0; i < iterations; i++)
                    {
                        samples[i] = pSamples[i] * iSamples[i];
                    }

                    result = true;
                }
            }

            return result;
        }
    }
}
