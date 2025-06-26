using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecondaryRisk : NodeWithFacts
    {
        public SecondaryRisk() : base(RangeType.Money)
        {
        }

        public SecondaryRisk(string name) : base(name, RangeType.Money)
        {
        }

        [JsonProperty("form")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SecondaryLossForm Form { get; set; } = SecondaryLossForm.Undetermined;

        protected override bool IsValidChild(Node node)
        {
            return (node is SecondaryLossEventFrequency && !(_children?.OfType<SecondaryLossEventFrequency>().Any() ?? false)) || 
                (node is SecondaryLossMagnitude && !(_children?.OfType<SecondaryLossMagnitude>().Any() ?? false));
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            // We must determine the statistics for the current node by 
            var lossEventFrequency = _children?.OfType<SecondaryLossEventFrequency>().FirstOrDefault();
            var lossMagnitude = _children?.OfType<SecondaryLossMagnitude>().FirstOrDefault();

            if (lossEventFrequency != null && lossMagnitude != null)
            {
                if (Simulate(lossEventFrequency, iterations, out var lefSamples) && 
                    (lefSamples?.Length ?? 0) == iterations &&
                    Simulate(lossMagnitude, iterations, out var lmSamples) && 
                    (lmSamples?.Length ?? 0) == iterations)
                {
                    // Assign to samples the product of lefSamples and lmSamples.
                    samples = new double[iterations];
                    for (int i = 0; i < iterations; i++)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        samples[i] = lefSamples[i] * lmSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }

                    result = true;
                }
            }

            return result;
        }
    }
}
