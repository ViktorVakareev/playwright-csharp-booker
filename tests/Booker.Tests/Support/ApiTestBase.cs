using System.Text;
using System.Text.Json;
using Microsoft.Playwright.NUnit;

namespace Booker.Tests.Support;

/// <summary>
/// Base for API tests: a Playwright APIRequestContext per test (no browser needed),
/// pointed at Restful-Booker, with JSON headers and small helpers for auth and test data.
/// Every booking a test creates is tracked and deleted after the test, so runs leave no data behind.
/// </summary>
public abstract class ApiTestBase : PlaywrightTest
{
    protected IAPIRequestContext Api = null!;
    private readonly List<int> _createdBookingIds = [];

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
    public async Task CleanUpAndDisposeApiContext()
    {
        try
        {
            // Best effort: a booking the test already deleted answers 405, which is fine here.
            foreach (var id in _createdBookingIds)
                await Api.DeleteAsync($"/booking/{id}", new() { Headers = BasicAuth() });
        }
        finally
        {
            await Api.DisposeAsync();
        }
    }

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

    /// <summary>Creates a valid booking, asserts it was accepted, and tracks it for cleanup.</summary>
    protected async Task<CreatedBooking> CreateBookingAsync(Booking booking)
    {
        var response = await Api.PostAsync("/booking", new() { DataString = ToJson(booking) });
        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());
        var created = await ReadJsonAsync<CreatedBooking>(response);
        _createdBookingIds.Add(created.BookingId);
        return created;
    }

    /// <summary>
    /// Posts any body as-is (for negative tests). If the API accepts it anyway, the new booking is tracked for cleanup.
    /// </summary>
    protected async Task<IAPIResponse> PostRawBookingAsync(string body)
    {
        var response = await Api.PostAsync("/booking", new() { DataString = body });
        if (response.Ok)
        {
            using var json = JsonDocument.Parse(await response.TextAsync());
            if (json.RootElement.TryGetProperty("bookingid", out var id)) _createdBookingIds.Add(id.GetInt32());
        }
        return response;
    }

    /// <summary>GET /booking with filters; returns the matching booking ids.</summary>
    protected async Task<List<int>> SearchAsync(Dictionary<string, object> filters)
    {
        var response = await Api.GetAsync("/booking", new() { Params = filters });
        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());
        using var json = JsonDocument.Parse(await response.TextAsync());
        return json.RootElement.EnumerateArray().Select(e => e.GetProperty("bookingid").GetInt32()).ToList();
    }

    protected static Dictionary<string, string> AuthCookie(string token) => new() { ["Cookie"] = $"token={token}" };

    /// <summary>Restful-Booker's second auth option: HTTP Basic with the admin credentials, no token needed.</summary>
    protected static Dictionary<string, string> BasicAuth() => new()
    {
        ["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Settings.AdminUser}:{Settings.AdminPassword}")),
    };
}
