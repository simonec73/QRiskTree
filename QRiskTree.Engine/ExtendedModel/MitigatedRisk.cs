using Newtonsoft.Json;
using QRiskTree.Engine.Model;

namespace QRiskTree.Engine.ExtendedModel
{
    /// <summary>
    /// Represents a risk that has been mitigated within a risk model.
    /// </summary>
    /// <remarks>The <see cref="MitigatedRisk"/> class extends the <see cref="Risk"/> class to include
    /// functionality for managing mitigations and baselines. It allows for the application and removal of mitigations,
    /// as well as the simulation of risk with applied mitigations. This class is intended for use within a risk
    /// management system where risks are assessed and mitigated based on defined models.</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class MitigatedRisk : Risk
    {
        #region Constructors.
        internal MitigatedRisk() : base()
        {
        }

        internal MitigatedRisk(string name) : base(name)
        {
        }
        #endregion

        #region Properties.
        [JsonProperty("riskModelId", Order = 4)]
        private Guid _riskModelId { get; set; }

        [JsonProperty("enabled", Order = 5)]
        private bool _isEnabled { get; set; } = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    Update();
                }
            }
        }
        #endregion

        #region Public methods: mitigations management.
        /// <summary>
        /// Applies a mitigation to this risk model by creating an AppliedMitigation instance and adding it as a child node.
        /// </summary>
        /// <param name="mitigation">Mitigation to be applied.</param>
        /// <param name="appliedMitigation">[out] The new applied mitigation object.</param>
        /// <returns>True if the generation succeeded, false otherwise.</returns>
        public bool ApplyMitigation(MitigationCost mitigation, out AppliedMitigation? appliedMitigation)
        {
            var riskModel = RiskModel.Get(_riskModelId);
            if (riskModel != null)
            {
                appliedMitigation = new AppliedMitigation(riskModel, mitigation);
                return Add(appliedMitigation);
            }
            else
            {
                appliedMitigation = null;
                return false;
            }
        }

        /// <summary>
        /// Remove a mitigation from this risk model by removing all AppliedMitigation instances that refer to the specified MitigationCost.
        /// </summary>
        /// <param name="mitigation">Mitigation to be removed.</param>
        /// <returns>True if at least an associated mitigation has been removed.</returns>
        public bool RemoveMitigation(MitigationCost mitigation)
        {
            bool result = false;

            var appliedMitigations = _children?.OfType<AppliedMitigation>().Where(x => x.MitigationCostId == mitigation.Id).ToArray();
            if (appliedMitigations?.Any() ?? false)
            {
                foreach (var m in appliedMitigations)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    result |= Remove(m);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

            return result;
        }

        /// <summary>
        /// Remove all mitigations from this risk model by removing all AppliedMitigation instances.
        /// </summary>
        /// <returns></returns>
        public bool RemoveMitigations()
        {
            var result = false;

            var appliedMitigations = _children?.OfType<AppliedMitigation>().ToArray();
            if (appliedMitigations?.Any() ?? false)
            {
                foreach (var m in appliedMitigations)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    result |= Remove(m);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

            return result;
        }
        #endregion

        #region Baseline management.
        private double[]? _baseline;

        /// <summary>
        /// Flags indicating whether this risk has a defined baseline.
        /// </summary>
        public bool HasBaseline => _baseline?.Any() ?? false;

        /// <summary>
        /// Gets the baseline values.
        /// </summary>
        public double[]? Baseline => _baseline?.ToArray();

        /// <summary>
        /// Gets the confidence level of the baseline.
        /// </summary>
        public Confidence BaselineConfidence { get; private set; } = Confidence.Low;

        /// <summary>
        /// Set the baseline values and confidence.
        /// </summary>
        /// <param name="baseline">New baseline.</param>
        /// <param name="confidence">New confidence level.</param>
        public void SetBaseline(double[] baseline, Confidence confidence)
        {
            if ((baseline?.Any() ?? false) && (confidence != Confidence.Low))
            {
                _baseline = baseline.ToArray();
                BaselineConfidence = confidence;
            }
        }

        /// <summary>
        /// Clear the baseline values and confidence.
        /// </summary>
        public void ClearBaseline()
        {
            _baseline = null;
            BaselineConfidence = Confidence.Low;
        }
        #endregion

        #region Member overrides.
        /// <summary>
        /// Checks if the specified node is a valid child for this node.
        /// </summary>
        /// <param name="node">Node to be checked.</param>
        /// <returns>True if it valid, false otherwise.</returns>
        protected override bool IsValidChild(Node node)
        {
            return base.IsValidChild(node) || (node is AppliedMitigation);
        }

        /// <summary>
        /// Simulates this risk, applying any mitigation costs from enabled AppliedMitigation child nodes.
        /// </summary>
        /// <param name="iterations">Number of iterations for the simulation.</param>
        /// <param name="samples">[out] Generated samples.</param>
        /// <param name="confidence"></param>
        /// <returns></returns>
        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            var result = false;
            samples = null;
            confidence = Confidence;

            if (IsEnabled)
            {
                if (HasBaseline && ((Baseline?.Length ?? 0) == iterations))
                {
                    // Use the baseline values as samples.
                    samples = Baseline;
                    confidence = BaselineConfidence;
                    result = true;
                }
                else if (base.Simulate(iterations, out samples, out confidence) && (samples?.Length ?? 0) == iterations)
                {
                    result = true;

                    // Set the baseline.
                    _baseline = samples?.ToArray();
                    BaselineConfidence = confidence;
                }

                if (result)
                {
                    var appliedMitigations = _children?.OfType<AppliedMitigation>().Where(x => x.IsEnabled).ToArray();
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
                                Confidence amConfidence = Confidence.Low;
                                bool ok = false;

                                if (appliedMitigation.HasBaseline && ((appliedMitigation.Baseline?.Length ?? 0) == iterations))
                                {
                                    amSamples = appliedMitigation.Baseline;
                                    amConfidence = appliedMitigation.BaselineConfidence;
                                    ok = true;
                                }
                                else if (Simulate(appliedMitigation, iterations, out amSamples) &&
                                    (amSamples?.Length ?? 0) == iterations)
                                {
                                    ok = true;
                                    amConfidence = appliedMitigation.Confidence;
#pragma warning disable CS8604 // Possible null reference argument.
                                    appliedMitigation.SetBaseline(amSamples, amConfidence);
#pragma warning restore CS8604 // Possible null reference argument.
                                }

                                if (ok)
                                {
                                    // Subtract the effect of the mitigation from each value.
                                    for (int i = 0; i < iterations; i++)
                                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                        samples[i] *= (1 - amSamples[i]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                    }

                                    if (appliedMitigation.Confidence < confidence)
                                        confidence = amConfidence;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region Internal methods.
        /// <summary>
        /// Assign the Risk to a RiskModel.
        /// </summary>
        /// <param name="model">Owning model.</param>
        internal void AssignModel(RiskModel model)
        {
            if (model != null)
            {
                _riskModelId = model.Id;
            }
        }
        #endregion
    }
}
