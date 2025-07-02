using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class FactsCollection
    {
        public FactsCollection()
        {
        }

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
                }
            }
            else
            {
                _facts ??= [];
                _facts.Add(fact);
                result = true;
            }

            return result;
        }

        public bool Remove(Fact fact)
        {
            bool result = false;

            var existing = _facts?.FirstOrDefault(x => x.Id == fact.Id);
            if (existing != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                result = _facts.Remove(existing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            return result;
        }

        public bool Replace(Fact newFact)
        {
            var oldFact = _facts?.FirstOrDefault(x => x.Id == newFact.Id);

            if (oldFact == null)
                return false;
            else
                return Replace(oldFact, newFact);
        }

        public bool Replace(Fact oldFact, Fact newFact)
        {
            var result = false;

            var index = _facts?.IndexOf(oldFact) ?? -1;
            if (index >= 0)
            {
                _facts![index] = newFact;
                result = true;
            }

            return result;
        }

        public bool HasFact(Fact fact)
        {
            return _facts?.Any(x => x.Id == fact.Id) ?? false;
        }

        public void Export(string filePath)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                SerializationBinder = new FactsTypesBinder(),
                MaxDepth = 128,
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(this, settings);
            System.IO.File.WriteAllText(filePath, json);
        }

        public static FactsCollection? Import(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            var json = System.IO.File.ReadAllText(filePath);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                SerializationBinder = new KnownTypesBinder(),
                MaxDepth = 128
            };

            return JsonConvert.DeserializeObject<FactsCollection>(json, settings);
        }
    }
}