using System.Net;
using System.Net.Http.Json;
using Longa.Application.Common.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Longa.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsValidHealthResponse()
    {
        var response = await _client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.NotNull(response);
        Assert.Equal("ok", response.Status);
        Assert.True(response.Timestamp <= DateTime.UtcNow.AddSeconds(1));
    }
}

public class SearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Suggest_WithoutQ_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/search/suggest?session_token=550e8400-e29b-41d4-a716-446655440000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Suggest_WithoutSessionToken_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/search/suggest?q=Paris");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Retrieve_WithoutSessionToken_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/search/retrieve/some-mapbox-id");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
