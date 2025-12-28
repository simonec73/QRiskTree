using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using System.ComponentModel;
using System.Data;

namespace QRiskTree.Engine.ExtendedModel
{
    /// <summary>
    /// Risk model containing mitigated risks and their associated mitigations.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RiskModel : ChangesTracker, IDisposable
    {
        private static readonly Dictionary<Guid, RiskModel> _instances = new();
        private readonly FactsManager _factsManager = new FactsManager();
        private const double CurrentSchemaVersion = 0.4;
        private const double MinSchemaVersion = 0.0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        private RiskModel()
        {
            _factsManager.FactAdded += (fact) =>
            {
                // Add the fact to the facts collection.
                _facts ??= new FactsCollection();
                _facts.Add(fact);
            };

            _factsManager.FactRemoved += (fact) =>
            {
                // Remove all references to the fact from the risks.
                RecursivelyRemoveFact(fact);

                // Remove the fact from the facts collection.
                if (_facts?.Facts?.Contains(fact) ?? false)
                {
                    _facts.Remove(fact);
                }
            };

            _factsManager.FactUpdated += (fact) =>
            {
                _facts?.Replace(fact);
            };
        }

        /// <summary>
        /// Create a new instance of the RiskModel.
        /// </summary>
        /// <returns></returns>
        public static RiskModel Create()
        {
            var result = new RiskModel();
            _instances.Add(result.Id, result);
            return result;
        }

        /// <summary>
        /// Retrieves a <see cref="RiskModel"/> instance by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="RiskModel"/> to retrieve.</param>
        /// <returns>The <see cref="RiskModel"/> instance associated with the specified <paramref name="id"/>,  or <see
        /// langword="null"/> if no instance with the given identifier exists.</returns>
        public static RiskModel? Get(Guid id)
        {
            if (_instances.TryGetValue(id, out var result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        #region IDisposable implementation.
        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources.  After calling <see cref="Dispose"/>, the instance should not be used.</remarks>
        public void Dispose()
        {
            _factsManager.Clear();
            _instances.Remove(Id);
        }
        #endregion

        #region Properties.
        [JsonProperty("schemaVersion", Order = 0)]
        private double SchemaVersion { get; set; }

        /// <summary>
        /// Model unique identifier.
        /// </summary>
        [JsonProperty("id", Order = 1)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("name", Order = 2)]
        private string _name { get; set; } = "Risk Model";

        /// <summary>
        /// Name of the model.
        /// </summary>
        /// <remarks>Default is "Risk Model".</remarks>
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

        [JsonProperty("description", Order = 3)]
        private string? _description { get; set; }

        /// <summary>
        /// Description of the model.
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
        /// Optimal level of parallelism to be used for simulations.
        /// </summary>
        /// <remarks>It is calculated as 80% of the logical processors minus one, with a minimum of 1.</remarks>
        public static int OptimalParallelism
        {
            get
            {
                int logicalProcessors = Environment.ProcessorCount;
                int maxAllowed = (int)Math.Floor(logicalProcessors * 0.8);
                return Math.Max(1, maxAllowed - 1);
            }
        }
        #endregion

        #region Range management.
        [JsonProperty("minPercentile", Order = 5)]
        private int _minPercentile { get; set; } = 10;

        /// <summary>
        /// Percentile value to use for the minimum of the range.
        /// </summary>
        public int MinPercentile
        {
            get => _minPercentile;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentile must be between 0 and 100.");
                _minPercentile = value;
                Update();
            }
        }

        [JsonProperty("maxPercentile", Order = 6)]
        private int _maxPercentile { get; set; } = 90;

        /// <summary>
        /// Percentile value to use for the maximum of the range.
        /// </summary>
        public int MaxPercentile
        {
            get => _maxPercentile;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentile must be between 0 and 100.");
                _maxPercentile = value;
                Update();
            }
        }

        /// <summary>
        /// Currency symbol used for displaying monetary values.
        /// </summary>
        [JsonProperty("currencySymbol", Order = 7)]
        public string CurrencySymbol { get; set; } = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;

        /// <summary>
        /// Monetary scale used for displaying monetary values.
        /// </summary>
        /// <remarks>It typically is empty, or assumes values like 'K' or 'M'.</remarks>
        [JsonProperty("monetaryScale", Order = 8)]
        public string? MonetaryScale { get; set; }
        #endregion

        #region Risks management.
        [JsonProperty("risks", Order = 10)]
        private List<MitigatedRisk>? _risks { get; set; }

        /// <summary>
        /// Get the collection of risks defined in the model.
        /// </summary>
        public IEnumerable<MitigatedRisk> Risks => _risks?.AsEnumerable() ?? [];

        /// <summary>
        /// Adds a new risk to the model.
        /// </summary>
        /// <returns>The created <see cref="MitigatedRisk"/>.</returns>
        public MitigatedRisk AddRisk()
        {
            var result = new MitigatedRisk();
            AddRisk(result);
            return result;
        }

        /// <summary>
        /// Adds a new risk to the model.
        /// </summary>
        /// <param name="name">The name of the new risk.</param>
        /// <returns>The created <see cref="MitigatedRisk"/>.</returns>
        public MitigatedRisk AddRisk(string name)
        {
            var result = new MitigatedRisk(name);
            AddRisk(result);
            return result;
        }

        private void AddRisk(MitigatedRisk risk)
        {
            risk.AssignModel(this);
            risk.AssignFactsManager(_factsManager);
            _risks ??= new List<MitigatedRisk>();
            _risks.Add(risk);
            risk.ChildAdded += OnChildAdded;
            risk.ChildRemoved += OnChildRemoved;
            risk.FactAdded += OnFactAdded;
            risk.FactRemoved += OnFactRemoved;
            Update();
        }

        /// <summary>
        /// Get a Risk by its unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the risk.</param>
        /// <returns>The <see cref="MitigatedRisk"/> with the specified ID, or null if not found.</returns>
        public MitigatedRisk? GetRisk(Guid id)
        {
            return _risks?.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// Removes a Risk by its unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the risk to remove.</param>
        /// <returns>True if the risk was successfully removed; otherwise, false.</returns>
        public bool RemoveRisk(Guid id)
        {
            var risk = GetRisk(id);
            var result = risk != null ? (_risks?.Remove(risk) ?? false) : false;
            if (result)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                RecursivelyRemoveEvents(risk);
#pragma warning restore CS8604 // Possible null reference argument.
                Update();
            }

            return result;
        }

        /// <summary>
        /// Removes all risks from the model.
        /// </summary>
        public void ClearRisks()
        {
            var risks = _risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    RecursivelyRemoveEvents(risk);
                }

                _risks?.Clear();
                Update();
            }
        }
        #endregion

