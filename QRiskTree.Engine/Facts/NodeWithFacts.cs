using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class NodeWithFacts : Node
    {
        [JsonProperty("facts", Order = 60)]
        private List<Guid>? _facts { get; set; }

        public NodeWithFacts(string label, RangeType rangeType) : base(label, rangeType)
        {
        }

        public NodeWithFacts(RangeType rangeType) : base(rangeType)
        {
        }

#pragma warning disable CS0067 // The events are never used.
        public event Action<NodeWithFacts, Fact>? FactAdded;
        public event Action<NodeWithFacts, Fact>? FactRemoved;
#pragma warning restore CS0067 // The events are never used.

        public IEnumerable<Fact>? Facts => FactsManager.Instance.GetFacts(_facts);

        public bool Add(Fact fact)
        {
            bool result = false;

            if (!(_facts?.Contains(fact.Id) ?? false))
            {
                if (!FactsManager.Instance.HasFact(fact))
                {
                    FactsManager.Instance.Add(fact);
                }

                _facts ??= [];
                _facts.Add(fact.Id);
                result = true;
                Update();
            }

            return result;
        }

        public bool Remove(Fact fact)
        {
            var result = _facts?.Remove(fact.Id) ?? false;

            if (result)
                Update();

            return result;
        }

        public bool HasFact(Guid factId)
        {
            return _facts?.Contains(factId) ?? false;
        }
    }
}