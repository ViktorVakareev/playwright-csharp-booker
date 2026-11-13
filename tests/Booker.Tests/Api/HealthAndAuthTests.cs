using System.Text.Json;
using Booker.Tests.Support;

namespace Booker.Tests.Api;

/// <summary>API tests against the real Restful-Booker service, through Playwright's APIRequestContext.</summary>
[TestFixture, Category("Api")]
public class HealthAndAuthTests : ApiTestBase
{
    [Test]
    public async Task Ping_reports_the_service_is_up()
    {
        var response = await Api.GetAsync("/ping");

        Assert.That(response.Status, Is.EqualTo(201));   // Restful-Booker's health check returns 201 Created
    }

    [Test]
    public async Task Valid_credentials_return_a_token()
    {
        var token = await CreateTokenAsync();

        Assert.That(token, Is.Not.Empty);
    }

    [Test]
    public async Task Bad_credentials_are_refused_with_a_reason()
    {
        var response = await Api.PostAsync("/auth", new() { DataString = ToJson(new { username = "admin", password = "wrong" }) });
        using var json = JsonDocument.Parse(await response.TextAsync());

        Assert.Multiple(() =>
        {
            // Worth mentioning in an interview: the API says "no" with 200 OK, so checking the status alone would pass.
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(json.RootElement.GetProperty("reason").GetString(), Is.EqualTo("Bad credentials"));
            Assert.That(json.RootElement.TryGetProperty("token", out _), Is.False);
        });
    }
}
