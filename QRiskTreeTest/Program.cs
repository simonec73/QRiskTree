using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.OpenFAIR;

#region Step 1 - Definition of the risk model.
var model = RiskModel.Instance;

#region Risk1 - A simple risk calculated using only the LossEventFrequency and LossMagnitude nodes.
var risk1 = model.AddRisk("Risk 1: Unauthorized access to sensitive data.");
risk1.Add(new LossEventFrequency() { Perc10 = 1, Mode = 12, Perc90 = 120, Confidence = Confidence.Moderate });
risk1.Add(new LossMagnitude() { Perc10 = 100000, Mode = 500000, Perc90 = 20000000, Confidence = Confidence.Moderate});
#endregion

#region Risk2 - A more complex risk with a full, nested structure.
var risk2 = model.AddRisk("Risk 2: Data loss due to hardware failure.");

#region Calculation of the Loss Event Frequency.
var lef = new LossEventFrequency("Data loss event frequency");
risk2.Add(lef);

var tef = new ThreatEventFrequency("Data loss threat event frequency");
lef.Add(tef);
tef.Add(new ContactFrequency("Data Loss Contact Frequency") { Perc10=52, Mode=356, Perc90=3560, Confidence = Confidence.Moderate});
tef.Add(new ProbabilityOfAction("Data loss probability of action") { Perc10 = 0.01, Mode = 0.1, Perc90 = 0.5, Confidence = Confidence.Moderate });

var vuln = new Vulnerability("Data loss vulnerability");
lef.Add(vuln);
vuln.Add(new ThreatCapability("Data loss threat capability") { Perc10 = 0.1, Mode = 0.5, Perc90 = 0.9, Confidence = Confidence.Moderate });
vuln.Add(new ResistenceStrength("Data loss resistance strength") { Perc10 = 0.2, Mode = 0.5, Perc90 = 0.75, Confidence = Confidence.Moderate });
#endregion

#region Calculation of the Loss Magnitude.
var mag = new LossMagnitude("Data loss magnitude");
risk2.Add(mag);

mag.Add(new PrimaryLoss("Primary loss: Productivity") { Form = PrimaryLossForm.Productivity, Perc10 = 0, Mode = 100000, Perc90 = 1000000, Confidence = Confidence.Low });
mag.Add(new PrimaryLoss("Primary loss: Replacement") { Form = PrimaryLossForm.Replacement, Perc10 = 1000, Mode = 2000, Perc90 = 3000, Confidence = Confidence.High });
mag.Add(new PrimaryLoss("Primary loss: Response") { Form = PrimaryLossForm.Response, Perc10 = 50000, Mode = 100000, Perc90 = 1000000, Confidence = Confidence.Low });

var sec1 = new SecondaryRisk("Secondary risk: Reputation") { Form = SecondaryLossForm.Reputation };
mag.Add(sec1);
sec1.Add(new SecondaryLossEventFrequency("Reputation event frequency") { Perc10 = 0.01, Mode = 0.02, Perc90 = 0.2, Confidence = Confidence.Moderate });
sec1.Add(new SecondaryLossMagnitude("Reputation loss magnitude") { Perc10 = 10000, Mode = 50000, Perc90 = 200000, Confidence = Confidence.Low });

var sec2 = new SecondaryRisk("Secondary risk: Competitive Advantage") { Form = SecondaryLossForm.Competitive_Advantage };
mag.Add(sec2);
sec2.Add(new SecondaryLossEventFrequency("Competitive Advantage event frequency") { Perc10 = 0.01, Mode = 0.02, Perc90 = 0.1, Confidence = Confidence.Moderate });
sec2.Add(new SecondaryLossMagnitude("Competitive Advantage loss magnitude") { Perc10 = 100000, Mode = 500000, Perc90 = 2000000, Confidence = Confidence.Low });
#endregion
#endregion

#region Risk3 - A third risk, using only LossEventFrequency and LossMagnitude nodes.
var risk3 = model.AddRisk("Risk 3: Phishing attack leading to credential theft.");
risk3.Add(new LossEventFrequency() { Perc10 = 12, Mode = 60, Perc90 = 1200, Confidence = Confidence.Moderate });
risk3.Add(new LossMagnitude() { Perc10 = 100, Mode = 500, Perc90 = 1500, Confidence = Confidence.Low });
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

Console.WriteLine($"Residual Risk for the baseline calculated in {stopwatch1.ElapsedMilliseconds} ms ({iterations} iterations).\n");

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
var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
var mitigations = model.OptimizeMitigations(out var firstYearCosts, out var followingYearsCosts, iterations: iterations);
stopwatch2.Stop();

Console.WriteLine($"\nOptimization completed in {stopwatch2.ElapsedMilliseconds} ms ({iterations} iterations).\n");

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
