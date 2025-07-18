using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ResistenceStrength : NodeWithFacts
    {
        internal ResistenceStrength() : base(RangeType.Percentage)
        {
        }

        internal ResistenceStrength(string name) : base(name, RangeType.Percentage)
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
