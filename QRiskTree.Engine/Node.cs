using MathNet.Numerics.Statistics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace QRiskTree.Engine
{
    /// <summary>
    /// Base class for all nodes in the risk tree.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Node : Range
    {
        #region Constructors.
        /// <summary>
        /// Costructor for the node.
        /// </summary>
        /// <param name="label">Label to assign to the node.</param>
        /// <param name="rangeType">Type of range associated with the node.</param>
        public Node(string label, RangeType rangeType) : this(rangeType)
        {
            _name = label;
        }

        /// <summary>
        /// Costructor for the node.
        /// </summary>
        /// <param name="rangeType">Type of range associated with the node.</param>
        public Node(RangeType rangeType) : base(rangeType)
        {
            _id = Guid.NewGuid();
        }
        #endregion

        #region Constants.
        /// <summary>
        /// Maximum number of iterations for the simulation.
        /// </summary>
        /// <remarks>This constant defines the upper limit for the number of iterations that can be used
        /// in the simulation. It ensures that the simulation has a limited duration.</remarks>
        public const uint MaxIterations = 10485760;
        /// <summary>
        /// Represents the minimum number of iterations required for the simulation.
        /// </summary>
        /// <remarks>This constant defines the lower limit for the number of iterations that can be used
        /// in the simulation. It ensures that the simulation runs with a sufficient number of iterations to produce
        /// meaningful results.</remarks>
        public const uint MinIterations = 10000;
        /// <summary>
        /// Represents the default number of iterations for the simulation.
        /// </summary>
        /// <remarks>This constant defines the standard iteration count used in simulation processes.  It
        /// can be used as a default value when no specific iteration count is provided.
        /// This number of iterations is not necessarily the most recommended one, 
        /// but just one that should provide decent results for average models, without consuming too many resources. 
        /// In fact, the recommendation is to increase this number to at least 200000 whenever possible, 
        /// to improve the quality of the simulation, as more iterations improve the stability of the results.</remarks>
        public const uint DefaultIterations = 100000;
        #endregion

        #region Properties.
        [JsonProperty("id", Order = 1)]
        private Guid _id { get; set; }

        public Guid Id => _id;

        [JsonProperty("name", Order = 2)]
        private string? _name { get; set; }

        /// <summary>
        /// Label assigned to the node.
        /// </summary>
        public string? Name
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

        [JsonProperty("description", Order = 3)]
        private string? _description { get; set; }

        /// <summary>
        /// Description of the node.
        /// </summary>
        public string? Description
        {             
            get => _description;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.CompareOrdinal(value, _description) != 0)
                {
                    _description = value;
                    Update();
                }
            }
        }

        /// <summary>
        /// Internal storage for child nodes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security",
            "SCS0028:TypeNameHandling is set to the other value than 'None'. It may lead to deserialization vulnerability.",
            Justification = "We use SerializationBinders (KnownTypesBinder and FactsTypesBinder) to ensure that we deserialize only known and approved objects.")]
        [JsonProperty("children", Order = 50, ItemTypeNameHandling = TypeNameHandling.Objects)]
        protected List<Node>? _children { get; set; }

        /// <summary>
        /// Child nodes.
        /// </summary>
        public IEnumerable<Node>? Children => _children?.AsEnumerable();
        #endregion

        #region Overrides.
        override public string ToString()
        {
            return _name ?? string.Empty; // provide a default value
        }
        #endregion

        #region Member functions: child management.
        public event Action<Node, Node>? ChildAdded;
        public event Action<Node, Node>? ChildRemoved;

        /// <summary>
        /// Add a node as a child.
        /// </summary>
        /// <param name="node">Node to be added.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The node cannot be null.</exception>
        /// <exception cref="ArgumentException">The node is not a valid child.</exception>
        public virtual bool Add(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node), "Node cannot be null.");
            if (!IsValidChild(node))
                throw new ArgumentException("Invalid child node.", nameof(node));

            var result = false;

            if (!(_children?.Contains(node) ?? false))
            {
                _children ??= [];
                _children.Add(node);
                result = true;
                ChildAdded?.Invoke(this, node);
                Update();
            }

            return result;
        }

        /// <summary>
        /// Remove a child node.
        /// </summary>
        /// <param name="node">Node to be removed</param>
        /// <returns>True if the node was removed successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">The node cannot be null.</exception>
        public virtual bool Remove(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node), "Node cannot be null.");

            var result = _children?.Remove(node) ?? false;

            if (result)
            {
                ChildRemoved?.Invoke(this, node);
                Update();
            }

            return result;
        }

        /// <summary>
        /// Method implemented in the child nodes to determine if a node can be added as a child.
        /// </summary>
        /// <param name="node">Node to be checked.</param>
        /// <returns>True if it can be added, false otherwise.</returns>
        protected abstract bool IsValidChild(Node node);
        #endregion

        #region Public member functions: simulation and statistics.
        /// <summary>
        /// Verifies if the node can be simulated.
        /// </summary>
        /// <param name="node">[out] Eventual node that cannot be simulated.</param>
        /// <returns>True if the node can be simulated, false otherwise.</returns>
        public bool CanBeSimulated(out Node? node)
        {
            bool result;

            var canBeSimulated = CanBeSimulated();
            if (canBeSimulated.HasValue)
            {
                result = canBeSimulated.Value;
                node = result ? null : this;
            }
            else
            {
                result = Calculated.HasValue && !Calculated.Value;
                node = null;

                if (!result)
                {
                    node = this;

                    if (HasAllChildren() && (_children?.Any() ?? false))
                    {
                        result = true;
                        var children = _children.ToArray();
                        foreach (var child in children)
                        {
                            if (!child.CanBeSimulated(out var violatingNode))
                            {
                                node = violatingNode;
                                result = false;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Performs a Monte Carlo simulation for the node.
        /// </summary>
        /// <param name="samples">Calculated samples.</param>
        /// <param name="minPercentile">Value corresponding to the minimum percentile.</param>
        /// <param name="maxPercentile">Value corresponding to the maximum percentile.</param>
        /// <param name="iterations">Number of samples to be generated. it must be between 1000 and 1048576.</param>
        /// <param name="container">Container for the simulation results.</param>
        /// <returns>True if the simulation has been performed, false otherwise.</returns>
        /// <remarks>The simulation is not performed if the node has been assigned its values.</remarks>
        public bool SimulateAndGetSamples(out double[]? samples, int minPercentile, int maxPercentile,
            uint iterations = DefaultIterations, ISimulationContainer? container = null)
        {
            if (iterations < MinIterations || iterations > MaxIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), $"Samples must be between {MinIterations} and {MaxIterations}.");

            return Simulate(this, minPercentile, maxPercentile, iterations, container, out samples);
        }

        /// <summary>
        /// Performs a Monte Carlo simulation for the node.
        /// </summary>
        /// <param name="minPercentile">Value corresponding to the minimum percentile.</param>
        /// <param name="maxPercentile">Value corresponding to the maximum percentile.</param>
        /// <param name="iterations">Number of samples to be generated. it must be between 1000 and 1048576.</param>
        /// <param name="container">Container for the simulation results.</param>
        /// <returns>True if the simulation has been performed, false otherwise.</returns>
        /// <remarks>The simulation is not performed if the node has been assigned its values.</remarks>
        public bool Simulate(int minPercentile, int maxPercentile, uint iterations = DefaultIterations, ISimulationContainer? container = null)
        {
            return SimulateAndGetSamples(out _, minPercentile, maxPercentile, iterations, container);
        }
        #endregion

        #region Protected/Private member functions: simulation and statistics.
        /// <summary>
        /// Function to be overridden in derived classes which allows them to override the default verification.
        /// </summary>
        /// <returns>Null, if the basic behavior must be applied. 
        /// If not null, the returned value determines if the node can be simulated.</returns>
        protected virtual bool? CanBeSimulated()
        {
            return null;
        }

        /// <summary>
        /// When overridden in a derived class, it determines if the node has all derived children.
        /// </summary>
        /// <returns>True if all children are present, false if any required child is missing.</returns>
        protected virtual bool HasAllChildren()
        {
            return true;
        }

        /// <summary>
        /// Determine the statistics for the current node by simulating it based on the information collected for its children.
        /// </summary>
        /// <param name="minPercentile">Value corresponding to the minimum percentile.</param>
        /// <param name="maxPercentile">Value corresponding to the maximum percentile.</param>
        /// <param name="iterations">Number of iterations which must be performed.</param>
        /// <param name="container">Container for the simulation results.</param>
        /// <param name="samples">[out] Calculated samples.</param>
        /// <returns>True if the simulation has succeeded, false otherwise.</returns>
        protected abstract bool Simulate(int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples);

        /// <summary>
        /// Simulate a child node.
        /// </summary>
        /// <param name="node">Child node to be simulated.</param>
        /// <param name="minPercentile">Value corresponding to the minimum percentile.</param>
        /// <param name="maxPercentile">Value corresponding to the maximum percentile.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <param name="container">Container for the simulation results.</param>
        /// <param name="samples">Calculated samples.</param>
        /// <returns>True if the simulation succeeds, otherwise false.</returns>
        protected bool Simulate(Node node, int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples)
        {
            bool result = false;
            samples = null;

            if (!node.Calculated.HasValue || node.Calculated.Value)
            {
                // The values have not been set by the user, so we shall calculate them.
                result = node.Simulate(minPercentile, maxPercentile, iterations, container, out samples);
                if (result && samples != null && samples.Length > 0)
                {
                    container?.AddSimulation(node, samples);
                    _min = samples.Percentile(minPercentile);
                    _mode = samples.CalculateMode();
                    _max = samples.Percentile(maxPercentile);
                    _confidence = samples.CalculateConfidence();
                    _calculated = true;
                    Update();
                }
            }
            else
            {
                // The values have been set by the user, so we can use them directly to generate the samples.
                result = node.GenerateSamples(iterations, out samples);
                if (result && samples != null && samples.Length > 0)
                {
                    container?.AddSimulation(node, samples);
                }
            }

            return result;
        }

        internal override void Update()
        {
            base.Update();
        }
        #endregion

        #region Range value storing.
        /// <summary>
        /// Forces a value for the range of the node.
        /// </summary>
        /// <param name="range">Range definition</param>
        /// <exception cref="ArgumentNullException">Input range cannot be null.</exception>
        public void SetRange(Range range)
        {
            if (range == null)
                throw new ArgumentNullException(nameof(range), "Range cannot be null.");
            _min = range.Min;
            _mode = range.Mode;
            _max = range.Max;
            _confidence = range.Confidence;
            _calculated = range.Calculated;
            Update();
        }
        #endregion
    }
}
