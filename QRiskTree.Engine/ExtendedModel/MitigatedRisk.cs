using Newtonsoft.Json;
using QRiskTree.Engine.Model;

namespace QRiskTree.Engine.ExtendedModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MitigatedRisk : Risk
    {
        internal MitigatedRisk() : base()
        {
        }

        internal MitigatedRisk(string name) : base(name)
        {
        }

        public void AssignModel(RiskModel model)
        {
            if (model != null)
            {
                _riskModelId = model.Id;
            }
        }

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

        #region Member overrides.
        protected override bool IsValidChild(Node node)
        {
            return base.IsValidChild(node) || (node is AppliedMitigation);
        }

        protected override bool Simulate(uint iterations, out double[]? samples, out Confidence confidence)
        {
            var result = false;
            samples = null;
            confidence = Confidence;

            if (IsEnabled && base.Simulate(iterations, out samples, out confidence) && (samples?.Length ?? 0) == iterations)
            {
                result = true;

                var appliedMitigations = _children?.OfType<AppliedMitigation>().Where(x => x.IsEnabled).ToArray();
                if (appliedMitigations?.Any() ?? false)
                {
                    // Apply each mitigation cost to the samples
                    foreach (var appliedMitigation in appliedMitigations)
                    {
                        if (Simulate(appliedMitigation, iterations, out var amSamples) &&
                            (amSamples?.Length ?? 0) == iterations)
                        {
                            // Subtract the effect of the mitigation from each value.
                            for (int i = 0; i < iterations; i++)
                            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                samples[i] *= (1 - amSamples[i]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            }

                            if (appliedMitigation.Confidence < confidence)
                                confidence = appliedMitigation.Confidence;
                        }
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
