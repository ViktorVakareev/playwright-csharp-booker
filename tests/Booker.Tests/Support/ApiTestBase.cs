using System.Text.Json;
using Microsoft.Playwright.NUnit;

namespace Booker.Tests.Support;

/// <summary>
/// Base for API tests: a Playwright APIRequestContext per test (no browser needed),
/// pointed at Restful-Booker, with JSON headers and small helpers for auth and test data.
/// </summary>
public abstract class ApiTestBase : PlaywrightTest
{
    protected IAPIRequestContext Api = null!;

    [SetUp]
    public async Task CreateApiContext() =>
        Api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Settings.BookerUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",          // Restful-Booker answers 418 without it
                ["Content-Type"] = "application/json",
            },
        });

    [TearDown]
    public async Task DisposeApiContext() => await Api.DisposeAsync();

    protected static string ToJson<T>(T value) => JsonSerializer.Serialize(value);

    protected static async Task<T> ReadJsonAsync<T>(IAPIResponse response) =>
        JsonSerializer.Deserialize<T>(await response.TextAsync())
        ?? throw new InvalidOperationException($"Empty JSON in response from {response.Url}");

    protected async Task<string> CreateTokenAsync()
    {
        var response = await Api.PostAsync("/auth", new()
        {
            DataString = ToJson(new { username = Settings.AdminUser, password = Settings.AdminPassword }),
        });
        using var json = JsonDocument.Parse(await response.TextAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    protected async Task<CreatedBooking> CreateBookingAsync(Booking booking)
    {
        var response = await Api.PostAsync("/booking", new() { DataString = ToJson(booking) });
        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());
        return await ReadJsonAsync<CreatedBooking>(response);
    }

    protected static Dictionary<string, string> AuthCookie(string token) => new() { ["Cookie"] = $"token={token}" };
}
