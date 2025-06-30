using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FactsDictionary : ChangesTracker
    {
        private static FactsDictionary _instance = new();

        private FactsDictionary() : base()
        {
        }

        public static FactsDictionary Instance => _instance;

        [JsonProperty("facts", ItemTypeNameHandling = TypeNameHandling.Objects)]
        private List<Fact>? _facts { get; set; }

        /// <summary>
        /// Get an enumeration of Facts.
        /// </summary>
        public IEnumerable<Fact>? Facts => _facts?.AsEnumerable();

        public IEnumerable<Fact>? GetFacts(IEnumerable<Guid>? factIds)
        {
            IEnumerable<Fact>? result = null;
            if (factIds?.Any() ?? false)
                result = _facts?.Where(x => factIds.Contains(x.Id));

            return result;
        }

        public bool Add(Fact fact)
        {
            bool result = false;

            var existing = _facts?.FirstOrDefault(x => x.Id == fact.Id);
            if (existing != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var index = _facts.IndexOf(existing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                if (index >= 0)
                {
                    _facts[index] = fact;
                    result = true;
                    Update();
                }
            }
            else
            {
                _facts ??= [];
                _facts.Add(fact);
                result = true;
                Update();
            }

            return result;
        }

        public bool Remove(Fact fact)
        {
            var result = Remove(fact.Id);
            if (result)
            {
                Update();
            } 
            
            return result;
        }

        public bool Remove(Guid factId)
        {
            bool result = false;

            var existing = _facts?.FirstOrDefault(x => x.Id == factId);
            if (existing != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                result = _facts.Remove(existing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            if (result)
            {
                Update();
            }

            return result;
        }

        public void Serialize(string filePath)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                SerializationBinder = new KnownTypesBinder(),
                MaxDepth = 128,
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(this, settings);
            System.IO.File.WriteAllText(filePath, json);
        }

        public static bool Load(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            var result = false;

            var json = System.IO.File.ReadAllText(filePath);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                SerializationBinder = new KnownTypesBinder(),
                MaxDepth = 128
            };
            var factsDictionary = JsonConvert.DeserializeObject<FactsDictionary>(json, settings);
            if (factsDictionary != null)
            {
                _instance = factsDictionary;
                result = true;
            }
            else
            {
                throw new InvalidOperationException($"Failed to load the Facts from '{filePath}'.");
            }

            return result;
        }
    }
}