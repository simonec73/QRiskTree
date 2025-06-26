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
    public class ResistenceStrength : NodeWithFacts
    {
        public ResistenceStrength() : base(RangeType.Percentage)
        {
        }

        public ResistenceStrength(string name) : base(name, RangeType.Percentage)
        {
        }

        protected override bool IsValidChild(Node node)
        {
            return false;
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            // This value cannot be simulated. User must provide it.
            samples = null;
            return false;
        }
    }
}
