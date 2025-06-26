using MathNet.Numerics.Statistics;
using Newtonsoft.Json;
using System.Data;
using System.Runtime.Serialization;
using System.Threading.Channels;

namespace QRiskTree.Engine.ExtendedOpenFAIR
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RiskModel
    {
        private static RiskModel _instance = new();

        public static RiskModel Instance => _instance;

        private RiskModel()
        {
            // Private constructor to enforce singleton pattern.
        }

        public static void Reset()
        {
            _instance = new RiskModel();
        }

        #region Risks management.
        [JsonProperty("risks")]
        private List<MitigatedRisk>? _risks { get; set; }

        public IEnumerable<MitigatedRisk> Risks => _risks?.AsEnumerable() ?? [];

        public MitigatedRisk AddRisk()
        {
            var result = new MitigatedRisk();
            _risks ??= new List<MitigatedRisk>();
            _risks.Add(result);
            return result;
        }

        public MitigatedRisk AddRisk(string name)
        {
            var result = new MitigatedRisk(name);
            _risks ??= new List<MitigatedRisk>();
            _risks.Add(result);
            return result;
        }

        public MitigatedRisk? GetRisk(Guid id)
        {
            return _risks?.FirstOrDefault(r => r.Id == id);
        }

        public bool RemoveRisk(Guid id)
        {
            var risk = GetRisk(id);
            return risk != null ? (_risks?.Remove(risk) ?? false) : false;
        }

        public void ClearRisks()
        {
            _risks?.Clear();
        }
        #endregion

        #region Mitigations management.
        [JsonProperty("mitigations")]
        private List<MitigationCost>? _mitigations { get; set; }

        public IEnumerable<MitigationCost> Mitigations => _mitigations?.AsEnumerable() ?? [];

        public MitigationCost AddMitigation()
        {
            var result = new MitigationCost();
            _mitigations ??= new List<MitigationCost>();
            _mitigations.Add(result);
            return result;
        }

        public MitigationCost AddMitigation(string name)
        {
            var result = new MitigationCost(name);
            _mitigations ??= new List<MitigationCost>();
            _mitigations.Add(result);
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
            }

            return result;
        }

        public void ClearMitigations()
        {
            _risks?.Select(x => x.RemoveMitigations());
            _mitigations?.Clear();
        }
        #endregion

        #region Simulation.
        /// <summary>
        /// Simulation of the model considering only the selected risks, without factoring in the selected mitigations.
        /// </summary>
        /// <param name="iterations">Number of iterations.</param>
        /// <returns>Residual risk.</returns>
        public Range? Simulate(uint iterations = Node.DefaultIterations)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            var enabledMitigations = _mitigations.Where(x => x.IsEnabled).Select(x => x.Id).ToArray();
#pragma warning restore CS8604 // Possible null reference argument.
            SetEnabledState();
            var samples = CalculateResidualRisk(iterations, out var confidence);
            SetEnabledState(enabledMitigations);
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
            return CalculateCosts(iterations, samples, confidence, out costFollowingYears);
        }

        private double[] CalculateResidualRisk(uint iterations, out Confidence confidence)
        {
            double[] result = new double[iterations];
            confidence = Confidence.High;

            var risks = _risks?.Where(x => x.IsSelected).ToArray();
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
#pragma warning disable CS8604 // Possible null reference argument.
                var enabledMitigations = _mitigations.Where(x => x.IsEnabled).Select(x => x.Id).ToArray();
#pragma warning restore CS8604 // Possible null reference argument.

                // Calculates the best combination of mitigations based on the optimization parameter.
                var combinations = GetAllCombinations(mitigationIds).ToArray();
                IEnumerable<Guid>? bestCombination = GetBestCombination(mitigationIds, combinations,
                    optimizationParameter, optimizeForFollowingYears, iterations, out var costFirstYear, out var costFollowingYears);
                if (bestCombination != null && costFirstYear != null && costFollowingYears != null)
                {
                    optimalCostFirstYear = costFirstYear;
                    optimalCostFollowingYears = costFollowingYears;
                    result = _mitigations.Where(x => bestCombination.Contains(x.Id));
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
                            case OptimizationParameter.Perc10:
                                if (costFollowingYears == null || simulatedCostFollowingYears.Perc10 < costFollowingYears.Perc10)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Perc90:
                                if (costFollowingYears == null || simulatedCostFollowingYears.Perc90 < costFollowingYears.Perc90)
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
                            case OptimizationParameter.Perc10:
                                if (costFirstYear == null || simulatedCostFirstYear.Perc10 < costFirstYear.Perc10)
                                {
                                    costFirstYear = simulatedCostFirstYear;
                                    costFollowingYears = simulatedCostFollowingYears;
                                    result = combination;
                                }
                                break;
                            case OptimizationParameter.Perc90:
                                if (costFirstYear == null || simulatedCostFirstYear.Perc90 < costFirstYear.Perc90)
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
    }
}
