using MathNet.Numerics.Statistics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace QRiskTree.Engine
{
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
        public const uint MaxIterations = 10485760; // Maximum number of iterations for the simulation.
        public const uint MinIterations = 10000; // Minimum number of iterations for the simulation.
        public const uint DefaultIterations = 100000; // Default number of iterations for the simulation.
        #endregion

        #region Properties.
        [JsonProperty("id")]
        private Guid _id { get; set; }

        public Guid Id => _id;

        [JsonProperty("name")]
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

        [JsonProperty("description")]
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

        [JsonProperty("children")]
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
        /// <summary>
        /// Add a node as a child.
        /// </summary>
        /// <param name="node">Node to be added.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The node cannot be null.</exception>
        /// <exception cref="ArgumentException">The node is not a valid child.</exception>
        public bool Add(Node node)
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
        public bool Remove(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node), "Node cannot be null.");

            var result = _children?.Remove(node) ?? false;

            if (result)
            {
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

        #region Member functions: simulation and statistics.
        /// <summary>
        /// Performs a Monte Carlo simulation for the node.
        /// </summary>
        /// <param name="samples">Calculated samples.</param>
        /// <param name="iterations">Number of samples to be generated. it must be between 1000 and 1048576.</param>
        /// <returns>True if the simulation has been performed, false otherwise.</returns>
        /// <remarks>The simulation is not performed if the node has been assigned its values.</remarks>
        public bool SimulateAndGetSamples(out double[]? samples, uint iterations = DefaultIterations)
        {
            if (iterations < MinIterations || iterations > MaxIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), $"Samples must be between {MinIterations} and {MaxIterations}.");

            var result = false;
            samples = null;

            // The cached samples cannot be used, so we must simulate the node.
            if (!Calculated.HasValue || Calculated.Value)
            {
                if (Simulate(iterations, out samples) && (samples?.Any() ?? false))
                {
                    UpdateStatistics(samples, iterations);
                    result = true;
                }
            }
            else
            {
                // The values have been set by the user, so we can use them directly to generate the samples.
                if (this.GenerateSamples(iterations, out samples) && (samples?.Any() ?? false))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Performs a Monte Carlo simulation for the node.
        /// </summary>
        /// <param name="iterations">Number of samples to be generated. it must be between 1000 and 1048576.</param>
        /// <returns>True if the simulation has been performed, false otherwise.</returns>
        /// <remarks>The simulation is not performed if the node has been assigned its values.</remarks>
        public bool Simulate(uint iterations = DefaultIterations)
        {
            return SimulateAndGetSamples(out _, iterations);
        }

        /// <summary>
        /// Determine the statistics for the current node by simulating it based on the information collected for its children.
        /// </summary>
        /// <param name="iterations">Number of iterations which must be performed.</param>
        /// <param name="samples">Calculated samples.</param>
        /// <returns>True if the simulation has succeeded, false otherwise.</returns>
        protected abstract bool Simulate(uint iterations, out double[]? samples);

        /// <summary>
        /// Simulate a child node.
        /// </summary>
        /// <param name="node">Child node to be simulated.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <param name="samples">Calculated samples.</param>
        /// <returns>True if the simulation succeeds, otherwise false.</returns>
        protected bool Simulate(Node node, uint iterations, out double[]? samples)
        {
            bool result = false;
            samples = null;

            if (!node.Calculated.HasValue || node.Calculated.Value)
            {
                // The values have not been set by the user, so we shall calculate them.
                result = node.Simulate(iterations, out samples);
                if (result && (samples?.Length ?? 0) > 0)
                {
                    node.UpdateStatistics(samples, iterations);
                }
            }
            else
            {
                // The values have been set by the user, so we can use them directly to generate the samples.
                result = node.GenerateSamples(iterations, out samples);
            }

            return result;
        }

        private void UpdateStatistics(double[]? samples, uint iterations)
        {
            if (samples != null && samples.Length == iterations)
            {
                // Assign the calculated values to the current node.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                _perc10 = samples.Percentile(10);
#pragma warning disable CS8604 // Possible null reference argument.
                _mode = samples.CalculateMode();
#pragma warning restore CS8604 // Possible null reference argument.
                _perc90 = samples.Percentile(90);
                _confidence = Confidence.Moderate;
                _calculated = true;
                Update();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        internal override void Update()
        {
            base.Update();
        }
        #endregion
    }
}
