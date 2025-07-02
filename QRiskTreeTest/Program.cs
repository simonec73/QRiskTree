using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.OpenFAIR;
using QRiskTree.Engine.Facts;

#region Step 1 - Definition of the risk model.
var model = RiskModel.Instance;
model.MinPercentile = 5;
model.MaxPercentile = 95;

#region Risk1 - A simple risk calculated using only the LossEventFrequency and LossMagnitude nodes.
var risk1 = model.AddRisk("Risk 1: Unauthorized access to sensitive data.");
risk1.AddLossEventFrequency().Set(1, 12, 120, Confidence.Moderate);
risk1.AddLossMagnitude().Set(100000, 500000, 20000000, Confidence.Moderate);
#endregion

#region Risk2 - A more complex risk with a full, nested structure.
var risk2 = model.AddRisk("Risk 2: Data loss due to hardware failure.");

#region Calculation of the Loss Event Frequency.
var lef = risk2.AddLossEventFrequency("Data loss event frequency");

var tef = lef.AddThreatEventFrequency("Data loss threat event frequency");
tef.AddContactFrequency("Data Loss Contact Frequency").Set(52, 356, 3560, Confidence.Moderate);
tef.AddProbabilityOfAction("Data loss probability of action").Set(0.01, 0.1, 0.5, Confidence.Moderate);

var vuln = lef.AddVulnerability("Data loss vulnerability");
vuln.AddThreatCapability("Data loss threat capability").Set(0.1, 0.5, 0.9,  Confidence.Moderate);
vuln.AddResistenceStrength("Data loss resistance strength").Set(0.2, 0.5, 0.75, Confidence.Moderate);
#endregion

#region Calculation of the Loss Magnitude.
var mag = risk2.AddLossMagnitude("Data loss magnitude");

mag.AddPrimaryLoss("Primary loss: Productivity").Set(LossForm.Productivity).Set(0, 100000, 1000000, Confidence.Low);
mag.AddPrimaryLoss("Primary loss: Replacement").Set(LossForm.Replacement).Set(1000, 2000, 3000, Confidence.High);
mag.AddPrimaryLoss("Primary loss: Response").Set(LossForm.Response).Set(50000, 100000, 1000000, Confidence.Low);

var sec1 = mag.AddSecondaryRisk("Secondary risk: Reputation").Set(LossForm.Reputation);
sec1.AddSecondaryLossEventFrequency("Reputation event frequency").Set(0.01, 0.02, 0.2, Confidence.Moderate);
sec1.AddSecondaryLossMagnitude("Reputation loss magnitude").Set(10000, 50000, 200000, Confidence.Low);

var sec2 = mag.AddSecondaryRisk("Secondary risk: Competitive Advantage").Set(LossForm.Competitive_Advantage);
sec2.AddSecondaryLossEventFrequency("Competitive Advantage event frequency").Set(0.01, 0.02, 0.1, Confidence.Moderate);
sec2.AddSecondaryLossMagnitude("Competitive Advantage loss magnitude").Set(100000, 500000, 2000000, Confidence.Low);
#endregion
#endregion

#region Risk3 - A third risk, using only LossEventFrequency and LossMagnitude nodes.
var risk3 = model.AddRisk("Risk 3: Phishing attack leading to credential theft.");
risk3.AddLossEventFrequency().Set(12, 60, 1200, Confidence.Moderate);
risk3.AddLossMagnitude().Set(100, 500, 1500, Confidence.Low);
#endregion

#region Definition of the mitigations.
var mitigation1 = model.AddMitigation("Mitigation 1: Implement multi-factor authentication.")
    .SetOperationCosts(1200, 6000, 20000, Confidence.Moderate)
    .Set<MitigationCost>(5000, 10000, 20000, Confidence.Moderate);
if (risk1.ApplyMitigation(mitigation1, out var r1Mitigation1))
{
    r1Mitigation1.Set(0.1, 0.2, 0.5, Confidence.Moderate);
}
if (risk3.ApplyMitigation(mitigation1, out var r3Mitigation1))
{
    r3Mitigation1.Set(0.5, 0.75, 0.9, Confidence.Moderate);
}

var mitigation2 = model.AddMitigation("Mitigation 2: Regular data backups.")
    .SetOperationCosts(1200, 12000, 24000, Confidence.Moderate)
    .Set<MitigationCost>(5000, 10000, 20000, Confidence.Moderate);
if (risk2.ApplyMitigation(mitigation2, out var r2Mitigation2))
{
    r3Mitigation1.Set(0.3, 0.67, 0.9, Confidence.High);
}

var mitigation3 = model.AddMitigation("Mitigation 3: Employee training on phishing awareness.")
    .SetOperationCosts(10000, 20000, 50000, Confidence.Moderate)
    .Set<MitigationCost>(200000, 300000, 5000000, Confidence.Moderate);
if (risk3.ApplyMitigation(mitigation3, out var r3Mitigation3))
{
    r3Mitigation3.Set(0.0001, 0.00025, 0.0004, Confidence.Moderate);
}
#endregion

#region Adding some facts to the model.
risk1.Add(new FactRange("Some context", "John Doe", "This is some information about something", new QRiskTree.Engine.Range(RangeType.Money, 10000, 20000, 100000, Confidence.Low)));

tef.Add(new FactHardNumber("Some context", "John Doe", "This is some information about the contact frequency", 100)
{
    Details = "This is a detail about the contact frequency.",
    Tags = new[] { "tag1", "tag2" },
    ReferenceDate = DateTime.Now.AddDays(-10)
});
#endregion
#endregion

