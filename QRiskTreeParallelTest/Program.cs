using QRiskTreeParallelTest;

uint iterations = 100000;
int runs = 8;

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

try
{
    // Step 1: Create 4 instances of QRiskTreeEngineTester
    var testers = new QRiskTreeEngineTester[runs];
    for (int i = 0; i < runs; i++)
    {
        testers[i] = new QRiskTreeEngineTester();
    }

    // Step 2: Start 4 tasks to run SimulateAsync in parallel
    var tasks = new Task<ParallelTestSimulationResult?>[runs];
    for (int i = 0; i < runs; i++)
    {
        tasks[i] = testers[i].SimulateAsync(iterations);
    }

    // Step 3: Await all tasks and print the elapsed times
    var results = await Task.WhenAll(tasks);
    for (int i = 0; i < results.Length; i++)
    {
        var result = results[i];
        if (result != null)
            Console.WriteLine($"Tester {i + 1} - Time: {result?.ElapsedTime} ms - Min: {result?.Min} - Mode: {result?.Mode} - Max: {result?.Max}");
    }
}
finally 
{ 
    stopwatch.Stop(); 
}
Console.WriteLine($"Total elapsed time for all testers: {stopwatch.ElapsedMilliseconds} ms");