        #region Mitigations management.
        [JsonProperty("mitigations", Order = 11)]
        private List<MitigationCost>? _mitigations { get; set; }

        /// <summary>
        /// Gets the collection of mitigation costs associated with the current instance.
        /// </summary>
        public IEnumerable<MitigationCost> Mitigations => _mitigations?.AsEnumerable() ?? [];

        /// <summary>
        /// Adds a new mitigation cost to the model.
        /// </summary>
        /// <returns>The created <see cref="MitigationCost"/>.</returns>
        public MitigationCost AddMitigation()
        {
            var result = new MitigationCost();
            AddMitigation(result);
            return result;
        }

        /// <summary>
        /// Adds a new mitigation with the specified name and returns the associated mitigation cost object.
        /// </summary>
        /// <param name="name">The name of the mitigation to add.</param>
        /// <returns>A <see cref="MitigationCost"/> object representing the cost details for the newly added mitigation.</returns>
        public MitigationCost AddMitigation(string name)
        {
            var result = new MitigationCost(name);
            AddMitigation(result);
            return result;
        }

        private void AddMitigation(MitigationCost mitigation)
        {
            _mitigations ??= new List<MitigationCost>();
            _mitigations.Add(mitigation);
            mitigation.ChildAdded += OnChildAdded;
            mitigation.ChildRemoved += OnChildRemoved;
            mitigation.FactAdded += OnFactAdded;
            mitigation.FactRemoved += OnFactRemoved;
            Update();
        }

        /// <summary>
        /// Get a Mitigation by its unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the mitigation.</param>
        /// <returns>The <see cref="MitigationCost"/> with the specified ID, or null if not found.</returns>
        public MitigationCost? GetMitigation(Guid id)
        {
            return _mitigations?.FirstOrDefault(m => m.Id == id);
        }

