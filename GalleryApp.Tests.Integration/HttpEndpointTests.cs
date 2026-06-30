using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GalleryApp.Tests.Integration;

public class HttpEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public HttpEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_Home_ReturnsHtmlSuccess()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/");
        resp.EnsureSuccessStatusCode();
        Assert.Contains("text/html", resp.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Get_Metrics_ExposesCustomGalleryMetrics()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/metrics");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("galleryapp_", body);   
    }

    [Fact]
    public async Task Get_Upload_WhenAnonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/Photos/Upload");
        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.Contains("/Account/Login", resp.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Post_Search_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/Photos/Search",
            new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Get_SearchPage_IsAnonymouslyAccessible()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/Photos/Search");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"Status {(int)resp.StatusCode}. Body:\n{body}");
    }
}