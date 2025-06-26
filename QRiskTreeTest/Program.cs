// See https://aka.ms/new-console-template for more information
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.OpenFAIR;

var model = RiskModel.Instance;

var risk1 = model.AddRisk();
risk1.Name = "Risk 1: Unauthorized access to sensitive data.";
risk1.Add(new LossEventFrequency() { Perc10 = 1, Mode = 12, Perc90 = 120, Confidence = QRiskTree.Engine.Confidence.Moderate });
risk1.Add(new LossMagnitude() { Perc10 = 100000, Mode = 500000, Perc90 = 20000000, Confidence = QRiskTree.Engine.Confidence.Moderate});

var risk2 = model.AddRisk();
risk2.Name = "Risk 2: Data loss due to hardware failure.";
risk2.Add(new LossEventFrequency() { Perc10 = 1, Mode = 2, Perc90 = 10, Confidence = QRiskTree.Engine.Confidence.High });
risk2.Add(new LossMagnitude() { Perc10 = 100000, Mode = 200000, Perc90 = 1000000, Confidence = QRiskTree.Engine.Confidence.Moderate });

var risk3 = model.AddRisk();
risk3.Name = "Risk 3: Phishing attack leading to credential theft.";
risk3.Add(new LossEventFrequency() { Perc10 = 12, Mode = 60, Perc90 = 1200, Confidence = QRiskTree.Engine.Confidence.Moderate });
risk3.Add(new LossMagnitude() { Perc10 = 100, Mode = 500, Perc90 = 1500, Confidence = QRiskTree.Engine.Confidence.Low });

var mitigation1 = model.AddMitigation();
mitigation1.Name = "Mitigation 1: Implement multi-factor authentication.";
mitigation1.Perc10 = 30000;
mitigation1.Mode = 50000;
mitigation1.Perc90 = 100000;
mitigation1.Confidence = QRiskTree.Engine.Confidence.Moderate;
mitigation1.OperationCosts = new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Money, 
    1200, 12000, 30000, QRiskTree.Engine.Confidence.Moderate);
if (risk1.ApplyMitigation(mitigation1, out var r1Mitigation1))
{
    r1Mitigation1.Perc10 = 0.1;
    r1Mitigation1.Mode = 0.2;
    r1Mitigation1.Perc90 = 0.5;
    r1Mitigation1.Confidence = QRiskTree.Engine.Confidence.Moderate;
}
if (risk3.ApplyMitigation(mitigation1, out var r3Mitigation1))
{
    r3Mitigation1.Perc10 = 0.1;
    r3Mitigation1.Mode = 0.5;
    r3Mitigation1.Perc90 = 0.75;
    r3Mitigation1.Confidence = QRiskTree.Engine.Confidence.Moderate;
}

var mitigation2 = model.AddMitigation();
mitigation2.Name = "Mitigation 2: Regular data backups.";
mitigation2.Perc10 = 10000;
mitigation2.Mode = 20000;
mitigation2.Perc90 = 40000;
mitigation2.Confidence = QRiskTree.Engine.Confidence.High;
mitigation2.OperationCosts = new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Money,
    1200, 24000, 48000, QRiskTree.Engine.Confidence.Moderate);
if (risk2.ApplyMitigation(mitigation2, out var r2Mitigation2))
{
    r2Mitigation2.Perc10 = 0.3;
    r2Mitigation2.Mode = 0.67;
    r2Mitigation2.Perc90 = 0.9;
    r2Mitigation2.Confidence = QRiskTree.Engine.Confidence.High;
}

var mitigation3 = model.AddMitigation();
mitigation3.Name = "Mitigation 3: Employee training on phishing awareness.";
mitigation3.Perc10 = 2000000;
mitigation3.Mode = 3000000;
mitigation3.Perc90 = 50000000;
mitigation3.Confidence = QRiskTree.Engine.Confidence.Moderate;
mitigation1.OperationCosts = new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Money,
    10000, 20000, 50000, QRiskTree.Engine.Confidence.Moderate);
if (risk3.ApplyMitigation(mitigation3, out var r3Mitigation3))
{
    r3Mitigation3.Perc10 = 0.0001;
    r3Mitigation3.Mode = 0.00025;
    r3Mitigation3.Perc90 = 0.0004;
    r3Mitigation3.Confidence = QRiskTree.Engine.Confidence.Moderate;
}

var mitigations = model.OptimizeMitigations(out var firstYearCosts, out var followingYearsCosts);

if (firstYearCosts != null)
{
    Console.WriteLine("--- Estimation of the Minimal Overall Yearly Cost for the first year:");
    Console.WriteLine($"- 10%: {firstYearCosts.Perc10.ToString("C0")}");
    Console.WriteLine($"- Mode: {firstYearCosts.Mode.ToString("C0")}");
    Console.WriteLine($"- 90%: {firstYearCosts.Perc90.ToString("C0")}");
    Console.WriteLine($"- Confidence: ({firstYearCosts.Confidence})");

    Console.WriteLine("\n--- Estimation of the Minimal Overall Yearly Cost for the following years:");
    Console.WriteLine($"- 10%: {followingYearsCosts.Perc10.ToString("C0")}");
    Console.WriteLine($"- Mode: {followingYearsCosts.Mode.ToString("C0")}");
    Console.WriteLine($"- 90%: {followingYearsCosts.Perc90.ToString("C0")}");
    Console.WriteLine($"- Confidence: ({followingYearsCosts.Confidence})");


    Console.WriteLine("\n--- Mitigations to be applied:");

    if (mitigations?.Any() ?? false)
    {
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
}

