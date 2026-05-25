using System.Net;
using System.Text;
using LunchYolo2.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace lunch_yolo2.Tests;

public class PlaywrightFixture : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;
    public string ServerAddress { get; private set; } = string.Empty;
    public List<string> CapturedUrls { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var handler = new MockHttpHandler(CapturedUrls);
            services.AddHttpClient<IWeatherService, WeatherService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddHttpClient<IStockIndexService, StockIndexService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the in-memory test host first (required by WebApplicationFactory internals)
        var testHost = builder.Build();

        // Add Kestrel on a random port for Playwright to connect to
        builder.ConfigureWebHost(b =>
            b.UseKestrel(o => o.Listen(System.Net.IPAddress.Loopback, 0)));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()!;
        ServerAddress = addresses.Addresses.First();

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _kestrelHost?.Dispose();
        base.Dispose(disposing);
    }
}

// Intercepts outbound HTTP calls, records URLs, returns realistic fake responses.
class MockHttpHandler(List<string> capturedUrls) : HttpMessageHandler
{
    private const string WeatherJson = """
        {"current":{"temperature_2m":18.5,"weathercode":1}}
        """;

    private const string StockJson = """
        {"chart":{"result":[{
          "timestamp":[1747526400,1747612800,1747699200],
          "indicators":{"quote":[{"close":[100.0,101.5,102.0]}]}
        }]}}
        """;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        capturedUrls.Add(url);

        var body = url.Contains("yahoo") ? StockJson : WeatherJson;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}

// Starts the server once for the entire test assembly.
[SetUpFixture]
public class TestAssemblySetup
{
    public static PlaywrightFixture Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public void StartServer()
    {
        Factory = new PlaywrightFixture();
        Factory.CreateClient(); // forces host creation and populates ServerAddress
    }

    [OneTimeTearDown]
    public void StopServer() => Factory.Dispose();
}
