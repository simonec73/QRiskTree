using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.OpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Risk : NodeWithFacts
    {
        public Risk() : base(RangeType.Money)
        {
        } 

        public Risk(string name) : base(name, RangeType.Money)
        {
        }

        #region Children management.
        public LossEventFrequency AddLossEventFrequency()
        {
            var result = new LossEventFrequency();
            if (!Add(result))
            {
                throw new InvalidOperationException("A LossEventFrequency node already exists as a child of this node.");
            }
            return result;
        }

        public LossEventFrequency AddLossEventFrequency(string name)
        {
            var result = new LossEventFrequency(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("A LossEventFrequency node already exists as a child of this node.");
            }
            return result;
        }

        public LossEventFrequency? GetLossEventFrequency()
        {
            return _children?.OfType<LossEventFrequency>().FirstOrDefault();
        }

        public LossMagnitude AddLossMagnitude()
        {
            var result = new LossMagnitude();
            if (!Add(result))
            {
                throw new InvalidOperationException("A LossMagnitude node already exists as a child of this node.");
            }
            return result;
        }

        public LossMagnitude AddLossMagnitude(string name)
        {
            var result = new LossMagnitude(name);
            if (!Add(result))
            {
                throw new InvalidOperationException("A LossMagnitude node already exists as a child of this node.");
            }
            return result;
        }

        public LossMagnitude? GetLossMagnitude()
        {
            return _children?.OfType<LossMagnitude>().FirstOrDefault();
        }
        #endregion

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return (node is LossEventFrequency && !(_children?.OfType<LossEventFrequency>().Any() ?? false)) || 
                (node is LossMagnitude && !(_children?.OfType<LossMagnitude>().Any() ?? false));
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            var lossEventFrequency = _children?.OfType<LossEventFrequency>().FirstOrDefault();
            var lossMagnitude = _children?.OfType<LossMagnitude>().FirstOrDefault();

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
