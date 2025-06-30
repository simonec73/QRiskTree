using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.OpenFAIR;

#region Step 1 - Definition of the risk model.
var model = RiskModel.Instance;

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

mag.AddPrimaryLoss("Primary loss: Productivity").Set(PrimaryLossForm.Productivity).Set(0, 100000, 1000000, Confidence.Low);
mag.AddPrimaryLoss("Primary loss: Replacement").Set(PrimaryLossForm.Replacement).Set(1000, 2000, 3000, Confidence.High);
mag.AddPrimaryLoss("Primary loss: Response").Set(PrimaryLossForm.Response).Set(50000, 100000, 1000000, Confidence.Low);

var sec1 = mag.AddSecondaryRisk("Secondary risk: Reputation").Set(SecondaryLossForm.Reputation);
sec1.AddSecondaryLossEventFrequency("Reputation event frequency").Set(0.01, 0.02, 0.2, Confidence.Moderate);
sec1.AddSecondaryLossMagnitude("Reputation loss magnitude").Set(10000, 50000, 200000, Confidence.Low);

var sec2 = mag.AddSecondaryRisk("Secondary risk: Competitive Advantage").Set(SecondaryLossForm.Competitive_Advantage);
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
var mitigation1 = model.AddMitigation("Mitigation 1: Implement multi-factor authentication.");
mitigation1.Perc10 = 5000;
mitigation1.Mode = 10000;
mitigation1.Perc90 = 20000;
mitigation1.Confidence = Confidence.Moderate;
mitigation1.OperationCosts = new QRiskTree.Engine.Range(RangeType.Money, 
    1200, 6000, 20000, Confidence.Moderate);
if (risk1.ApplyMitigation(mitigation1, out var r1Mitigation1))
{
    r1Mitigation1.Perc10 = 0.1;
    r1Mitigation1.Mode = 0.2;
    r1Mitigation1.Perc90 = 0.5;
    r1Mitigation1.Confidence = Confidence.Moderate;
}
if (risk3.ApplyMitigation(mitigation1, out var r3Mitigation1))
{
    r3Mitigation1.Perc10 = 0.5;
    r3Mitigation1.Mode = 0.75;
    r3Mitigation1.Perc90 = 0.9;
    r3Mitigation1.Confidence = Confidence.Moderate;
}

var mitigation2 = model.AddMitigation("Mitigation 2: Regular data backups.");
mitigation2.Perc10 = 5000;
mitigation2.Mode = 10000;
mitigation2.Perc90 = 20000;
mitigation2.Confidence = Confidence.High;
mitigation2.OperationCosts = new QRiskTree.Engine.Range(RangeType.Money,
    1200, 12000, 24000, Confidence.Moderate);
if (risk2.ApplyMitigation(mitigation2, out var r2Mitigation2))
{
    r2Mitigation2.Perc10 = 0.3;
    r2Mitigation2.Mode = 0.67;
    r2Mitigation2.Perc90 = 0.9;
    r2Mitigation2.Confidence = Confidence.High;
}

var mitigation3 = model.AddMitigation("Mitigation 3: Employee training on phishing awareness.");
mitigation3.Perc10 = 2000000;
mitigation3.Mode = 3000000;
mitigation3.Perc90 = 50000000;
mitigation3.Confidence = Confidence.Moderate;
mitigation1.OperationCosts = new QRiskTree.Engine.Range(RangeType.Money,
    10000, 20000, 50000, Confidence.Moderate);
if (risk3.ApplyMitigation(mitigation3, out var r3Mitigation3))
{
    r3Mitigation3.Perc10 = 0.0001;
    r3Mitigation3.Mode = 0.00025;
    r3Mitigation3.Perc90 = 0.0004;
    r3Mitigation3.Confidence = Confidence.Moderate;
}
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
    Console.WriteLine($"- 10th percentile: {baseline.Perc10.ToString("C0")}");
    Console.WriteLine($"- Mode: {baseline.Mode.ToString("C0")}");
    Console.WriteLine($"- 90th percentile: {baseline.Perc90.ToString("C0")}");
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
    Console.Write($"- 10th percentile: {firstYearCosts.Perc10.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Perc10 - firstYearCosts.Perc10).ToString("C0")}, equal to {((baseline.Perc10 - firstYearCosts.Perc10) / baseline.Perc10).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- Mode: {firstYearCosts.Mode.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Mode - firstYearCosts.Mode).ToString("C0")}), equal to {((baseline.Mode - firstYearCosts.Mode) / baseline.Mode).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- 90th percentile: {firstYearCosts.Perc90.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Perc90 - firstYearCosts.Perc90).ToString("C0")}), equal to {((baseline.Perc90 - firstYearCosts.Perc90) / baseline.Perc90).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.WriteLine($"- Confidence: {firstYearCosts.Confidence}.");
}

if (followingYearsCosts != null)
{
    Console.WriteLine("\n--- Estimation of the Minimal Overall Yearly Cost for the following years:");
    Console.Write($"- 10th percentile: {followingYearsCosts.Perc10.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Perc10 - followingYearsCosts.Perc10).ToString("C0")}), equal to {((baseline.Perc10 - followingYearsCosts.Perc10) / baseline.Perc10).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- Mode: {followingYearsCosts.Mode.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Mode - followingYearsCosts.Mode).ToString("C0")}), equal to {((baseline.Mode - followingYearsCosts.Mode) / baseline.Mode).ToString("P2")}).");
    else
        Console.WriteLine(".");
    Console.Write($"- 90th percentile: {followingYearsCosts.Perc90.ToString("C0")}");
    if (baseline != null)
        Console.WriteLine($" (saving {(baseline.Perc90 - followingYearsCosts.Perc90).ToString("C0")}), equal to {((baseline.Perc90 - followingYearsCosts.Perc90) / baseline.Perc90).ToString("P2")}).");
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
        Console.WriteLine($"- Implementation Costs: {mitigation.Perc10} - {mitigation.Mode} - {mitigation.Perc90} ({mitigation.Confidence})");
        if (mitigation.OperationCosts != null)
        {
            Console.WriteLine($"- Operation Costs: {mitigation.OperationCosts.Perc10} - {mitigation.OperationCosts.Mode} - {mitigation.OperationCosts.Perc90} ({mitigation.OperationCosts.Confidence})");
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