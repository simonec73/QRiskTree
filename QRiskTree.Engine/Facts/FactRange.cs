using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FactRange : Fact
    {
        private FactRange() : base("Unknown", "Unknown", "Unknown")
        {
            _range = new Range(RangeType.Money);
        }

        public FactRange(string context, string source, string name, Range range) : base(context, source, name)
        {
            _range = range;
        }

        public FactRange(FactHardNumber hn, RangeType rangeType)
            : this(hn.Context, hn.Source, hn.Name, new Range(rangeType, hn.Value, hn.Value, hn.Value, Confidence.Moderate))
        {
            Details = hn.Details;
            Tags = hn.Tags;
            ReferenceDate = hn.ReferenceDate;
            CreatedBy = hn.CreatedBy;
            CreatedOn = hn.CreatedOn;
            Obsolete = hn.Obsolete;
            ReplacedBy = hn.ReplacedBy;
        }

        [JsonProperty("range", Order = 20)]
        private Range _range { get; set; }

        public Range Range
        {
            get => _range;
            set
            {
                if (_range != value)
                {
                    if (_range != null)
                    {
                        _range.Changed -= _range_Changed;
                    }
                    _range = value;
                    _range.Changed += _range_Changed;
                    Update();
                }
            }
        }

        private void _range_Changed(object? sender, EventArgs e)
        {
            if (sender is ChangesTracker tracker)
            {
                Update(tracker, e);
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            _range.Changed += _range_Changed;
        }
    }
}