using Newtonsoft.Json;
using QRiskTree.Engine.OpenFAIR;

namespace QRiskTree.Engine.ExtendedOpenFAIR
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

        [JsonProperty("selected")]
        private bool _isSelected { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    Update();
                }
            }
        }

        public bool ApplyMitigation(MitigationCost mitigation, out AppliedMitigation appliedMitigation)
        {
            appliedMitigation = new AppliedMitigation(mitigation);
            return Add(appliedMitigation);
        }

        public bool RemoveMitigation(MitigationCost mitigation)
        {
            bool result = false;

            var appliedMitigations = _children?.OfType<AppliedMitigation>().Where(x => x.MitigationCostId == mitigation.Id).ToArray();
            if (appliedMitigations?.Any() ?? false)
            {
                foreach (var m in appliedMitigations)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    result |= _children.Remove(m);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

            return result;
        }

        public bool RemoveMitigations()
        {
            var result = false;

            var appliedMitigations = _children?.OfType<AppliedMitigation>().ToArray();
            if (appliedMitigations?.Any() ?? false)
            {
                foreach (var m in appliedMitigations)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    result |= _children.Remove(m);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

            return result;
        }

        protected override bool IsValidChild(Node node)
        {
            return base.IsValidChild(node) || (node is AppliedMitigation);
        }

        protected override bool Simulate(uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            if (IsSelected && base.Simulate(iterations, out samples) && (samples?.Length ?? 0) == iterations)
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
                        }
                    }
                }
            }

            return result;
        }
    }
}
