using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace lunch_yolo2.Tests;

[TestFixture]
public class OfficeCardTests : PageTest
{
    private string ServerAddress => TestAssemblySetup.Factory.ServerAddress;

    [SetUp]
    public async Task NavigateToHome()
    {
        TestAssemblySetup.Factory.CapturedUrls.Clear();
        await Page.GotoAsync(ServerAddress, new() { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [Test]
    public async Task ThreeOfficeCards_Render()
    {
        await Expect(Page.Locator(".widget-label")).ToHaveCountAsync(3);
    }

    [Test]
    public async Task StockPrices_AreVisibleAndNumeric()
    {
        var prices = Page.Locator(".ftse-price");
        await Expect(prices).ToHaveCountAsync(3);

        for (var i = 0; i < 3; i++)
        {
            var text = await prices.Nth(i).TextContentAsync() ?? "";
            Assert.That(text.Trim(), Is.Not.Empty, $"Office {i}: stock price is empty");
            var normalized = text.Replace(",", "").Replace(".", "");
            Assert.That(normalized, Does.Match(@"^\d+$"), $"Office {i}: price '{text}' is not numeric");
        }
    }

    [Test]
    public async Task WeatherTemps_ContainDegreeSymbol()
    {
        var temps = Page.Locator(".weather-temp");
        await Expect(temps).ToHaveCountAsync(3);

        for (var i = 0; i < 3; i++)
        {
            var text = await temps.Nth(i).TextContentAsync() ?? "";
            Assert.That(text, Does.Contain("°"), $"Office {i}: weather temp '{text}' missing degree symbol");
        }
    }

    [Test]
    public async Task Sparklines_HaveNonEmptyPoints()
    {
        var lines = Page.Locator("polyline");
        await Expect(lines).ToHaveCountAsync(3);

        for (var i = 0; i < 3; i++)
        {
            var points = await lines.Nth(i).GetAttributeAsync("points") ?? "";
            Assert.That(points.Trim(), Is.Not.Empty, $"Office {i}: sparkline has no points");
        }
    }

    [Test]
    public void TickerUrls_AreNotDoubleEncoded()
    {
        // Regression test for the %5E → %255E double-encoding bug (issue #9).
        // The config stores raw symbols (^FTSE etc); Uri.EscapeDataString encodes
        // ^ to %5E exactly once. If config reverts to pre-encoded values (%5EFTSE),
        // Uri.EscapeDataString would produce %255EFTSE and Yahoo returns 404.
        var yahooUrls = TestAssemblySetup.Factory.CapturedUrls
            .Where(u => u.Contains("yahoo"))
            .ToList();

        Assert.That(yahooUrls, Is.Not.Empty, "No Yahoo Finance requests were captured");

        foreach (var url in yahooUrls)
            Assert.That(url, Does.Not.Contain("%25"),
                $"Double-encoded URL detected: {url}");
    }
}
