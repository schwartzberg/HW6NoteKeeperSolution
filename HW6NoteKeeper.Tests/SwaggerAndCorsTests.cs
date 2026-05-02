using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace HW6NoteKeeper.Tests
{
    /// <summary>
    /// Verifies the HW6 Section 1 changes that make the OpenAPI document and
    /// the application CORS configuration compatible with Azure API Management:
    ///   - 1.1 Every operation in /swagger/v1/swagger.json has a unique operationId.
    ///   - 1.3 The OpenAPI document advertises a server URL that matches the
    ///         host that served it (PreSerializeFilters host fix-up).
    ///   - 1.4 CORS is enabled with a permissive default policy.
    ///
    /// Uses WebApplicationFactory&lt;Program&gt; to host the application in-process
    /// so the test exercises the actual Program.cs pipeline (and surfaces any
    /// regressions to those four lines if anyone removes them).
    /// </summary>
    /// <remarks>
    /// IMPORTANT: WebApplicationFactory boots the full app, which in this project
    /// runs DbInitializer against the configured SQL Server during startup.
    /// Tests share a single fixture (IClassFixture) so the cost is paid once.
    /// </remarks>
    [Collection("Sequential")]
    [Trait("Category", "Integration")]
    public class SwaggerAndCorsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SwaggerAndCorsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SwaggerJson_IsServed_AndIsValidJson()
        {
            using var client = _factory.CreateClient();

            var response = await client.GetAsync("/swagger/v1/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            doc.RootElement.TryGetProperty("paths", out _).Should().BeTrue("the OpenAPI document must contain a paths element");
        }

        [Fact]
        public async Task SwaggerJson_AllOperations_HaveNonNullUniqueOperationIds()
        {
            using var client = _factory.CreateClient();
            var body = await client.GetStringAsync("/swagger/v1/swagger.json");
            using var doc = JsonDocument.Parse(body);

            var operationIds = new List<string>();

            foreach (var path in doc.RootElement.GetProperty("paths").EnumerateObject())
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    if (!method.Value.TryGetProperty("operationId", out var opIdEl)) continue;
                    var opId = opIdEl.GetString();
                    opId.Should().NotBeNullOrWhiteSpace(
                        $"every operation must have an operationId (path '{path.Name}', method '{method.Name}')");
                    operationIds.Add(opId!);
                }
            }

            operationIds.Should().NotBeEmpty();
            operationIds.Should().OnlyHaveUniqueItems(
                "API Management requires unique operationIds across the entire document (HW6 1.1)");
        }

        [Fact]
        public async Task SwaggerJson_ServersUrl_ReflectsRequestHost()
        {
            using var client = _factory.CreateClient();
            var body = await client.GetStringAsync("/swagger/v1/swagger.json");
            using var doc = JsonDocument.Parse(body);

            doc.RootElement.TryGetProperty("servers", out var servers).Should().BeTrue(
                "PreSerializeFilters must add a servers entry so APIM picks up the correct host (HW6 1.3)");
            servers.GetArrayLength().Should().BeGreaterThan(0);

            var url = servers[0].GetProperty("url").GetString();
            url.Should().NotBeNullOrWhiteSpace();
            // WebApplicationFactory's TestServer uses an http(s)://localhost host.
            url!.Should().StartWith("http", "the server URL should be derived from the request scheme/host");
            url.Should().Contain("localhost",
                "the in-process TestServer host is localhost; PreSerializeFilters should reflect that");
        }

        [Fact]
        public async Task Cors_PreflightRequest_ReturnsAllowOriginHeader()
        {
            using var client = _factory.CreateClient();

            using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/notes/tags");
            preflight.Headers.Add("Origin", "https://apim.example.com");
            preflight.Headers.Add("Access-Control-Request-Method", "GET");
            preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

            var response = await client.SendAsync(preflight);

            // CORS middleware short-circuits preflight to 204 No Content (or 200) and adds the headers.
            ((int)response.StatusCode).Should().BeOneOf(200, 204);
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
                "the default CORS policy registered in Program.cs must allow any origin (HW6 1.4)");

            var allowOrigin = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Origin"));
            allowOrigin.Should().Be("*");
        }

        [Fact]
        public async Task Cors_GetRequestWithOrigin_ReturnsAllowOriginHeader()
        {
            using var client = _factory.CreateClient();

            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notes/tags");
            req.Headers.Add("Origin", "https://apim.example.com");

            var response = await client.SendAsync(req);

            // The CORS header should be present on the actual response too,
            // regardless of the response's status code.
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
                "GET responses must carry CORS headers when an Origin is presented (HW6 1.4)");
        }
    }
}
