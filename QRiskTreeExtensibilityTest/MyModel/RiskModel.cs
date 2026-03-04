using Newtonsoft.Json;
using QRiskTree.Engine;

namespace QRiskTreeExtensibilityTest.MyModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RiskModel
    {
        static RiskModel()
        {
            // Register known types for JSON serialization.
            KnownTypesBinder.AddKnownType(typeof(Risk));
            KnownTypesBinder.AddKnownType(typeof(Probability));
            KnownTypesBinder.AddKnownType(typeof(Impact));
        }

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
            }
        }
        #endregion

        #region Risks management.
        [JsonProperty("risks", Order = 10)]
        private List<Risk>? _risks { get; set; }

        /// <summary>
        /// Get the collection of risks defined in the model.
        /// </summary>
        public IEnumerable<Risk> Risks => _risks?.AsEnumerable() ?? [];

        /// <summary>
        /// Adds a new risk to the model.
        /// </summary>
        /// <returns>The created <see cref="Risk"/>.</returns>
        public Risk AddRisk()
        {
            var result = new Risk();
            AddRisk(result);
            return result;
        }

        /// <summary>
        /// Adds a new risk to the model.
        /// </summary>
        /// <param name="name">The name of the new risk.</param>
        /// <returns>The created <see cref="Risk"/>.</returns>
        public Risk AddRisk(string name)
        {
            var result = new Risk(name);
            AddRisk(result);
            return result;
        }

        private void AddRisk(Risk risk)
        {
            _risks ??= new List<Risk>();
            _risks.Add(risk);
        }

        /// <summary>
        /// Get a Risk by its unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the risk.</param>
        /// <returns>The <see cref="Risk"/> with the specified ID, or null if not found.</returns>
        public Risk? GetRisk(Guid id)
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
            return risk != null ? (_risks?.Remove(risk) ?? false) : false;
        }

        /// <summary>
        /// Removes all risks from the model.
        /// </summary>
        public void ClearRisks()
        {
            var risks = _risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                _risks?.Clear();
            }
        }
        #endregion

        #region Simulation.
        /// <summary>
        /// Simulation of the model considering only the selected risks, without factoring in the selected mitigations.
        /// </summary>
        /// <param name="iterations">Number of iterations.</param>
        /// <returns>Residual risk.</returns>
        /// <remarks>It clears up the baseline definition.</remarks>
        public QRiskTree.Engine.Range? Simulate(uint iterations = Node.DefaultIterations)
        {
            double[]? samples = null;

            try
            {
                samples = CalculateResidualRisk(iterations);
            }
            catch 
            {
                // Ignore exceptions.
            }

            return samples?.ToRange(RangeType.Money, _minPercentile, _maxPercentile);
        }

        private double[] CalculateResidualRisk(uint iterations)
        {
            if (iterations < Node.MinIterations || iterations > Node.MaxIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), $"Samples must be between {Node.MinIterations} and {Node.MaxIterations}.");

            double[] result = new double[iterations];

            var risks = _risks?.ToArray();
            if (risks?.Any() ?? false)
            {
                foreach (var risk in risks)
                {
                    if (risk.SimulateAndGetSamples(out var riskSamples, 
                            MinPercentile, MaxPercentile, iterations) &&
                        riskSamples != null && riskSamples.Length == iterations)
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            result[i] += riskSamples[i];
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region Serialization and Deserialization.
        /// <summary>
        /// Serialize the model to file.
        /// </summary>
        /// <param name="filePath">Path where the file must be saved.</param>
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
                throw new InvalidOperationException($"Failed to load the Risk Model from '{filePath}'.");

            return result;
        }
        #endregion
    }
}
