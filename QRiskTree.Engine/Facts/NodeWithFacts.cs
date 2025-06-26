using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class NodeWithFacts : Node
    {
        [JsonProperty("facts")]
        private List<Guid>? _facts { get; set; }

        public NodeWithFacts(string label, RangeType rangeType) : base(label, rangeType)
        {
        }

        public NodeWithFacts(RangeType rangeType) : base(rangeType)
        {
        }

        public IEnumerable<Fact>? Facts => FactsDictionary.Instance.GetFacts(_facts);

        public bool Add(Fact fact)
        {
            bool result = false;

            if (_facts?.Contains(fact.Id) ?? false)
            {
                _facts ??= [];
                _facts.Add(fact.Id);
                result = true;
                Update();
            }

            return result;
        }

        public bool Remove(Fact fact)
        {
            return Remove(fact.Id);
        }

        public bool Remove(Guid factId)
        {
            var result = _facts?.Remove(factId) ?? false;

            if (result)
                Update();

            return result;
        }
    }
}