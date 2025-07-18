using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LossMagnitude : NodeWithFacts
    {
        internal LossMagnitude() : base(RangeType.Money)
        {
        }

        internal LossMagnitude(string name) : base(name, RangeType.Money)
        {
        }

        #region Children management.
        public PrimaryLoss AddPrimaryLoss()
        {
            var result = new PrimaryLoss();
            if (!Add(result))
            {
                throw new InvalidOperationException("PrimaryLoss node creation failed.");
            }
            return result;
        }

        public PrimaryLoss AddPrimaryLoss(string name)
        {
            var result = new PrimaryLoss(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("PrimaryLoss node creation failed.");
            }
            return result;
        }

        public PrimaryLoss? GetPrimaryLoss()
        {
            return _children?.OfType<PrimaryLoss>().FirstOrDefault();
        }

        public SecondaryRisk AddSecondaryRisk()
        {
            var result = new SecondaryRisk();
            if (!Add(result))
            {
                throw new InvalidOperationException("SecondaryRisk node creation failed.");
            }
            return result;
        }

        public SecondaryRisk AddSecondaryRisk(string name)
        {
            var result = new SecondaryRisk(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("SecondaryRisk node creation failed.");
            }
            return result;
        }

        public SecondaryRisk? GetSecondaryRisk()
        {
            return _children?.OfType<SecondaryRisk>().FirstOrDefault();
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return (node is PrimaryLoss) || (node is SecondaryRisk);
        }

        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            var result = true;
            samples = new double[iterations];
            confidence = Confidence.High;

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

                        confidence = primaryLoss.Confidence < confidence
                            ? primaryLoss.Confidence
                            : confidence;
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

                        confidence = secondaryRisk.Confidence < confidence
                            ? secondaryRisk.Confidence
                            : confidence;
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
        #endregion
    }
}
