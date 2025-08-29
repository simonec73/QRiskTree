using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    /// <summary>
    /// Represents a node that can be associated with a collection of facts.
    /// </summary>
    /// <remarks>This abstract class extends the functionality of a <see cref="Node"/> by allowing the
    /// association of facts. Facts are managed through a <see cref="FactsManager"/> and are identified by their unique
    /// <see cref="Guid"/>.</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class NodeWithFacts : Node
    {
        private FactsManager? _factsManager;

        [JsonProperty("facts", Order = 60)]
        private List<Guid>? _facts { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeWithFacts"/> class with the specified label and range type.
        /// </summary>
        /// <param name="label">The label associated with the node.</param>
        /// <param name="rangeType">The range type that defines the node's range behavior.</param>
        public NodeWithFacts(string label, RangeType rangeType) : base(label, rangeType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeWithFacts"/> class with the specified range type.
        /// </summary>
        /// <param name="rangeType">The range type associated with this node. This value determines the range characteristics of the node.</param>
        public NodeWithFacts(RangeType rangeType) : base(rangeType)
        {
        }

#pragma warning disable CS0067 // The events are never used.
        /// <summary>
        /// Occurs when a new fact is added to the node.
        /// </summary>
        /// <remarks>This event is triggered whenever a fact is added to the associated <see
        /// cref="NodeWithFacts"/>. Subscribers can use this event to respond to changes in the node's facts.</remarks>
        public event Action<NodeWithFacts, Fact>? FactAdded;

        /// <summary>
        /// Occurs when a fact is removed from a node.
        /// </summary>
        /// <remarks>This event is triggered whenever a fact is removed from the specified <see
        /// cref="NodeWithFacts"/>. Subscribers can use this event to perform actions in response to the removal of a
        /// fact.</remarks>
        public event Action<NodeWithFacts, Fact>? FactRemoved;
#pragma warning restore CS0067 // The events are never used.

        /// <summary>
        /// Gets the collection of facts associated with the current context.
        /// </summary>
        /// <remarks>The property returns null if the FactsManager has not been assigned to the <see
        /// cref="NodeWithFacts"/>, yet.</remarks>
        public IEnumerable<Fact>? Facts => _factsManager?.GetFacts(_facts);

        /// <summary>
        /// Assigs a FactsManager to this NodeWithFacts instance.
        /// </summary>
        /// <param name="factsManager"></param>
        public void AssignFactsManager(FactsManager factsManager)
        {
            _factsManager = factsManager;
        }

        /// <summary>
        /// Adds the specified fact to the collection if it does not already exist.
        /// </summary>
        /// <remarks>If the fact does not already exist in the collection and the associated facts
        /// manager, it will be added to both. The method ensures that the collection is updated and triggers an update
        /// operation after adding the fact.</remarks>
        /// <param name="fact">The fact to add. Must not be null.</param>
        /// <returns><see langword="true"/> if the fact was successfully added; otherwise, <see langword="false"/>.</returns>
        public bool Add(Fact fact)
        {
            bool result = false;

            if (!(_facts?.Contains(fact.Id) ?? false))
            {
                if (_factsManager != null && !_factsManager.HasFact(fact))
                {
                    _factsManager.Add(fact);
                }

                _facts ??= [];
                _facts.Add(fact.Id);
                result = true;
                Update();
            }

            return result;
        }

        /// <summary>
        /// Removes the specified fact from the collection.
        /// </summary>
        /// <remarks>If the collection is null or the specified fact does not exist in the collection, the
        /// method returns <see langword="false"/>. If the fact is successfully removed, the collection is
        /// updated.</remarks>
        /// <param name="fact">The fact to remove. The fact's <see cref="Fact.Id"/> is used to identify it in the collection.</param>
        /// <returns><see langword="true"/> if the fact was successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(Fact fact)
        {
            var result = _facts?.Remove(fact.Id) ?? false;

            if (result)
                Update();

            return result;
        }

        /// <summary>
        /// Determines whether the specified fact is present in the collection.
        /// </summary>
        /// <remarks>If the collection of facts is <see langword="null"/>, this method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="factId">The unique identifier of the fact to locate.</param>
        /// <returns><see langword="true"/> if the fact with the specified identifier exists in the collection; otherwise, <see
        /// langword="false"/>.</returns>
        public bool HasFact(Guid factId)
        {
            return _facts?.Contains(factId) ?? false;
        }

        public override bool Add(Node node)
        {
            if (_factsManager != null && node is NodeWithFacts nodeWithFacts)
            {
                nodeWithFacts.AssignFactsManager(_factsManager);
            }

            return base.Add(node);
        }
    }
}