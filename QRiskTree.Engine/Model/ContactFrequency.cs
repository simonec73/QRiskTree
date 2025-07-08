using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ContactFrequency : NodeWithFacts
    {
        internal ContactFrequency() : base(RangeType.Frequency)
        {
        }

        internal ContactFrequency(string name) : base(name, RangeType.Frequency)
        {
        }

        /// <summary>
        /// Types of contact.
        /// </summary>
        [JsonProperty("contactType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ContactType ContactType { get; set; }

        #region Member overrides.
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
        #endregion
    }
}
