using TaskWeaverLib;

var count = 0;

Console.WriteLine("Hello, Thready!");
var worker = new TaskWeaver((ct) => IncreaseCount(),
    async () => Console.WriteLine("Executing before stop"));

await worker.StartAsync();

Console.ReadKey();
await worker.StopAsync();

async Task IncreaseCount()
{
    count++;
    Console.WriteLine(count);
}