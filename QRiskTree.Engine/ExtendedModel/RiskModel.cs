using MathNet.Numerics.Statistics;
using Newtonsoft.Json;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;
using System.Data;
using System.Runtime.Serialization;
using System.Threading.Channels;

namespace QRiskTree.Engine.ExtendedModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RiskModel : ChangesTracker
    {
        private static RiskModel _instance = new();

        public static RiskModel Instance => _instance;

        private RiskModel()
        {
            FactsManager.Instance.FactAdded += (fact) =>
            {
                // Add the fact to the facts collection.
                _facts ??= new FactsCollection();
                _facts.Add(fact);
            };

            FactsManager.Instance.FactRemoved += (fact) =>
            {
                // Remove all references to the fact from the risks.
                RecursivelyRemoveFact(fact);

                // Remove the fact from the facts collection.
                if (_facts?.Facts?.Contains(fact) ?? false)
                {
                    _facts.Remove(fact);
                }
            };

            FactsManager.Instance.FactUpdated += (fact) =>
            {
                _facts?.Replace(fact);
            };
        }

        public static void Reset()
        {
            _instance.ClearMitigations();
            _instance.ClearRisks();
            _instance = new RiskModel();
        }

        #region Properties.
        [JsonProperty("name", Order = 1)]
        private string _name { get; set; } = "Risk Model";

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

        [JsonProperty("description", Order = 2)]
        private string? _description { get; set; }

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
        #endregion

        #region Range management.
        [JsonProperty("minPercentile", Order = 5)]
        public int MinPercentile
        {
            get => Range.MinPercentile;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentile must be between 0 and 100.");
                Range.MinPercentile = value;
                Update();
            }
        }

        [JsonProperty("maxPercentile", Order = 6)]
        public int MaxPercentile
        {
            get => Range.MaxPercentile;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "Percentile must be between 0 and 100.");
                Range.MaxPercentile = value;
                Update();
            }
        }
        #endregion

        #region Risks management.
        [JsonProperty("risks", Order = 10)]
        private List<MitigatedRisk>? _risks { get; set; }

        public IEnumerable<MitigatedRisk> Risks => _risks?.AsEnumerable() ?? [];

        public MitigatedRisk AddRisk()
        {
            var result = new MitigatedRisk();
            _risks ??= new List<MitigatedRisk>();
            _risks.Add(result);
            result.ChildAdded += OnChildAdded;
            result.ChildRemoved += OnChildRemoved;
            result.FactAdded += OnFactAdded;
            result.FactRemoved += OnFactRemoved;
            Update();
            return result;
        }

        public MitigatedRisk AddRisk(string name)
        {
            var result = new MitigatedRisk(name);
            _risks ??= new List<MitigatedRisk>();
            _risks.Add(result);
            result.ChildAdded += OnChildAdded;
            result.ChildRemoved += OnChildRemoved;
            result.FactAdded += OnFactAdded;
            result.FactRemoved += OnFactRemoved;
            Update();
            return result;
        }

        public MitigatedRisk? GetRisk(Guid id)
        {
            return _risks?.FirstOrDefault(r => r.Id == id);
        }

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

        public IEnumerable<MitigationCost> Mitigations => _mitigations?.AsEnumerable() ?? [];

        public MitigationCost AddMitigation()
        {
            var result = new MitigationCost();
            _mitigations ??= new List<MitigationCost>();
            _mitigations.Add(result);
            result.ChildAdded += OnChildAdded;
            result.ChildRemoved += OnChildRemoved;
            result.FactAdded += OnFactAdded;
            result.FactRemoved += OnFactRemoved;
            Update();
            return result;
        }

        public MitigationCost AddMitigation(string name)
        {
            var result = new MitigationCost(name);
            _mitigations ??= new List<MitigationCost>();
            _mitigations.Add(result);
            result.ChildAdded += OnChildAdded;
            result.ChildRemoved += OnChildRemoved;
            result.FactAdded += OnFactAdded;
            result.FactRemoved += OnFactRemoved;
            Update();
            return result;
        }

        public MitigationCost? GetMitigation(Guid id)
        {
            return _mitigations?.FirstOrDefault(m => m.Id == id);
        }

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

        public bool RecursivelyCheckIsUnnecessary(Node node, Fact fact)
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
        /// <summary>
        /// Event raised when the simulation of a risk is completed.
        /// It includes the effects of the Mitigations, but not their cost.
        /// </summary>
        /// <remarks>The first parameter is the simulated Risk,
        /// the second is the enumeration of the identifiers of the Mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<MitigatedRisk, IEnumerable<Guid>?, double[]>? RiskSimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the model is completed.
        /// It includes the effects of the Mitigations, but not their cost.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]>? SimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the first year is completed.
        /// It includes the effects of the Mitigations and their implementation and operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]>? FirstYearSimulationCompleted;
        /// <summary>
        /// Event raised when the simulation of the following years is completed.
        /// It includes the effects of the Mitigations and their operation costs.
        /// </summary>
        /// <remarks>The first parameter is the enumeration of the identifiers of the mitigations considered during the generation,
        /// and the third is an array containing the generated samples.</remarks>
        public event Action<IEnumerable<Guid>?, double[]>? FollowingYearsSimulationCompleted;

        /// <summary>
        /// Simulation of the model considering only the selected risks, without factoring in the selected mitigations.
        /// </summary>
        /// <param name="iterations">Number of iterations.</param>
        /// <returns>Residual risk.</returns>
        public Range? Simulate(uint iterations = Node.DefaultIterations)
        {
            var enabledMitigations = _mitigations?.Where(x => x.IsEnabled).Select(x => x.Id).ToArray();
            SetEnabledState();

            double[]? samples = null;
            Confidence confidence = Confidence.Moderate;

            try
            {
                samples = CalculateResidualRisk(iterations, out confidence);
                SimulationCompleted?.Invoke(null, samples);
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

            return samples?.ToRange(RangeType.Money, confidence);
        }

        /// <summary>
        /// Simulation of the model considering the selected risks and enabled mitigations.
        /// </summary>
        /// <param name="costFollowingYears">Overall costs calculated for the years following the first.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <returns>Costs calculated for the first year.</returns>
        public Range? Simulate(out Range? costFollowingYears, uint iterations = Node.DefaultIterations)
        {
            var samples = CalculateResidualRisk(iterations, out var confidence);

            try
            {
                SimulationCompleted?.Invoke(_mitigations?.Where(x => x.IsEnabled).Select(x => x.Id), samples);
            }
            catch
            {
                // Ignore exceptions from the event handler.
            }

            return CalculateCosts(iterations, samples, confidence, out costFollowingYears);
        }

        private double[] CalculateResidualRisk(uint iterations, out Confidence confidence)
        {
            double[] result = new double[iterations];
            confidence = Confidence.High;

            var risks = _risks?.Where(x => x.IsEnabled).ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    if (risk.SimulateAndGetSamples(out var riskSamples, iterations) &&
                        (riskSamples?.Length ?? 0) == iterations)
                    {
                        if (confidence > risk.Confidence)
                        {
                            confidence = risk.Confidence;
                        }

                        for (int i = 0; i < iterations; i++)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            result[i] += riskSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }

                        try
                        {
#pragma warning disable CS8604 // Possible null reference argument.
                            RiskSimulationCompleted?.Invoke(risk, 
                                _mitigations?.Where(x => x.IsEnabled).Select(x => x.Id), riskSamples);
#pragma warning restore CS8604 // Possible null reference argument.
                        }
                        catch
                        {
                            // Ignore exceptions from the event handler.
                        }
                    }
                }
            }

            return result;
        }

        private Range? CalculateCosts(uint iterations, double[]? samples, Confidence confidence, out Range? followingYearsCosts)
        {
            Range? result = null;
            followingYearsCosts = null;

            if (samples != null && samples.Length == iterations)
            {
                var firstYearSamples = samples.ToArray();
                var followingYearsSamples = samples.ToArray();

                var mitigations = _mitigations?.Where(x => x.IsEnabled).ToArray();
                if (mitigations?.Any() ?? false)
                {
                    foreach (var mitigation in mitigations)
                    {
                        confidence = CalculateCosts(mitigation, iterations, confidence, firstYearSamples, followingYearsSamples);
                    }

                    try
                    {
                        var mitigationIds = mitigations.Select(x => x.Id);
                        FirstYearSimulationCompleted?.Invoke(mitigationIds, firstYearSamples);
                        FollowingYearsSimulationCompleted?.Invoke(mitigationIds, followingYearsSamples);
                    }
                    catch
                    {
                        // Ignore exceptions from the event handler.
                    }

                    result = firstYearSamples.ToRange(RangeType.Money, confidence);
                    followingYearsCosts = followingYearsSamples.ToRange(RangeType.Money, confidence);
                }
            }

            return result;
        }

        private Confidence CalculateCosts(MitigationCost mitigation, uint iterations, Confidence confidence, double[] firstYearSamples, double[] followingYearsSamples)
        {
            if (mitigation.SimulateAndGetSamples(out var implementationCostSamples, iterations) &&
                (implementationCostSamples?.Length ?? 0) == iterations)
            {
                if (confidence > mitigation.Confidence)
                {
                    confidence = mitigation.Confidence;
                }

                for (int i = 0; i < iterations; i++)
                {
                    // The implementation cost affects only the first year.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    firstYearSamples[i] += implementationCostSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }

                if (mitigation.OperationCosts != null &&
                    mitigation.OperationCosts.GenerateSamples(iterations, out var operationalCostSamples) &&
                    (operationalCostSamples?.Length ?? 0) == iterations)
                {

                    for (int i = 0; i < iterations; i++)
                    {
                        // Operational costs affect both the first year and following years.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        firstYearSamples[i] += operationalCostSamples[i];
                        followingYearsSamples[i] += operationalCostSamples[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
            }

            return confidence;
        }

        public IEnumerable<MitigationCost>? OptimizeMitigations(out Range? optimalCostFirstYear, 
            out Range? optimalCostFollowingYears,
            IEnumerable<Guid>? selectedMitigations = null,
            OptimizationParameter optimizationParameter = OptimizationParameter.Mode,
            bool optimizeForFollowingYears = false,
            uint iterations = Node.DefaultIterations)
        {
            IEnumerable<MitigationCost>? result = null;
            optimalCostFirstYear = null;
            optimalCostFollowingYears = null;

            IEnumerable<Guid>? mitigationIds = null;
            if (selectedMitigations == null)
            {
                mitigationIds = _mitigations?.Select(x => x.Id).ToArray();
            }
            else
            {
                mitigationIds = selectedMitigations.Where(x => _mitigations?.Any(y => x == y.Id) ?? false).ToArray();
            }

            if (mitigationIds?.Any() ?? false)
            {
                var enabledMitigations = _mitigations?.Where(x => x.IsEnabled).Select(x => x.Id).ToArray();

                // Calculates the best combination of mitigations based on the optimization parameter.
                var combinations = GetAllCombinations(mitigationIds).ToArray();
                IEnumerable<Guid>? bestCombination = GetBestCombination(mitigationIds, combinations,
                    optimizationParameter, optimizeForFollowingYears, iterations, out var costFirstYear, out var costFollowingYears);
                if (bestCombination != null && costFirstYear != null && costFollowingYears != null)
                {
                    optimalCostFirstYear = costFirstYear;
                    optimalCostFollowingYears = costFollowingYears;
                    result = _mitigations?.Where(x => bestCombination.Contains(x.Id));
                }

                // Restore the original enabled state of mitigations.
                SetEnabledState(enabledMitigations);
            }

            return result;
        }

        private IEnumerable<Guid>? GetBestCombination(IEnumerable<Guid> selectedMitigations, 
            IEnumerable<Guid>[] combinations, 
            OptimizationParameter optimizationParameter, bool optimizeForFollowingYears,
            uint iterations, out Range? costFirstYear, out Range? costFollowingYears)
        {
            IEnumerable<Guid>? result = null;
            costFirstYear = null;
            costFollowingYears = null;

            foreach (var combination in combinations)
            {
                var simulatedCostFirstYear = SimulateCombination(selectedMitigations.Where(x => combination.Contains(x)), 
                    iterations, out var simulatedCostFollowingYears);

                if (simulatedCostFirstYear != null && simulatedCostFollowingYears != null)
                {
                    if (optimizeForFollowingYears)
                    {
                        switch (optimizationParameter)
                        {
                            case OptimizationParameter.Mode:
                                if (costFollowingYears == null || simulatedCostFollowingYears.Mode < costFollowingYears.Mode)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Min:
                                if (costFollowingYears == null || simulatedCostFollowingYears.Min < costFollowingYears.Min)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Max:
                                if (costFollowingYears == null || simulatedCostFollowingYears.Max < costFollowingYears.Max)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (optimizationParameter)
                        {
                            case OptimizationParameter.Mode:
                                if (costFirstYear == null || simulatedCostFirstYear.Mode < costFirstYear.Mode)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Min:
                                if (costFirstYear == null || simulatedCostFirstYear.Min < costFirstYear.Min)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Max:
                                if (costFirstYear == null || simulatedCostFirstYear.Max < costFirstYear.Max)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                        }
                    }
                }
            }

            return result;
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

        private Range? SimulateCombination(IEnumerable<Guid> mitigations, uint iterations, out Range? costFollowingYears)
        {
            // Set the enabled state of mitigations based on the selection.
            SetEnabledState(mitigations);

            return Simulate(out costFollowingYears, iterations);
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
        #endregion

        #region Serialization and Deserialization.

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
            var riskModel = JsonConvert.DeserializeObject<RiskModel>(json, settings);
            if (riskModel != null)
            {
                _instance = riskModel;
                result = true;
            }
            else
            {
                throw new InvalidOperationException($"Failed to load the Risk Model from '{filePath}'.");
            }

            return result;
        }
        #endregion

        #region Facts management.
        [JsonProperty("facts", Order = 12)]
        private FactsCollection? _facts { get; set; }

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
        #endregion
    }
}
