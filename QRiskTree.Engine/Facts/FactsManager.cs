namespace QRiskTree.Engine.Facts
{
    public class FactsManager
    {
        internal FactsManager() : base()
        {
        }

        private FactsCollection _facts = new();

        public event Action<Fact>? FactAdded;
        public event Action<Fact>? FactRemoved;
        public event Action<Fact>? FactUpdated;

        /// <summary>
        /// Get an enumeration of Facts.
        /// </summary>
        public IEnumerable<Fact>? Facts => _facts.Facts;

        public IEnumerable<Fact>? GetFacts(IEnumerable<Guid>? factIds)
        {
            return _facts.GetFacts(factIds);
        }

        public bool Add(Fact fact)
        {
            var result = _facts.Add(fact);
            if (result)
            {
                FactAdded?.Invoke(fact);
            }

            return result;
        }

        public bool Remove(Fact fact)
        {
            var result = _facts.Remove(fact);

            if (result)
            {
                FactRemoved?.Invoke(fact);
            }

            return result;
        }

        public void Clear()
        {
            var facts = _facts.Facts?.ToArray();
            if (facts?.Any() ?? false)
            {
                foreach (var fact in facts)
                {
                    if (Remove(fact))
                        FactRemoved?.Invoke(fact);
                }
            }
        }

        public bool HasFact(Fact fact)
        {
            return _facts?.HasFact(fact) ?? false;

        }

        public void Import(string filePath, bool overwrite = false)
        {
            var facts = FactsCollection.Import(filePath)?.Facts;
            if (facts?.Any() ?? false)
            {
                foreach (var fact in facts)
                {
                    var existing = _facts.Facts?.FirstOrDefault(x => x.Id == fact.Id);
                    if (existing != null)
                    {
                        if (overwrite)
                        {
                            var replaced = _facts.Replace(existing, fact);
                            if (replaced)
                            {
                                FactUpdated?.Invoke(fact);
                            }
                        }
                    }
                    else
                    {
                        var added = _facts.Add(fact);
                        if (added)
                        {
                            FactAdded?.Invoke(fact);
                        }
                    }
                }
            }
        }

        public void Export(string filePath)
        {
            _facts.Export(filePath);
        }
    }
}