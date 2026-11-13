using System.Net;
using System.Text.Json;
using Booker.Tests.TestData;

namespace Booker.Tests.Component;

/// <summary>
/// Component tests: one component (our RestSharp-based BookerApiClient) tested in isolation.
/// Its dependency, the network, is replaced by a stub, so we check *our* code:
/// the request it builds and how it handles responses. Fast and offline, unlike the API tests.
/// </summary>
[TestFixture, Category("Component")]
public class BookerApiClientTests
{
    private const string BaseUrl = "http://booker.test";

    [Test]
    public async Task Create_sends_a_JSON_POST_with_the_API_field_names_and_returns_the_new_id()
    {
        var stub = new StubHttpHandler(HttpStatusCode.OK, """{"bookingid": 42, "booking": {"firstname": "Ann"}}""");
        using var client = new BookerApiClient(BaseUrl, stub);
        var booking = new BookingBuilder().WithFirstName("Ann").WithPrice(99).Build();

        var created = await client.CreateBookingAsync(booking);

        var sent = stub.Requests.Single();
        using var body = JsonDocument.Parse(sent.Body!);
        Assert.Multiple(() =>
        {
            Assert.That(created.BookingId, Is.EqualTo(42));
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(sent.Uri.AbsolutePath, Is.EqualTo("/booking"));
            Assert.That(sent.Headers["Accept"], Does.Contain("application/json"));
            Assert.That(body.RootElement.GetProperty("firstname").GetString(), Is.EqualTo("Ann"));      // lowercase, as the API expects
            Assert.That(body.RootElement.GetProperty("totalprice").GetInt32(), Is.EqualTo(99));
            Assert.That(body.RootElement.GetProperty("bookingdates").GetProperty("checkin").GetString(),
                Is.EqualTo(booking.BookingDates.CheckIn.ToString("yyyy-MM-dd")));
        });
    }

    [Test]
    public async Task Update_sends_the_auth_token_as_a_cookie()
    {
        var stub = new StubHttpHandler(HttpStatusCode.OK, """{"firstname": "Ann", "lastname": "Booker"}""");
        using var client = new BookerApiClient(BaseUrl, stub);

        await client.UpdateBookingAsync(7, new BookingBuilder().Build(), "abc123");

        var sent = stub.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Put));
            Assert.That(sent.Uri.AbsolutePath, Is.EqualTo("/booking/7"));
            Assert.That(sent.Headers["Cookie"], Does.Contain("token=abc123"));
        });
    }

    [Test]
    public async Task Get_returns_null_for_a_booking_that_does_not_exist()
    {
        var stub = new StubHttpHandler(HttpStatusCode.NotFound, "Not Found");
        using var client = new BookerApiClient(BaseUrl, stub);

        var booking = await client.GetBookingAsync(999);

        Assert.That(booking, Is.Null);
    }

    [Test]
    public void Bad_credentials_become_a_clear_exception_even_though_the_API_returns_200()
    {
        var stub = new StubHttpHandler(HttpStatusCode.OK, """{"reason": "Bad credentials"}""");
        using var client = new BookerApiClient(BaseUrl, stub);

        var error = Assert.ThrowsAsync<BookerApiException>(() => client.CreateTokenAsync("admin", "wrong"));

        Assert.That(error!.Message, Does.Contain("Bad credentials"));
    }

    [Test]
    public void A_server_error_is_reported_with_its_status_code()
    {
        var stub = new StubHttpHandler(HttpStatusCode.InternalServerError, "boom");
        using var client = new BookerApiClient(BaseUrl, stub);

        var error = Assert.ThrowsAsync<BookerApiException>(() => client.CreateBookingAsync(new BookingBuilder().Build()));

        Assert.That(error!.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }
}
