using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LossEventFrequency : NodeWithFacts
    {
        public LossEventFrequency() : base(RangeType.Frequency)
        {
        }

        public LossEventFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return (node is ThreatEventFrequency && !(_children?.OfType<ThreatEventFrequency>().Any() ?? false)) || 
                (node is Vulnerability && !(_children?.OfType<Vulnerability>().Any() ?? false));
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            var threatEventFrequency = _children?.OfType<ThreatEventFrequency>().FirstOrDefault();
            var vulnerability = _children?.OfType<Vulnerability>().FirstOrDefault();

            if (threatEventFrequency != null && vulnerability != null)
            {
                if (Simulate(threatEventFrequency, iterations, out var tefSamples) && 
                    (tefSamples?.Length ?? 0) == iterations &&
                    Simulate(vulnerability, iterations, out var vSamples) && 
                    (vSamples?.Length ?? 0) == iterations)
                {
                    // Assign to samples the product of tefSamples and vSamples.
                    samples = new double[iterations];
                    for (int i = 0; i < iterations; i++)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        samples[i] = tefSamples[i] * vSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }

                    result = true;
                }
            }

            return result;
        }
    }
}
