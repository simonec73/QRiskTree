using Newtonsoft.Json;
using QRiskTree.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.Facts
{
    /// <summary>
    /// Analyzes the facts.
    /// </summary>
    /// <remarks>If the node has associated facts, it processes them and then applies the selected operation. 
    /// If it doesn't have associated facts, it gets the samples from the child nodes and then applies the operation to the samples.</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class FactAnalyzerNode : NodeWithFacts
    {
        /// <summary>
        /// Initializes a new instance of the FactAnalyzerNode class with a number range type.
        /// </summary>
        public FactAnalyzerNode() : base(RangeType.Number)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FactAnalyzerNode class with a number range type.
        /// </summary>
        /// <param name="name">Name of the Fact Analyzer.</param>
        public FactAnalyzerNode(string name) : base(name, RangeType.Number)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FactAnalyzerNode class with a number range type.
        /// </summary>
        /// <param name="name">Name of the Fact Analyzer.</param>
        /// <param name="rangeType">Type of the range to be created.</param>
        public FactAnalyzerNode(string name, RangeType rangeType) : base(name, rangeType)
        {
        }

        #region Properties.
        /// <summary>
        /// Operation to be applied by the Analyzer.
        /// </summary>
        /// <remarks>Defaults to Sum.</remarks>
        [JsonProperty("operation")]
        public FactAnalyzerOperation Operation { get; set; } = FactAnalyzerOperation.Sum;

        /// <summary>
        /// Indicates if the result of the operation should be inverted numerically (e.g., multiplied by -1).
        /// </summary>
        /// <remarks>Defaults to false.<para/>
        /// If true, the result of the Operation is multiplied by -1.</remarks>
        [JsonProperty("opposite")]
        public bool Opposite { get; set; }

        /// <summary>
        /// Indicates if the result of the operation should be inverted as a reciprocal (e.g., 1 divided by the result).
        /// </summary>
        /// <remarks>Defaults to false.<para/>
        /// If true, the result of the Operation is inverted as a reciprocal.</remarks>
        [JsonProperty("reciprocal")]
        public bool Reciprocal { get; set; }
        #endregion

        #region Overrides.
        protected override bool IsValidChild(Node node)
        {
            return node is FactAnalyzerNode;
        }

        protected override bool? CanBeSimulated()
        {
            return Facts?.Any();
        }

        protected override bool Simulate(int minPercentile, int maxPercentile, uint iterations, ISimulationContainer? container, out double[]? samples)
        {
            var result = false;
            samples = null;

            var facts = Facts;
            var items = 0;

            if (facts?.Any() ?? false)
            {
                samples = new double[iterations];
                items = facts.Count();
                foreach (var fact in facts)
                {
                    if (fact is FactHardNumber factHardNumber)
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            DoOperation(samples, i, factHardNumber.Value);
                        }
                        result = true;
                    }
                    else if (fact is FactRange factRange && 
                        factRange.Range.GenerateSamples(iterations, out var rangeSamples))
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            DoOperation(samples, i, rangeSamples?[i] ?? 0.0);
                        }

                        result = true;
                    }
                }
            }
            else
            {
                var children = _children?.ToArray();

                if (children?.Any() ?? false)
                {
                    samples = new double[iterations];
                    items = children.Length;
                    foreach (var child in children)
                    {
                        if (child is FactAnalyzerNode factAnalyzerNode)
                        {
                            if (factAnalyzerNode.SimulateAndGetSamples(out var nodeSamples, minPercentile, maxPercentile, iterations, container) &&
                                nodeSamples != null && nodeSamples.Length == iterations)
                            {
                                for (int i = 0; i < iterations; i++)
                                {
                                    DoOperation(samples, i, nodeSamples?[i] ?? 0.0);
                                }
                            }
                        }
                    }
                    result = true;
                }
            }

            if (samples != null && samples.Length == iterations)
            {
                if (items > 0 && Operation == FactAnalyzerOperation.Average)
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        samples[i] /= items;
                    }
                }

                if (Opposite)
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        samples[i] = -samples[i];
                    }
                }

                if (Reciprocal)
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        if (samples[i] != 0)
                            samples[i] = 1.0 / samples[i];
                    }
                }
            }

            return result;
        }
        #endregion

        #region Auxiliary methods.
        private void DoOperation(double[] samples, int index, double value)
        {
            switch (Operation)
            {
                case FactAnalyzerOperation.Sum:
                    samples[index] += value;
                    break;
                case FactAnalyzerOperation.Multiply:
                    if (samples[index] == 0)
                        samples[index] = value;
                    else
                        samples[index] *= value;
                    break;
                case FactAnalyzerOperation.Average:
                    samples[index] += value;
                    break;
                case FactAnalyzerOperation.Min:
                    samples[index] = Math.Min(samples[index], value);
                    break;
                case FactAnalyzerOperation.Max:
                    samples[index] = Math.Max(samples[index], value);
                    break;
            }

        }
        #endregion
    }
}