#region Step 2 - Calculation of the residual risk for the baseline.
uint iterations = Node.DefaultIterations;

var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
var baseline = model.Simulate(iterations);
stopwatch1.Stop();

Console.WriteLine($"Residual Risk for the baseline calculated in {stopwatch1.ElapsedMilliseconds}ms ({Statistics.Simulations} * {iterations} samples generated).\n");

if (baseline != null)
{
    Console.WriteLine("--- Estimation of the Residual Risk for the baseline:");
    Console.WriteLine($"- {model.MinPercentile}th percentile: {baseline.Min.ToString("C0")}");
    Console.WriteLine($"- Mode: {baseline.Mode.ToString("C0")}");
    Console.WriteLine($"- {model.MaxPercentile}th percentile: {baseline.Max.ToString("C0")}");
    Console.WriteLine($"- Confidence: {baseline.Confidence}");
}
#endregion

#region Step 3 - Identification of the optimal set of mitigations.
Statistics.ResetSimulations();
var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
var mitigations = model.OptimizeMitigations(out var firstYearCosts, out var followingYearsCosts, iterations: iterations);
stopwatch2.Stop();

Console.WriteLine($"\nOptimization completed in {stopwatch2.ElapsedMilliseconds}ms ({Statistics.Simulations} * {iterations} samples generated).\n");

if (firstYearCosts != null)
{
    Console.WriteLine("--- Estimation of the Minimal Overall Yearly Cost for the first year:");
    Console.Write($"- {model.MinPercentile}th percentile: {firstYearCosts.Min.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Min - firstYearCosts.Min).ToString("C0")}, equal to {((baseline.Min - firstYearCosts.Min) / baseline.Min).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- Mode: {firstYearCosts.Mode.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Mode - firstYearCosts.Mode).ToString("C0")}), equal to {((baseline.Mode - firstYearCosts.Mode) / baseline.Mode).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- {model.MaxPercentile}th percentile: {firstYearCosts.Max.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Max - firstYearCosts.Max).ToString("C0")}), equal to {((baseline.Max - firstYearCosts.Max) / baseline.Max).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.WriteLine($"- Confidence: {firstYearCosts.Confidence}.");
}

if (followingYearsCosts != null)
{
    Console.WriteLine("\n--- Estimation of the Minimal Overall Yearly Cost for the following years:");
    Console.Write($"- {model.MinPercentile}th percentile: {followingYearsCosts.Min.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Min - followingYearsCosts.Min).ToString("C0")}), equal to {((baseline.Min - followingYearsCosts.Min) / baseline.Min).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- Mode: {followingYearsCosts.Mode.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Mode - followingYearsCosts.Mode).ToString("C0")}), equal to {((baseline.Mode - followingYearsCosts.Mode) / baseline.Mode).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- {model.MaxPercentile}th percentile: {followingYearsCosts.Max.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Max - followingYearsCosts.Max).ToString("C0")}), equal to {((baseline.Max - followingYearsCosts.Max) / baseline.Max).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.WriteLine($"- Confidence: {followingYearsCosts.Confidence}.");
}

if (mitigations?.Any() ?? false)
{
    Console.WriteLine("\n--- Mitigations to be applied:");
    foreach (var mitigation in mitigations)
    {
        Console.WriteLine($"\n{mitigation.Name}");
        Console.WriteLine($"- Implementation Costs: {mitigation.Min} - {mitigation.Mode} - {mitigation.Max} ({mitigation.Confidence})");
        if (mitigation.OperationCosts != null)
        {
            Console.WriteLine($"- Operation Costs: {mitigation.OperationCosts.Min} - {mitigation.OperationCosts.Mode} - {mitigation.OperationCosts.Max} ({mitigation.OperationCosts.Confidence})");
        }
    }
}
#endregion

#region Save the risk model to a file.
Console.WriteLine("\n--- Model Serialization.");
model.Serialize(@"c:\temp\RiskModel.json");
Console.WriteLine("Risk model saved to c:\\temp\\RiskModel.json.");

Console.WriteLine("\n--- Model reset.");
RiskModel.Reset();
model = RiskModel.Instance;
if (!(model.Risks?.Any() ?? false))
{
    Console.WriteLine("The Risk Model has been reset successfully.");

    Console.WriteLine("\n--- Model Deserialization.");
    if (RiskModel.Load(@"c:\temp\RiskModel.json"))
    {
        model = RiskModel.Instance;
        if (model.Risks?.Any() ?? false)
        {
            Console.WriteLine("Risk Model loaded successfully from c:\\temp\\RiskModel.json.");
            Console.WriteLine("Risks in the loaded model:");
            foreach (var risk in model.Risks)
            {
                Console.WriteLine($"- {risk.Name}");
                var fact = risk.Facts?.FirstOrDefault();
                if (fact != null)
                {
                    Console.WriteLine($"  - Fact: {fact.Name} ({fact.Context})");
                    if (fact is FactRange factRange)
                    {
                        Console.WriteLine($"    - Range: {factRange.Range.Min} - {factRange.Range.Mode} - {factRange.Range.Max} ({factRange.Range.Confidence})");
                    }
                    else if (fact is FactHardNumber factHardNumber)
                    {
                        Console.WriteLine($"    - Value: {factHardNumber.Value}");
                    }
                }
            }
            Console.WriteLine("Mitigations in the loaded model:");
            foreach (var mitigation in model.Mitigations)
            {
                Console.WriteLine($"- {mitigation.Name}");
            }
        }
        else
        {
            Console.WriteLine("No risks found in the loaded Risk Model.");
        }
    }
}
#endregion