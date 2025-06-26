using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Fact : ChangesTracker
    {
        public Fact(string context, string source, string name) : base()
        {
            _context = context;
            _source = source;
            _name = name;
        }

        [JsonProperty("id")]
        public Guid Id { get; protected set; } = Guid.NewGuid();

        [JsonProperty("context")]
        private string _context;

        public string Context
        {
            get => _context;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.CompareOrdinal(value, _context) != 0)
                {
                    _context = value;
                    Update();
                }
            }
        }

        [JsonProperty("source")]
        private string _source;

        public string Source
        {
            get => _source;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.CompareOrdinal(value, _source) != 0)
                {
                    _source = value;
                    Update();
                }
            }
        }

        [JsonProperty("name")]
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.CompareOrdinal(value, _name) != 0)
                {
                    _name = value;
                    Update();
                }
            }
        }

        [JsonProperty("details")]
        private string? _details;

        public string? Details
        {
            get => _details;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.CompareOrdinal(value, _details) != 0)
                {
                    _details = value;
                    Update();
                }
            }
        }

        [JsonProperty("tags")]
        private List<string>? _tags { get; set; }

        public IEnumerable<string>? Tags
        {
            get => _tags?.ToArray();
            set
            {
                bool different = true;

                if ((value?.Count() ?? 0) == (_tags?.Count ?? 0))
                {
                    different = false;
                    if (value?.Any() ?? false)
                    {
                        foreach (var item in value)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            // We are sure here that we have at least a tag in the list.
                            if (!_tags.Contains(item))
                            {
                                different = true;
                                break;
                            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
                    }
                }

                if (different)
                {
                    if (value?.Any() ?? false)
                    {
                        _tags = [.. value];
                    }
                    else
                    {                         
                        _tags = null;
                    }
                    Update();
                }
            }
        }

        [JsonProperty("refDate")]
        public DateTime ReferenceDate { get; set; }

        [JsonProperty("obsolete")]
        public bool Obsolete { get; protected set; }

        [JsonProperty("replacedBy")]
        public Guid ReplacedBy { get; protected set; }

        public void MarkObsolete(Fact? newFact = null)
        {
            Obsolete = true;
            if (newFact != null)
            {
                ReplacedBy = newFact.Id;
            }
            Update();
        }
    }
}