        /// <summary>
        /// Removes the mitigation with the specified identifier from the collection.
        /// </summary>
        /// <remarks>When a mitigation is removed, all references to it are also removed from associated
        /// risks. The method has no effect if the specified mitigation does not exist.</remarks>
        /// <param name="id">The unique identifier of the mitigation to remove.</param>
        /// <returns>true if the mitigation was found and removed; otherwise, false.</returns>
        public bool RemoveMitigation(Guid id)
        {
            var result = false;

            var mitigation = GetMitigation(id);
            if (mitigation != null)
            {
                // Remove all references to this mitigation from the risks.
                _risks?.Select(x => x.RemoveMitigation(mitigation));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                result = _mitigations.Remove(mitigation);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (result)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    RecursivelyRemoveEvents(mitigation);
#pragma warning restore CS8604 // Possible null reference argument.
                    Update();
                }
            }

            return result;
        }

        /// <summary>
        /// Removes all mitigations from the model.
        /// </summary>
        public void ClearMitigations()
        {
            _risks?.Select(x => x.RemoveMitigations());

            var mitigations = _mitigations?.ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                {
                    RecursivelyRemoveEvents(mitigation);
                }

                _mitigations?.Clear();
                Update();
            }
        }
        #endregion

        #region Events management.
        private void OnChildAdded(Node parent, Node child)
        {
            RecursivelyAddEvents(child);
        }

        private void OnChildRemoved(Node parent, Node child)
        {
            RecursivelyRemoveEvents(child);
        }

        private void OnFactAdded(NodeWithFacts node, Fact fact)
        {
            // Add the fact to the facts collection.
            _facts ??= new FactsCollection();
            _facts?.Add(fact);
            Update();
        }

        private void OnFactRemoved(NodeWithFacts node, Fact fact)
        {
            // Remove the fact from the facts collection.
            if (IsUnnecessary(fact))
            {
                _facts?.Remove(fact);
                Update();
            }
        }

        private void RecursivelyAddEvents(Node node)
        {
            node.ChildAdded += OnChildAdded;
            node.ChildRemoved += OnChildRemoved;
            if (node is NodeWithFacts nodeWithFacts)
            {
                nodeWithFacts.FactAdded += OnFactAdded;
                nodeWithFacts.FactRemoved += OnFactRemoved;
            }

            var children = node.Children?.ToArray();
            if (children?.Any() ?? false)
            {
                foreach (var child in children)
                {
                    RecursivelyAddEvents(child);
                }
            }
        }

        private void RecursivelyRemoveEvents(Node node)
        {
            node.ChildAdded -= OnChildAdded;
            node.ChildRemoved -= OnChildRemoved;

            if (node is NodeWithFacts nodeWithFacts)
            {
                nodeWithFacts.FactAdded -= OnFactAdded;
                nodeWithFacts.FactRemoved -= OnFactRemoved;
            }

            var children = node.Children?.ToArray();
            if (children?.Any() ?? false)
            {
                foreach (var child in children)
                {
                    RecursivelyRemoveEvents(child);
                }
            }
        }

        private bool IsUnnecessary(Fact fact)
        {
            var result = true;

            var risks = _risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    if (!RecursivelyCheckIsUnnecessary(risk, fact))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        private bool RecursivelyCheckIsUnnecessary(Node node, Fact fact)
        {
            var result = true;

            if (node is NodeWithFacts nodeWithFacts)
            {
                if (nodeWithFacts.HasFact(fact.Id))
                {
                    result = false;
                }
            }

            if (result)
            {
                var children = node.Children?.ToArray();
                if (children?.Any() ?? false)
                {
                    // Check all children recursively.
                    foreach (var child in children)
                    {
                        result = RecursivelyCheckIsUnnecessary(child, fact);
                        if (!result)
                            break;
                    }
                }
            }

            return result;
        }
        #endregion

        #region Simulation.
        #region Events.
        /// <summary>
        /// Event raised when the simulation of a risk is completed.
        /// It includes the effects of the Mitigations, but not their cost.
        /// </summary>
        /// <remarks>The first parameter is the simulated Risk,
        /// the second is the enumeration of the identifiers of the Mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<MitigatedRisk, IEnumerable<Guid>?, double[]?>? RiskSimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the model is completed.
        /// It includes the effects of the Mitigations, but not their cost.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]?>? SimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the first year is completed.
        /// It includes the effects of the Mitigations and their implementation and operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]?>? FirstYearSimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the following years is completed.
        /// It includes the effects of the Mitigations and their operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]?>? FollowingYearsSimulationCompleted;
        /// <summary>
        /// Occurs when the baseline simulation has completed.
        /// </summary>
        /// <remarks>The event provides an array of double values representing the results of the
        /// simulation.</remarks>
        public event Action<double[]?>? BaselineSimulationCompleted;
        /// <summary>
        /// Event raised when the optimal simulation of the first year is completed.
        /// It includes the effects of the Mitigations and their implementation and operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]?>? OptimalFirstYearSimulationCompleted;
        /// <summary>
        /// Event raised when the optimal simulation of the following years is completed.
        /// It includes the effects of the Mitigations and their operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]?>? OptimalFollowingYearsSimulationCompleted;
        #endregion

        #region Public methods.
        /// <summary>
        /// Simulation of the model considering only the selected risks, without factoring in the selected mitigations.
        /// </summary>
        /// <param name="iterations">Number of iterations.</param>
        /// <returns>Residual risk.</returns>
        /// <remarks>It clears up the baseline definition.</remarks>
        public Range? Simulate(uint iterations = Node.DefaultIterations)
        {
            var enabledMitigations = _mitigations?.Where(x => x.IsEnabled).Select(x => x.Id).ToArray();
            SetEnabledState();
            ClearBaselineRisks();
            ClearBaselineMitigations();

            double[]? samples = null;

            try
            {
                samples = CalculateResidualRisk(iterations);
                SimulationCompleted?.Invoke(null, samples);
                BaselineSimulationCompleted?.Invoke(samples);
            }
            catch 
            {
                // Ignore exceptions.
            }
            finally
            {                 
                // Restore the original enabled state of mitigations.
                SetEnabledState(enabledMitigations);
            }

            return samples?.ToRange(RangeType.Money, _minPercentile, _maxPercentile);
        }

        /// <summary>
        /// Calculate the optimal combination of mitigations to minimize the overall cost of the model.
        /// </summary>
        /// <param name="optimizationParameter">Parameter to be used for the optimization. By default, the Mode is used.</param>
        /// <param name="optimizeForFollowingYears">Flag specifying if the optimization must be for the following years.
        /// If it is for the first year, the implementation costs are also considered.
        /// By default, the optimization considers the implementation costs.</param>
        /// <param name="iterations">Number of iterations. By default, the value of <see cref="Node.DefaultIterations"/> is used.</param>
        /// <returns>The simuation results.</returns>
        /// <remarks>It doesn't clear up the baseline definition.
        /// This variant is synchronous and is designed to be executed where it is paramount 
        /// to avoid consuming excessive resources. For example, it is ideal for centralized computing.</remarks>
        public SimulationResult? OptimizeMitigations(
            OptimizationParameter optimizationParameter = OptimizationParameter.Mode,
            bool optimizeForFollowingYears = false,
            uint iterations = Node.DefaultIterations)
        {
            SimulationResult? result = null;

            var mitigations = _mitigations?.Where(x => x.IsEnabled).ToArray();
            var mitigationIds = mitigations?.Select(x => x.Id).ToArray();

            if (mitigations != null && (mitigationIds?.Any() ?? false))
            {
                // Calculates the implementation and operational costs for the Mitigations.
                var costs = new Dictionary<Guid, (double[] ImplementationCosts, double[] OperationalCosts)>();
                foreach (var mitigation in mitigations)
                {
                    if (CalculateMitigationCosts(mitigation, iterations, out var implementationCostSamples, out var operationalCostSamples))
                    {
                        costs.Add(mitigation.Id, (implementationCostSamples!, operationalCostSamples!));
                    }
                }

                // Calculates the best combination of mitigations based on the optimization parameter.
                var combinations = RemoveCombinationsWithoutAuxiliary(GetAllCombinations(mitigationIds)).ToArray();
                var bestCombination = GetBestCombination(combinations, costs,
                    optimizationParameter, optimizeForFollowingYears, iterations);
                if (bestCombination != null)
                {
                    bestCombination.Apply(this, MinPercentile, MaxPercentile);
                    result = bestCombination;
                }

                // Restore the original enabled state of mitigations.
                SetEnabledState(mitigationIds);
            }

            return result;
        }

        /// <summary>
        /// Calculate asynchronously the optimal combination of mitigations to minimize the overall cost of the model.
        /// </summary>
        /// <param name="optimizationParameter">Parameter to be used for the optimization. By default, the Mode is used.</param>
        /// <param name="optimizeForFollowingYears">Flag specifying if the optimization must be for the following years.
        /// If it is for the first year, the implementation costs are also considered.
        /// By default, the optimization considers the implementation costs.</param>
        /// <param name="iterations">Number of iterations. By default, the value of <see cref="Node.DefaultIterations"/> is used.</param>
        /// <param name="parallelism">Degree of parallelism to use for the calculation. The default is to use up to 80% of the CPU.</param>
        /// <returns>The simuation results.</returns>
        /// <remarks>It doesn't clear up the baseline definition.<para/>
        /// This variant calculates each combination of the possible mitigations in parallel.</remarks>
        public async Task<SimulationResult?> OptimizeMitigationsAsync(
            OptimizationParameter optimizationParameter = OptimizationParameter.Mode,
            bool optimizeForFollowingYears = false,
            uint iterations = Node.DefaultIterations,
            int parallelism = 0)
        {
            SimulationResult? result = null;

            var mitigations = _mitigations?.Where(x => x.IsEnabled).ToArray();
            var mitigationIds = mitigations?.Select(x => x.Id).ToArray();

            if (mitigations != null && (mitigationIds?.Any() ?? false))
            {
                // Calculates the implementation and operational costs for the Mitigations.
                var costs = new Dictionary<Guid, (double[] ImplementationCosts, double[] OperationalCosts)>();
                foreach (var mitigation in mitigations)
                {
                    if (CalculateMitigationCosts(mitigation, iterations, out var implementationCostSamples, out var operationalCostSamples))
                    {
                        costs.Add(mitigation.Id, (implementationCostSamples!, operationalCostSamples!));
                    }
                }

                if (parallelism <= 0)
                {
                    parallelism = OptimalParallelism;
                }

                // Calculates the best combination of mitigations based on the optimization parameter.
                var combinations = RemoveCombinationsWithoutAuxiliary(GetAllCombinations(mitigationIds)).ToArray();
                var bestCombination = await GetBestCombinationAsync(combinations, costs,
                    optimizationParameter, optimizeForFollowingYears, iterations, parallelism);
                if (bestCombination != null)
                {
                    bestCombination.Apply(this, MinPercentile, MaxPercentile);
                    result = bestCombination;
                }

                // Restore the original enabled state of mitigations.
                SetEnabledState(mitigationIds);
            }

            return result;
        }
        #endregion

        #region Private methods.
        private bool CalculateMitigationCosts(MitigationCost mitigation, uint iterations, 
            out double[]? implementationCostSamples, out double[]? operationalCostSamples)
        {
            var result = false;
            implementationCostSamples = null;
            operationalCostSamples = null;

            if (mitigation.HasBaselines && (mitigation.ImplementationBaseline?.Length ?? 0) == iterations &&
                (mitigation.OperationBaseline?.Length ?? 0) == iterations)
            {
                implementationCostSamples = mitigation.ImplementationBaseline;
                operationalCostSamples = mitigation.OperationBaseline;
                result = true;
            }
            else if (mitigation.GenerateSamples(iterations, out var samples1) &&
                samples1 != null && samples1.Length == iterations &&
                mitigation.OperationCosts != null &&
                mitigation.OperationCosts.GenerateSamples(iterations, out var samples2) &&
                samples2 != null && samples2.Length == iterations)
            {
                implementationCostSamples = samples1;
                operationalCostSamples = samples2;
                mitigation.SetBaselines(implementationCostSamples, operationalCostSamples);
                result = true;
            }

            return result;
        }

        private double[] CalculateResidualRisk(uint iterations, SimulationResult? simulationResult = null)
        {
            if (iterations < Node.MinIterations || iterations > Node.MaxIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), $"Samples must be between {Node.MinIterations} and {Node.MaxIterations}.");

            double[] result = new double[iterations];

            var risks = _risks?.Where(x => x.IsEnabled).ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    if (risk.SimulateAndGetSamples(out var riskSamples, 
                            MinPercentile, MaxPercentile, iterations, simulationResult) &&
                        riskSamples != null && riskSamples.Length == iterations)
                    {
                        var mitigations = simulationResult?.SelectedMitigations?.ToArray();
                        if (mitigations?.Any() ?? false)
                        {
                            var appliedMitigations = risk.Children?.OfType<AppliedMitigation>()
                                .Where(x => mitigations.Contains(x.MitigationCostId)).ToArray();
                            if (appliedMitigations?.Any() ?? false)
                            {
                                // Apply each mitigation cost to the samples
                                foreach (var appliedMitigation in appliedMitigations)
                                {
                                    // Auxiliary mitigations do not affect the residual risk,
                                    // only the implementation and operation costs.
                                    if (!appliedMitigation.IsAuxiliary)
                                    {
                                        double[]? amSamples = null;
                                        bool ok = false;

                                        lock (appliedMitigation)
                                        {
                                            if (appliedMitigation.HasBaseline && ((appliedMitigation.Baseline?.Length ?? 0) == iterations))
                                            {
                                                amSamples = appliedMitigation.Baseline;
                                                ok = true;
                                            }
                                            else if (appliedMitigation.SimulateAndGetSamples(out amSamples,
                                                MinPercentile, MaxPercentile, iterations, simulationResult) &&
                                                (amSamples?.Length ?? 0) == iterations)
                                            {
                                                ok = true;
#pragma warning disable CS8604 // Possible null reference argument.
                                                appliedMitigation.SetBaseline(amSamples);
#pragma warning restore CS8604 // Possible null reference argument.
                                            }
                                        }

                                        if (ok)
                                        {
                                            // Subtract the effect of the mitigation from each value.
                                            for (int i = 0; i < iterations; i++)
                                            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                                riskSamples[i] *= (1 - amSamples[i]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        simulationResult?.AddSimulation(risk, riskSamples);

                        try
                        {
                            RiskSimulationCompleted?.Invoke(risk,
                                _mitigations?.Where(x => x.IsEnabled).Select(x => x.Id), riskSamples);
                        }
                        catch
                        {
                            // Ignore exceptions from the event handler.
                        }

                        for (int i = 0; i < iterations; i++)
                        {
                            result[i] += riskSamples[i];
                        }
                    }
                }
            }

            return result;
        }

        private void ClearBaselineRisks()
        {
            var risks = _risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    risk.ClearBaseline();

                    var mitigations = risk.Children?.OfType<AppliedMitigation>().ToArray();
                    if (mitigations?.Any() ?? false)
                    {
                        foreach (var mitigation in mitigations)
                        {
                            mitigation.ClearBaseline();
                        }
                    }
                }
            }
        }

        private void ClearBaselineMitigations()
        {
            var mitigations = _mitigations?.ToArray();
            if (mitigations?.Any() ?? false)
            {
                foreach (var mitigation in mitigations)
                {
                    mitigation.ClearBaselines();
                }
            }
        }

        private static IEnumerable<IEnumerable<Guid>> GetAllCombinations(IEnumerable<Guid> guids)
        {
            var list = guids.ToList();
            int count = list.Count;

            // Generate combinations of size 1 to N
            for (int size = 1; size <= count; size++)
            {
                foreach (var combination in GetCombinations(list, size))
                {
                    yield return combination;
                }
            }
        }

        private SimulationResult? GetBestCombination( 
            IEnumerable<Guid>[] combinations, Dictionary<Guid, (double[], double[])> costs,
            OptimizationParameter optimizationParameter, bool optimizeForFollowingYears,
            uint iterations)
        {
            SimulationResult? result = null;

            foreach (var combination in combinations)
            {
                var simulationResult = SimulateCombination(combination,
                    costs, iterations, optimizationParameter, optimizeForFollowingYears);

                if (simulationResult.IsBetterThan(result))
                {
                    result = simulationResult;
                }
            }

            if (result != null)
            {
                try
                {
                    OptimalFirstYearSimulationCompleted?.Invoke(result.SelectedMitigations, result.FirstYearSamples);
                    OptimalFollowingYearsSimulationCompleted?.Invoke(result.SelectedMitigations, result.FollowingYearSamples);
                }
                catch
                {
                    // Ignore exceptions from the event handler.
                }
            }

            return result;
        }

        private async Task<SimulationResult?> GetBestCombinationAsync(
            IEnumerable<Guid>[] combinations, Dictionary<Guid, (double[], double[])> costs, 
            OptimizationParameter optimizationParameter, bool optimizeForFollowingYears,
            uint iterations, int parallelism)
        {
            SimulationResult? result = null;

            await Parallel.ForEachAsync(
                combinations,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                (combination, token) =>
                {
                    var simulationResult = SimulateCombination(combination,
                        costs, iterations, optimizationParameter, optimizeForFollowingYears);

                    lock (this)
                    {
                        if (simulationResult.IsBetterThan(result))
                        {
                            result = simulationResult;
                        }
                    }

                    return ValueTask.CompletedTask;
                });

            if (result != null)
            {
                try
                {
                    OptimalFirstYearSimulationCompleted?.Invoke(result.SelectedMitigations, result.FirstYearSamples);
                    OptimalFollowingYearsSimulationCompleted?.Invoke(result.SelectedMitigations, result.FollowingYearSamples);
                }
                catch
                {
                    // Ignore exceptions from the event handler.
                }
            }

            return result;
        }

        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IList<T> list, int length)
        {
            if (length == 0)
                yield return new T[0];
            else
            {
                for (int i = 0; i <= list.Count - length; i++)
                {
                    foreach (var tail in GetCombinations(list.Skip(i + 1).ToList(), length - 1))
                    {
                        yield return new[] { list[i] }.Concat(tail);
                    }
                }
            }
        }

        private IEnumerable<Guid>? GetMitigationIDs(IEnumerable<Guid>? selectedMitigations)
        {
            IEnumerable<Guid>? mitigationIds = null;
            if (selectedMitigations == null)
            {
                mitigationIds = _mitigations?.Select(x => x.Id).ToArray();
            }
            else
            {
                mitigationIds = selectedMitigations.Where(x => _mitigations?.Any(y => x == y.Id) ?? false).ToArray();
            }

            return mitigationIds;
        }

        private IEnumerable<IEnumerable<Guid>> RemoveCombinationsWithoutAuxiliary(IEnumerable<IEnumerable<Guid>> inputCombinations)
        {
            var auxiliaryMitigationIds = _risks?
                .SelectMany(risk => risk.Children?.OfType<AppliedMitigation>() ?? Enumerable.Empty<AppliedMitigation>())
                .Where(mitigation => mitigation.IsAuxiliary)
                .Select(x => x.MitigationCostId)
                .Distinct()
                .ToArray();

            if (auxiliaryMitigationIds == null || auxiliaryMitigationIds.Length == 0)
                return inputCombinations;

            return inputCombinations.Where(combination => auxiliaryMitigationIds.All(auxId => combination.Contains(auxId)));
        }

        private void SetEnabledState(IEnumerable<Guid>? mitigations = null)
        {
            // Set the enabled state of mitigations based on the selection.
            var allMitigations = _mitigations?.ToArray();
            if (allMitigations?.Any() ?? false)
            {
                foreach (var m in allMitigations)
                {
                    m.IsEnabled = mitigations?.Any(x => x == m.Id) ?? false;
                }
            }
        }

        private SimulationResult SimulateCombination(IEnumerable<Guid> mitigations,
            Dictionary<Guid, (double[], double[])> costs, uint iterations,
            OptimizationParameter optimizationParameter, bool optimizeForFollowingYears)
        {
            var result = new SimulationResult(mitigations, optimizationParameter, optimizeForFollowingYears);
            var samples = CalculateResidualRisk(iterations, result);
            
            if (mitigations?.Any() ?? false)
            {
                // Add the implementation and operational costs of the selected mitigations.
                var samplesFirstYear = samples.ToArray();
                var samplesNextYears = samples.ToArray();
                foreach (var mitigation in mitigations)
                {
                    if (costs.TryGetValue(mitigation, out var costValues))
                    {
                        // First year: implementation + operational costs.
                        samplesFirstYear = samplesFirstYear
                            .Zip(costValues.Item1, (a, b) => a + b)
                            .Zip(costValues.Item2, (a, b) => a + b)
                            .ToArray();
                        // Next years: only operational costs.
                        samplesNextYears = samplesNextYears
                            .Zip(costValues.Item2, (a, b) => a + b)
                            .ToArray();
                    }
                }
                result.StoreResults(samplesFirstYear, samplesNextYears, _minPercentile, _maxPercentile);

                try
                {
                    if (optimizeForFollowingYears)
                    {
                        SimulationCompleted?.Invoke(mitigations, samplesNextYears);
                    }
                    else
                    {
                        SimulationCompleted?.Invoke(mitigations, samplesFirstYear);
                    }
                }
                catch
                {
                    // Ignore exceptions from the event handler.
                }
            }
            else
            {
                // No mitigations selected, so both first year and following years are the same.
                result.StoreResults(samples, samples, _minPercentile, _maxPercentile);

                try
                {
                    SimulationCompleted?.Invoke(mitigations, samples);
                }
                catch
                {
                    // Ignore exceptions from the event handler.
                }
            }

            return result;
        }
        #endregion
        #endregion

        #region Serialization and Deserialization.
        /// <summary>
        /// Serialize the model to file.
        /// </summary>
        /// <param name="filePath">Path where the file must be saved.</param>
        public void Serialize(string filePath)
        {
            SchemaVersion = CurrentSchemaVersion;
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

        /// <summary>
        /// Loads a Risk Model from the specified file.
        /// </summary>
        /// <param name="filePath">Path to the file to load the model from.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">The specified file was not found.</exception>
        /// <exception cref="InvalidOperationException">Model cannot be deserialized.</exception>
        /// <exception cref="NotSupportedException">The model file version is not supported.</exception>
        public static RiskModel? Load(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            RiskModel? result = null;

            var json = System.IO.File.ReadAllText(filePath);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                SerializationBinder = new KnownTypesBinder(),
                MaxDepth = 128
            };
            result = JsonConvert.DeserializeObject<RiskModel>(json, settings);
            if (result == null)
            {
                throw new InvalidOperationException($"Failed to load the Risk Model from '{filePath}'.");
            }
            else
            {
                // Check the model file version.
                if (result.SchemaVersion < MinSchemaVersion)
                {
                    throw new NotSupportedException($"The model file version {result.SchemaVersion} is not supported.");
                }

                _instances.Add(result.Id, result);

                // Register all the Facts in the FactsManager.
                var facts = result._facts?.Facts?.ToArray();
                if (facts?.Any() ?? false)
                {
                    foreach (var fact in facts)
                    {
                        result._factsManager?.Add(fact);
                    }
                }

                // Register all NodeWithFacts with the FactsManager.
                var risks = result._risks?.ToArray();
                if (risks?.Any() ?? false)
                {
                    foreach (var risk in risks)
                    {
                        risk.AssignModel(result);
                        var mitigations = risk.Children?.OfType<AppliedMitigation>().ToArray();
                        if (mitigations?.Any() ?? false)
                        {
                            foreach (var mitigation in mitigations)
                            {
                                mitigation.AssignModel(result);
                            }
                        }
                        RecursiveRegisterWithFactsManager(risk, result._factsManager);
                    }
                }
            }

            return result;
        }

        private static void RecursiveRegisterWithFactsManager(Node node, FactsManager? factsManager)
        {
            if (factsManager == null)
                return;

            if (node is NodeWithFacts nodeWithFacts)
            {
                nodeWithFacts.AssignFactsManager(factsManager);
            }

            var children = node.Children?.ToArray();
            if (children?.Any() ?? false)
            {
                foreach (var child in children)
                {
                    RecursiveRegisterWithFactsManager(child, factsManager);
                }
            }
        }
        #endregion

        #region Facts management.
        [JsonProperty("facts", Order = 12)]
        private FactsCollection? _facts { get; set; }

        /// <summary>
        /// Adds a fact to the collection managed by the system.
        /// </summary>
        /// <param name="fact">The fact to add. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the fact was successfully added; otherwise, <see langword="false"/>.</returns>
        public bool AddFact(Fact fact)
        {
            return _factsManager.Add(fact);
        }

        /// <summary>
        /// Removes the specified fact from the collection.
        /// </summary>
        /// <param name="fact">The fact to remove from the collection. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the fact was successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveFact(Fact fact)
        {
            return _factsManager.Remove(fact);
        }

        private void RecursivelyRemoveFact(Fact fact)
        {
            // Remove the fact from all risks and their children.
            _risks?.ForEach(r => RemoveFact(r, fact));
        }

        private void RemoveFact(NodeWithFacts node, Fact fact)
        {
            if (node.HasFact(fact.Id))
            {
                // Remove the fact from the node.
                node.Remove(fact);
                Update();
            }

            var children = node.Children?.OfType<NodeWithFacts>().ToArray();
            if (children?.Any() ?? false)
            {
                // Recursively remove the fact from all children.
                foreach (var child in children)
                {
                    RemoveFact(child, fact);
                }
            }
        }

        /// <summary>
        /// Gets the collection of available facts managed by the system.
        /// </summary>
        public IEnumerable<Fact>? AvailableFacts => _factsManager.Facts;

        /// <summary>
        /// Import the facts in the embedded FactsManager from a JSON file.
        /// </summary>
        /// <param name="filePath">Path of the file containing the Facts to be imported.</param>
        /// <param name="overwrite">True if eventual existing facts must be overwritten, false otherwise.</param>
        public void ImportFacts(string filePath, bool overwrite = false)
        {
            _factsManager.Import(filePath, overwrite);
        }

        /// <summary>
        /// Exports the current set of facts to the specified file.
        /// </summary>
        /// <remarks>This method writes the facts managed by the internal facts manager to the specified
        /// file.  Ensure the application has the necessary permissions to write to the specified file path.</remarks>
        /// <param name="filePath">The path of the file to which the facts will be exported. The path must be a valid file path and cannot be
        /// null or empty.</param>
        public void ExportFacts(string filePath)
        {
            _factsManager.Export(filePath);
        }
        #endregion
    }
}
