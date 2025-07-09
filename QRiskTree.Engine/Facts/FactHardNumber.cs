using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FactHardNumber : Fact
    {
        private FactHardNumber() : base("Unknown", "Unknown", "Unknown")
        {

        }

        public FactHardNumber(string context, string source, string name, double value)
            : base(context, source, name)
        {
            _value = value;
        }

        public FactHardNumber(FactRange factRange)
            : this(factRange.Context, factRange.Source, factRange.Name, factRange.Range.Mode)
        {
            Details = factRange.Details;
            Tags = factRange.Tags;
            ReferenceDate = factRange.ReferenceDate;
            CreatedBy = factRange.CreatedBy;
            CreatedOn = factRange.CreatedOn;
            Obsolete = factRange.Obsolete;
            ReplacedBy = factRange.ReplacedBy;
        }

        public static implicit operator FactHardNumber(FactRange factRange)
        {
            return new FactHardNumber(factRange);
        }

        [JsonProperty("value", Order = 20)]
        private double _value { get; set; } = 0.0;

        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    Update();
                }
            }
        }
    }
}