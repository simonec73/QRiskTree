using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System.Xml.Linq;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ThreatEventFrequency : NodeWithFacts
    {
        internal ThreatEventFrequency() : base(RangeType.Frequency)
        {
        }

        internal ThreatEventFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

        #region Children management.
        public ContactFrequency AddContactFrequency()
        {
            var result = new ContactFrequency();
            if (!Add(result))
            {
                throw new InvalidOperationException("ContactFrequency node creation failed.");
            }
            return result;
        }

        public ContactFrequency AddContactFrequency(string name)
        {
            var result = new ContactFrequency(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("ContactFrequency node creation failed.");
            }
            return result;
        }

        public ContactFrequency? GetContactFrequency()
        {
            return _children?.OfType<ContactFrequency>().FirstOrDefault();
        }

        public ProbabilityOfAction AddProbabilityOfAction()
        {
            var result = new ProbabilityOfAction();
            if (!Add(result))
            {
                throw new InvalidOperationException("ProbabilityOfAction node creation failed.");
            }
            return result;
        }

        public ProbabilityOfAction AddProbabilityOfAction(string name)
        {
            var result = new ProbabilityOfAction(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("ProbabilityOfAction node creation failed.");
            }
            return result;
        }

        public ProbabilityOfAction? GetProbabilityOfAction()
        {
            return _children?.OfType<ProbabilityOfAction>().FirstOrDefault();
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return (node is ContactFrequency && !(_children?.OfType<ContactFrequency>().Any() ?? false)) ||
                (node is ProbabilityOfAction && !(_children?.OfType<ProbabilityOfAction>().Any() ?? false));
        }

        protected override bool HasAllChildren()
        {
            return (_children?.OfType<ContactFrequency>().Any() ?? false) &&
                (_children?.OfType<ProbabilityOfAction>().Any() ?? false);
        }

        protected override bool Simulate(int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples)
        {
            var result = false;
            samples = null;

            var contactFrequency = _children?.OfType<ContactFrequency>().FirstOrDefault();
            var probabilityOfAction = _children?.OfType<ProbabilityOfAction>().FirstOrDefault();

            if (contactFrequency != null && probabilityOfAction != null)
            {
                if (contactFrequency.SimulateAndGetSamples(out var cSamples, minPercentile, maxPercentile, iterations, container) && 
                    (cSamples?.Length ?? 0) == iterations &&
                    probabilityOfAction.SimulateAndGetSamples(out var pSamples, minPercentile, maxPercentile, iterations, container) && 
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
        #endregion
    }
}
