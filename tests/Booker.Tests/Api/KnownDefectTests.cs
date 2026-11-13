using Booker.Tests.Support;
using Booker.Tests.TestData;

namespace Booker.Tests.Api;

/// <summary>
/// Known defects: each test states what the API *should* do, so today they fail.
/// They are [Explicit]: skipped in a normal run (the build stays green), but run on demand:
///   dotnet test --filter "TestCategory=KnownDefect"
/// When a defect is fixed, its test turns green; then remove [Explicit] from it and it becomes a regular regression test.
/// Note that our own BookingValidator (unit tests) already rejects the first three cases: the rules exist, the API doesn't apply them.
/// </summary>
[TestFixture, Category("Api"), Category("KnownDefect")]
[Explicit("Known Restful-Booker defects: these fail until the API is fixed. Run with --filter TestCategory=KnownDefect")]
public class KnownDefectTests : ApiTestBase
{
    [Test]
    public async Task Check_out_before_check_in_should_be_rejected()
    {
        var booking = new BookingBuilder().WithNights(-3).Build();

        var response = await PostRawBookingAsync(ToJson(booking));

        Assert.That(response.Status, Is.EqualTo(400), "API accepted a stay that ends before it starts");
    }

    [Test]
    public async Task A_negative_price_should_be_rejected()
    {
        var booking = new BookingBuilder().WithPrice(-50).Build();

        var response = await PostRawBookingAsync(ToJson(booking));

        Assert.That(response.Status, Is.EqualTo(400), "API accepted a negative price");
    }

    [Test]
    public async Task An_invalid_date_should_be_rejected()
    {
        var response = await PostRawBookingAsync(BookingJson(checkin: "\"not-a-date\""));

        // Today: 200, and the date is stored as "0NaN-aN-aN".
        Assert.That(response.Status, Is.EqualTo(400), await response.TextAsync());
    }

    [Test]
    public async Task A_non_numeric_price_should_be_rejected()
    {
        var response = await PostRawBookingAsync(BookingJson(totalprice: "\"abc\""));

        // Today: 200, and the price is stored as null.
        Assert.That(response.Status, Is.EqualTo(400), await response.TextAsync());
    }

    [Test]
    public async Task An_empty_body_should_return_400_not_500()
    {
        var response = await PostRawBookingAsync("");

        // A 500 means the server crashed on client input: the client did something wrong, so it should be a 4xx.
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Changing_a_missing_booking_should_return_404_not_405()
    {
        var response = await Api.DeleteAsync("/booking/99999999", new() { Headers = BasicAuth() });

        Assert.That(response.Status, Is.EqualTo(404));
    }

    /// <summary>A valid booking body as raw JSON, with one field replaced by any JSON value (to send wrong types).</summary>
    private static string BookingJson(string totalprice = "100", string checkin = "\"2030-01-01\"") => $$$"""
        {"firstname": "Defect", "lastname": "Test", "totalprice": {{{totalprice}}}, "depositpaid": true,
         "bookingdates": {"checkin": {{{checkin}}}, "checkout": "2030-01-05"}}
        """;
}
