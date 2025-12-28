using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ExtendedModel
{
    /// <summary>
    /// Result of the simulation performed to optimize the Mitigations.
    /// </summary>
    public class SimulationResult : ISimulationContainer
    {
        private readonly IEnumerable<Guid>? _selectedMitigations;
        private readonly OptimizationParameter _optimizationParameter;
        private readonly bool _optimizeForFollowingYears;
        private readonly Dictionary<Guid, double[]> _results = new();
        private readonly Dictionary<Guid, RangeType> _rangeTypes = new();
        private Range? _firstYear;
        private double[]? _firstYearSamples;
        private Range? _followingYears;
        private double[]? _followingYearsSamples;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="selectedMitigations">Identifiers of the selected mitigations used for the simulation.</param>
        /// <param name="optimizationParameter">Parameter used for optimization during the simulation.</param>
        /// <param name="optimizeForFollowingYears">Indicates whether to optimize for following years.</param>
        public SimulationResult(IEnumerable<Guid>? selectedMitigations, 
            OptimizationParameter optimizationParameter, bool optimizeForFollowingYears)
        {
            _selectedMitigations = selectedMitigations;
            _optimizationParameter = optimizationParameter;
            _optimizeForFollowingYears = optimizeForFollowingYears;
        }

        /// <summary>
        /// Gets the mitigations used for the simulation.
        /// </summary>
        public IEnumerable<Guid>? SelectedMitigations => _selectedMitigations;

        /// <summary>
        /// Gets the range that represents the expected losses for the first year.
        /// </summary>
        public Range? FirstYear => _firstYear;

        /// <summary>
        /// Gets the range that represents the expected losses for the following year.
        /// </summary>
        public Range? FollowingYears => _followingYears;

        /// <summary>
        /// Gets the collection of sample values for the first year.
        /// </summary>
        public double[]? FirstYearSamples => _firstYearSamples;

        /// <summary>
        /// Gets the collection of sample values for the following years.
        /// </summary>
        public double[]? FollowingYearSamples => _followingYearsSamples;

        /// <summary>
        /// Register the samples generated for a given node.
        /// </summary>
        /// <param name="node">Node that has been simulated.</param>
        /// <param name="samples">Samples generated for the node.</param>
        public void AddSimulation(Node node, double[] samples)
        {
            _results[node.Id] = samples;
            _rangeTypes[node.Id] = node.RangeType;
        }

        /// <summary>
        /// Get the samples generated for a given node.
        /// </summary>
        /// <param name="node">Node that has been simulated.</param>
        /// <returns>Samples generated for the node.</returns>
        public double[]? GetSimulation(Node node)
        {
            return _results.ContainsKey(node.Id) ? _results[node.Id] : null;
        }

        /// <summary>
        /// Gets the range type associated with the specified node, if available.
        /// </summary>
        /// <param name="node">The node for which to retrieve the associated range type. Cannot be null.</param>
        /// <returns>The range type associated with the specified node, or null if no range type is defined for the node.</returns>
        public RangeType? GetRangeType(Node node)
        {
            return _rangeTypes.ContainsKey(node.Id) ? _rangeTypes[node.Id] : null;
        }

        /// <summary>
        /// Stores the results of the simulation for the first year and the following years.
        /// </summary>
        /// <param name="samplesFirstYear">Samples related to the first year.</param>
        /// <param name="samplesFollowingYears">Samples related to the following years.</param>
        /// <param name="minPercentile">Minimum percentile.</param>
        /// <param name="maxPercentile">Maximum percentile.</param>
        public void StoreResults(double[] samplesFirstYear, double[] samplesFollowingYears, 
            int minPercentile, int maxPercentile)
        {
            _firstYearSamples = samplesFirstYear;
            _followingYearsSamples = samplesFollowingYears;
            _firstYear = samplesFirstYear.ToRange(RangeType.Money, minPercentile, maxPercentile);
            _followingYears = samplesFollowingYears.ToRange(RangeType.Money, minPercentile, maxPercentile);
        }

        /// <summary>
        /// Compare the current SimulationResult with another one and determine if it is better.
        /// </summary>
        /// <param name="other">The other SimulationResult to be compared with the current one.</param>
        /// <returns>True if the current SimulationResult is better, false otherwise</returns>
        public bool IsBetterThan(SimulationResult? other)
        {
            var result = false;
            var otherFirstYear = other?.FirstYear;
            var otherFollowingYears = other?.FollowingYears;

            if (otherFirstYear == null || otherFollowingYears == null)
                return true;

            if (_firstYear == null || _followingYears == null)
                return false;

            if (_optimizeForFollowingYears)
            {
                switch (_optimizationParameter)
                {
                    case OptimizationParameter.Mode:
                        if (_followingYears.Mode < otherFollowingYears.Mode)
                        {
                            result = true;
                        }
                        break;
                    case OptimizationParameter.Min:
                        if (_followingYears.Min < otherFollowingYears.Min)
                        {
                            result = true;
                        }
                        break;
                    case OptimizationParameter.Max:
                        if (_followingYears.Max < otherFollowingYears.Max)
                        {
                            result = true;
                        }
                        break;
                }
            }
            else
            {
                switch (_optimizationParameter)
                {
                    case OptimizationParameter.Mode:
                        if (_firstYear.Mode < otherFirstYear.Mode)
                        {
                            result = true;
                        }
                        break;
                    case OptimizationParameter.Min:
                        if (_firstYear.Min < otherFirstYear.Min)
                        {
                            result = true;
                        }
                        break;
                    case OptimizationParameter.Max:
                        if (_firstYear.Max < otherFirstYear.Max)
                        {
                            result = true;
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Apply the simulation results to the given RiskModel.
        /// </summary>
        /// <param name="model">Model to which the simulation results will be applied.</param>
        /// <param name="minPercentile">Value of the minimum percentile.</param>
        /// <param name="maxPercentile">Value of the maximum percentile.</param>
        public void Apply(RiskModel model, int minPercentile, int maxPercentile)
        {
            var risks = model.Risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                    Apply(risk, minPercentile, maxPercentile);
            }

            var mitigations = model.Mitigations?.ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                    Apply(mitigation, minPercentile, maxPercentile);
            }
        }

        /// <summary>
        /// Apply the simulation results to the given node and its children.
        /// </summary>
        /// <param name="node">Node to which the simulation results will be applied.</param>
        /// <param name="minPercentile">Value of the minimum percentile.</param>
        /// <param name="maxPercentile">Value of the maximum percentile.</param>
        public void Apply(Node node, int minPercentile, int maxPercentile)
        {
            var nodeSamples = GetSimulation(node);
            var rangeType = GetRangeType(node);
            if (nodeSamples != null && rangeType != null)
            {
                var range = nodeSamples.ToRange(rangeType.Value, minPercentile, maxPercentile);
                if (range != null)
                {
                    node.SetRange(range);
                }
            }

            var children = node.Children?.ToArray();
            if (children?.Any() ?? false)
            {
                foreach (var child in children)
                    Apply(child, minPercentile, maxPercentile);
            }
        }
    }
}
