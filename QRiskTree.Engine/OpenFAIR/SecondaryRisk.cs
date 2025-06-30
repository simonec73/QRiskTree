using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecondaryRisk : NodeWithFacts
    {
        internal SecondaryRisk() : base(RangeType.Money)
        {
        }

        internal SecondaryRisk(string name) : base(name, RangeType.Money)
        {
        }

        #region Loss Form management.
        [JsonProperty("form")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SecondaryLossForm Form { get; set; } = SecondaryLossForm.Undetermined;
        
        public SecondaryRisk Set(SecondaryLossForm form)
        {
            Form = form;
            return this;
        }
        #endregion

        #region Children management.
        public SecondaryLossEventFrequency AddSecondaryLossEventFrequency()
        {
            var result = new SecondaryLossEventFrequency();
            if (!Add(result))
            {
                throw new InvalidOperationException("A SecondaryLossEventFrequency node already exists as a child of this node.");
            }
            return result;
        }

        public SecondaryLossEventFrequency AddSecondaryLossEventFrequency(string name)
        {
            var result = new SecondaryLossEventFrequency(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("A SecondaryLossEventFrequency node already exists as a child of this node.");
            }
            return result;
        }

        public SecondaryLossEventFrequency? GetSecondaryLossEventFrequency()
        {
            return _children?.OfType<SecondaryLossEventFrequency>().FirstOrDefault();
        }

        public SecondaryLossMagnitude AddSecondaryLossMagnitude()
        {
            var result = new SecondaryLossMagnitude();
            if (!Add(result))
            {
                throw new InvalidOperationException("A SecondaryLossMagnitude node already exists as a child of this node.");
            }
            return result;
        }

        public SecondaryLossMagnitude AddSecondaryLossMagnitude(string name)
        {
            var result = new SecondaryLossMagnitude(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("A SecondaryLossMagnitude node already exists as a child of this node.");
            }
            return result;
        }

        public SecondaryLossMagnitude? GetSecondaryLossMagnitude()
        {
            return _children?.OfType<SecondaryLossMagnitude>().FirstOrDefault();
        }
        #endregion

        #region Member overrides.
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
        #endregion
    }
}
