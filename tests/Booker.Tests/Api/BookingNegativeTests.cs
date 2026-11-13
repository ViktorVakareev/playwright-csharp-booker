using System.Text.Json;
using Booker.Tests.Support;
using Booker.Tests.TestData;

namespace Booker.Tests.Api;

/// <summary>
/// Negative API tests: wrong auth, wrong ids, wrong bodies, wrong headers.
/// They assert what Restful-Booker actually does today, and the comments say where that differs from a textbook REST API.
/// Each "rejected" test also checks the booking was not changed, because a correct status with a changed record is still a bug.
/// </summary>
[TestFixture, Category("Api"), Category("Negative")]
public class BookingNegativeTests : ApiTestBase
{
    // ---------- Auth ----------

    [TestCase("bogus-token")]
    [TestCase("")]
    public async Task Update_with_an_invalid_token_is_forbidden(string token)
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var changed = new BookingBuilder().WithPrice(1).Build();

        var response = await Api.PutAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(changed), Headers = AuthCookie(token) });

        Assert.That(response.Status, Is.EqualTo(403));
        await AssertUnchangedAsync(created);
    }

    [Test]
    public async Task Partial_update_without_a_token_is_forbidden()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var response = await Api.PatchAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(new { firstname = "Hacked" }) });

        Assert.That(response.Status, Is.EqualTo(403));
        await AssertUnchangedAsync(created);
    }

    [Test]
    public async Task Delete_without_a_token_is_forbidden_and_the_booking_survives()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var delete = await Api.DeleteAsync($"/booking/{created.BookingId}");
        var get = await Api.GetAsync($"/booking/{created.BookingId}");

        Assert.Multiple(() =>
        {
            Assert.That(delete.Status, Is.EqualTo(403));
            Assert.That(get.Status, Is.EqualTo(200));
        });
    }

    [TestCase("admin", "")]
    [TestCase("", "password123")]
    [TestCase("admin", "PASSWORD123")]        // passwords are case-sensitive
    [TestCase("nobody", "password123")]
    public async Task Wrong_credentials_never_return_a_token(string username, string password)
    {
        var response = await Api.PostAsync("/auth", new() { DataString = ToJson(new { username, password }) });

        await AssertBadCredentialsAsync(response);
    }

    [Test]
    public async Task Auth_with_an_empty_body_never_returns_a_token()
    {
        var response = await Api.PostAsync("/auth", new() { DataString = "{}" });

        await AssertBadCredentialsAsync(response);
    }

    // ---------- Ids ----------

    [TestCase("99999999")]   // a number that was never issued
    [TestCase("abc")]        // not a number at all
    public async Task Reading_an_unknown_booking_returns_404(string id)
    {
        var response = await Api.GetAsync($"/booking/{id}");

        Assert.That(response.Status, Is.EqualTo(404));
    }

    [TestCase("PUT")]
    [TestCase("DELETE")]
    public async Task Changing_an_unknown_booking_returns_405(string method)
    {
        var response = await Api.FetchAsync("/booking/99999999", new()
        {
            Method = method,
            Headers = BasicAuth(),
            DataString = method == "PUT" ? ToJson(new BookingBuilder().Build()) : null,
        });

        // Quirk: a textbook API answers 404 here. Restful-Booker answers 405 Method Not Allowed.
        Assert.That(response.Status, Is.EqualTo(405));
    }

    // ---------- Bodies ----------

    [Test]
    public async Task Full_update_with_only_some_fields_is_rejected()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var token = await CreateTokenAsync();

        // PUT replaces the whole booking, so a partial body is invalid (PATCH is the partial update).
        var response = await Api.PutAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(new { firstname = "OnlyThis" }), Headers = AuthCookie(token) });

        Assert.That(response.Status, Is.EqualTo(400));
        await AssertUnchangedAsync(created);
    }

    [Test]
    public async Task Malformed_json_is_rejected_with_400()
    {
        var response = await PostRawBookingAsync("{ this is not json");

        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task An_empty_body_is_not_accepted()
    {
        var response = await PostRawBookingAsync("");

        // Restful-Booker answers 500 here, which is a defect (see KnownDefectTests). This test only guards "not accepted".
        Assert.That(response.Ok, Is.False, $"Expected an error, got {response.Status}");
    }

    // ---------- Headers ----------

    [TestCase("application/json", 200)]
    [TestCase("application/xml", 200)]
    [TestCase("text/plain", 418)]     // I'm a teapot: any type other than JSON or XML
    [TestCase("text/html", 418)]
    public async Task The_Accept_header_decides_whether_the_API_answers(string accept, int expectedStatus)
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var response = await Api.GetAsync($"/booking/{created.BookingId}", new() { Headers = new Dictionary<string, string> { ["Accept"] = accept } });

        Assert.That(response.Status, Is.EqualTo(expectedStatus));
    }

    // ---------- Helpers ----------

    private async Task AssertUnchangedAsync(CreatedBooking created)
    {
        var stored = await ReadJsonAsync<Booking>(await Api.GetAsync($"/booking/{created.BookingId}"));
        Assert.Multiple(() =>
        {
            Assert.That(stored.FirstName, Is.EqualTo(created.Booking.FirstName), "first name changed");
            Assert.That(stored.TotalPrice, Is.EqualTo(created.Booking.TotalPrice), "price changed");
        });
    }

    private static async Task AssertBadCredentialsAsync(IAPIResponse response)
    {
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));   // the API says "no" with 200 OK, so the body is what matters
            Assert.That(json.RootElement.GetProperty("reason").GetString(), Is.EqualTo("Bad credentials"));
            Assert.That(json.RootElement.TryGetProperty("token", out _), Is.False);
        });
    }
}
