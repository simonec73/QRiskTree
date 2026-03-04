using QRiskTree.Engine;
using QRiskTreeExtensibilityTest.MyModel;

var model = new RiskModel();

var firstRisk = model.AddRisk("First");
firstRisk.Add(new Probability("First probability").Set<Node>(0.1, 0.25, 0.5, Confidence.Moderate));
firstRisk.Add(new Impact("First impact").Set<Node>(1000, 5000, 10000, Confidence.Moderate));

var secondRisk = model.AddRisk("Second");
secondRisk.Add(new Probability("Second probability").Set<Node>(0.25, 0.5, 0.9, Confidence.High));
secondRisk.Add(new Impact("Second impact").Set<Node>(500, 2000, 5000, Confidence.Moderate));

var result = model.Simulate(10000);

Console.WriteLine($"Min: {result?.Min ?? -1}");
Console.WriteLine($"Mode: {result?.Mode ?? -1}");
Console.WriteLine($"Max: {result?.Max ?? -1}");
Console.WriteLine($"Confidence: {result?.Confidence ?? Confidence.Low}");