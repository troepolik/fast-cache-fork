using System.Diagnostics;

namespace CachedExample;

public static class GetOrCompute
{
    private static readonly HttpClient Http = new();

    public static async ValueTask Run()
    {
        const string query = "Kyiv?format=3";
        var stopwatch = Stopwatch.StartNew();
        var oneSecond = TimeSpan.FromSeconds(1);

        // 'Cached.LastOrCompute': Either get last cached value or execute delegate to retrieve and store new value in the cache
        var weather = await Cached.GetOrCompute(query, FetchWeather, oneSecond);
        Console.WriteLine($"New: {weather} in {stopwatch.ElapsedMilliseconds}");

        // Call 'LastOrCompute' for the second time. Since we already called it once with the parameter 'query',
        // the method immediately returns the value we received earlier. By using ValueTask<T> we ensure there are no allocations
        // Note: starting with .NET 7, passing a 'MethodGroup' as 'Func' parameter will no longer allocate
        stopwatch.Restart();
        var cachedWeather = await Cached.GetOrCompute(query, FetchWeather, oneSecond);
        Console.WriteLine($"Cached: {cachedWeather} in {stopwatch.ElapsedMilliseconds}");

        // In fact, the second call has returned the same cached string instance we received earlier from 'FetchWeather'
        Debug.Assert(ReferenceEquals(weather, cachedWeather));

        ThreadPool.QueueUserWorkItem(_ => Seed(32));
        ThreadPool.QueueUserWorkItem(_ => Seed(512UL));
        ThreadPool.QueueUserWorkItem(_ => Seed(1337L));
        ThreadPool.QueueUserWorkItem(_ => Seed(new { Date = DateTime.Now, Bytes = new byte[] { 1, 2, 3, 4 } }));
        ThreadPool.QueueUserWorkItem(_ => Seed(new { A = "hello world", B = 420 }));

        ThreadPool.QueueUserWorkItem(_ => Seed(32U));
        ThreadPool.QueueUserWorkItem(_ => Seed(512F));
        ThreadPool.QueueUserWorkItem(_ => Seed(1337D));
        ThreadPool.QueueUserWorkItem(_ => Seed(new { Date = DateTime.Now, Ints = new int[] { 1, 2, 3, 4 } }));
        ThreadPool.QueueUserWorkItem(_ => Seed(new { A = "hello world", B = 420F }));

        // Wait for one second
        await Task.Delay(oneSecond);

        // Retrieve weather once again because previously cached value has expired
        stopwatch.Restart();
        weather = await Cached.GetOrCompute(query, FetchWeather, oneSecond);
        Console.WriteLine($"New: {weather} in {stopwatch.ElapsedMilliseconds}");
        while (Console.ReadLine() != "S")
        {
            Console.WriteLine("Doing full GC");
            GC.Collect();
        }
    }

    private static async Task<string> FetchWeather(string query)
    {
        Console.WriteLine($"Fetching weather for {query}");
        return await Http.GetStringAsync($"https://wttr.in/{query}");
    }

    private static void Seed<T>(T value) where T : notnull
    {
        const int count = 1_000_000;

        for (int i = 0; i < count; i++)
        {
            value.Cache(i, TimeSpan.FromSeconds(30));
        }

        Console.WriteLine($"Added {count} of {typeof(T).Name} to cache.");
    }
}
