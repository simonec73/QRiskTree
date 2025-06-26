using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ThreatEventFrequency : NodeWithFacts
    {
        public ThreatEventFrequency() : base(RangeType.Frequency)
        {
        }

        public ThreatEventFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return (node is ContactFrequency && !(_children?.OfType<ContactFrequency>().Any() ?? false)) ||
                (node is ProbabilityOfAction && !(_children?.OfType<ProbabilityOfAction>().Any() ?? false));
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            var contactFrequency = _children?.OfType<ContactFrequency>().FirstOrDefault();
            var probabilityOfAction = _children?.OfType<ProbabilityOfAction>().FirstOrDefault();

            if (contactFrequency != null && probabilityOfAction != null)
            {
                if (Simulate(contactFrequency, iterations, out var cSamples) && 
                    (cSamples?.Length ?? 0) == iterations &&
                    Simulate(probabilityOfAction, iterations, out var pSamples) && 
                    (pSamples?.Length ?? 0) == iterations)
                {
                    // Assign to samples the product of cSamples and pSamples.
                    samples = new double[iterations];
                    for (int i = 0; i < iterations; i++)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        samples[i] = cSamples[i] * pSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }

                    result = true;
                }
            }

            return result;
        }
    }
}
