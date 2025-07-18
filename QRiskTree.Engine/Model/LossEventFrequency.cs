using Newtonsoft.Json;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LossEventFrequency : NodeWithFacts
    {
        internal LossEventFrequency() : base(RangeType.Frequency)
        {
        }

        internal LossEventFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

        #region Children management.
        public ThreatEventFrequency AddThreatEventFrequency()
        {
            var result = new ThreatEventFrequency();

            if (!Add(result))
            { 
                throw new InvalidOperationException("A ThreatEventFrequency node already exists as a child of this node.");
            }

            return result;
        }

        public ThreatEventFrequency AddThreatEventFrequency(string name)
        {
            var result = new ThreatEventFrequency(name);

            if (!Add(result))
            {
                throw new InvalidOperationException("A ThreatEventFrequency node already exists as a child of this node.");
            }

            return result;
        }

        public ThreatEventFrequency? GetThreatEventFrequency()
        {
            return _children?.OfType<ThreatEventFrequency>().FirstOrDefault();
        }

        public Vulnerability AddVulnerability()
        {
            var result = new Vulnerability();
            if (!Add(result))
            {
                throw new InvalidOperationException("A Vulnerability node already exists as a child of this node.");
            }
            return result;
        }
        
        public Vulnerability AddVulnerability(string name)
        {
            var result = new Vulnerability(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("A Vulnerability node already exists as a child of this node.");
            }
            return result;
        }

        public Vulnerability? GetVulnerability()
        {
            return _children?.OfType<Vulnerability>().FirstOrDefault();
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return (node is ThreatEventFrequency && !(_children?.OfType<ThreatEventFrequency>().Any() ?? false)) || 
                (node is Vulnerability && !(_children?.OfType<Vulnerability>().Any() ?? false));
        }

        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            var result = false;
            samples = null;
            confidence = Confidence;

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

                    confidence = threatEventFrequency.Confidence < vulnerability.Confidence
                        ? threatEventFrequency.Confidence
                        : vulnerability.Confidence;

                    result = true;
                }
            }

            return result;
        }
        #endregion
    }
}
