using Newtonsoft.Json;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecondaryLossEventFrequency : NodeWithFacts
    {
        internal SecondaryLossEventFrequency() : base(RangeType.Percentage)
        {
        }

        internal SecondaryLossEventFrequency(string name) : base(name, RangeType.Percentage)
        {
        }

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return false;
        }

        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;
            confidence = Confidence;

            return false;
        }
        #endregion
    }
}
