using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;

namespace QRiskTreeParallelTest
{

    internal class QRiskTreeEngineTester
    {
        private readonly RiskModel _model;

        public QRiskTreeEngineTester()
        {
            _model = RiskModel.Create();
        }

        public RiskModel Model => _model;

        public Task<SimulationResult?> SimulateAsync(uint iterations)
        {
            return Task.Run(() => Simulate(iterations));
        }

        private SimulationResult? Simulate(uint iterations)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            QRiskTree.Engine.Range? costs = null;

            try
            {
                var rng = new Random();

                for (int i = 0; i < 5; i++)
                {
                    var risk = _model.AddRisk($"Risk {i + 1}");
                    var rand = rng?.Next(10) ?? 0;
                    risk.AddLossEventFrequency().Set(rand, rand * 2, rand * 3, Confidence.Moderate);
                    rand = rng?.Next(10) ?? 0;
                    risk.AddLossMagnitude().Set(rand * 10000, rand * 25000, rand * 50000, Confidence.Moderate);
                }

                for (int j = 0; j < 3; j++)
                {
                    var rand = rng?.Next(10) ?? 0;
                    var mitigation = _model.AddMitigation($"Mitigation {j + 1}")
                        .SetOperationCosts(rand * 1000, rand * 2500, rand * 10000, Confidence.Moderate)
                        .Set<MitigationCost>(rand * 5000, rand * 10000, rand * 30000, Confidence.Moderate);

                    var risks = _model.Risks?.ToArray();
                    if (risks?.Any() ?? false)
                    {
                        foreach (var risk in risks)
                        {
                            if (risk.ApplyMitigation(mitigation, out var appliedMitigation))
                            {
                                var dRand = rng?.NextDouble() ?? 0.0;
                                appliedMitigation?.Set(dRand, double.Min(dRand + 0.1, 1.0), double.Min(dRand + 0.25, 1.0), Confidence.High);
                            }
                        }
                    }
                }

                var baseline = _model.Simulate(iterations);
                _model.OptimizeMitigations(out costs, out var followingYearsCosts, iterations: iterations);
            }
            finally
            {
                stopwatch.Stop();
            }

            return new SimulationResult(stopwatch.ElapsedMilliseconds, costs?.Min ?? 0.0, costs?.Mode ?? 0.0, costs?.Max ?? 0.0);
        }
    }
}
