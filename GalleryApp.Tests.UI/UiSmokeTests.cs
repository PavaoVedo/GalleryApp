using Microsoft.Playwright;
using Xunit;

namespace GalleryApp.Tests.UI;

public class UiSmokeTests : IAsyncLifetime
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("UI_BASE_URL") ?? "http://localhost:8080";

    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _pw.Dispose();
    }

    [Fact]
    public async Task HomePage_Loads_WithTitle()
    {
        var page = await _browser.NewPageAsync();
        var resp = await page.GotoAsync(BaseUrl);
        Assert.NotNull(resp);
        Assert.True(resp!.Ok, $"Home returned HTTP {resp.Status}");
        Assert.False(string.IsNullOrWhiteSpace(await page.TitleAsync()));
    }

    [Fact]
    public async Task LoginPage_ShowsEmailAndPasswordInputs()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Identity/Account/Login");
        await Assertions.Expect(page.Locator("#Input_Email")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Input_Password")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SearchPage_IsReachableAnonymously()
    {
        var page = await _browser.NewPageAsync();
        var resp = await page.GotoAsync($"{BaseUrl}/Photos/Search");
        Assert.True(resp!.Ok);
    }
}