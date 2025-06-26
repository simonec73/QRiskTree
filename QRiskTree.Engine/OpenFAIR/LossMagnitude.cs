using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LossMagnitude : NodeWithFacts
    {
        public LossMagnitude() : base(RangeType.Money)
        {
        }

        public LossMagnitude(string name) : base(name, RangeType.Money)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return (node is PrimaryLoss) || (node is SecondaryRisk);
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = true;
            samples = new double[iterations];

            var primaryLosses = _children?.OfType<PrimaryLoss>().ToArray();
            var secondaryRisks = _children?.OfType<SecondaryRisk>().ToArray();

            if (primaryLosses?.Any() ?? false)
            {
                foreach (var primaryLoss in primaryLosses)
                {
                    if (Simulate(primaryLoss, iterations, out var plSamples) && (plSamples?.Length ?? 0) == iterations)
                    {
                        for (int i = 0; i < iterations; i++)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            samples[i] += plSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
                    }
                    else
                    {
                        result = false;
                        break;
                    }
                }
            }

            if (result && (secondaryRisks?.Any() ?? false))
            {
                foreach (var secondaryRisk in secondaryRisks)
                {
                    if (Simulate(secondaryRisk, iterations, out var srSamples) && (srSamples?.Length ?? 0) == iterations)
                    {
                        for (int i = 0; i < iterations; i++)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            samples[i] += srSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
                    }
                    else
                    {                         
                        result = false;
                        break;
                    }
                }
            }

            if (!result)
            {
                samples = null; // Reset samples if simulation failed
            }

            return result;
        }
    }
}
