using System.Text.Json;
using Booker.Tests.Support;
using Booker.Tests.TestData;

namespace Booker.Tests.Api;

/// <summary>
/// CRUD tests for /booking. Every test creates its own booking first, so it knows the expected data
/// and never depends on records that someone else (or another test) might change.
/// </summary>
[TestFixture, Category("Api")]
public class BookingApiTests : ApiTestBase
{
    [Test]
    public async Task Created_booking_can_be_read_back_unchanged()
    {
        var booking = new BookingBuilder().WithPrice(220).WithDepositPaid(false).Build();

        var created = await CreateBookingAsync(booking);
        var response = await Api.GetAsync($"/booking/{created.BookingId}");
        var stored = await ReadJsonAsync<Booking>(response);

        Assert.Multiple(() =>
        {
            Assert.That(created.BookingId, Is.GreaterThan(0));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(stored.FirstName, Is.EqualTo(booking.FirstName));
            Assert.That(stored.LastName, Is.EqualTo(booking.LastName));
            Assert.That(stored.TotalPrice, Is.EqualTo(220));
            Assert.That(stored.DepositPaid, Is.False);
            Assert.That(stored.BookingDates.CheckIn, Is.EqualTo(booking.BookingDates.CheckIn));
            Assert.That(stored.BookingDates.CheckOut, Is.EqualTo(booking.BookingDates.CheckOut));
        });
    }

    [Test]
    public async Task Search_by_name_finds_the_new_booking()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var response = await Api.GetAsync("/booking", new()
        {
            Params = new Dictionary<string, object> { ["firstname"] = created.Booking.FirstName },
        });
        using var json = JsonDocument.Parse(await response.TextAsync());
        var ids = json.RootElement.EnumerateArray().Select(e => e.GetProperty("bookingid").GetInt32()).ToList();

        Assert.That(ids, Does.Contain(created.BookingId));
    }

    [Test]
    public async Task Update_with_a_token_changes_the_booking()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var token = await CreateTokenAsync();
        var changed = new BookingBuilder().WithFirstName(created.Booking.FirstName).WithPrice(999).WithAdditionalNeeds("Late check-out").Build();

        var response = await Api.PutAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(changed), Headers = AuthCookie(token) });
        var updated = await ReadJsonAsync<Booking>(response);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(updated.TotalPrice, Is.EqualTo(999));
            Assert.That(updated.AdditionalNeeds, Is.EqualTo("Late check-out"));
        });
    }

    [Test]
    public async Task Update_without_a_token_is_forbidden()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var response = await Api.PutAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(created.Booking) });

        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task Deleted_booking_is_gone()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var token = await CreateTokenAsync();

        var delete = await Api.DeleteAsync($"/booking/{created.BookingId}", new() { Headers = AuthCookie(token) });
        var get = await Api.GetAsync($"/booking/{created.BookingId}");

        Assert.Multiple(() =>
        {
            Assert.That(delete.Status, Is.EqualTo(201));   // Restful-Booker's quirk: 201 for a delete
            Assert.That(get.Status, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task Missing_required_fields_are_not_accepted()
    {
        var response = await Api.PostAsync("/booking", new() { DataString = """{"lastname": "OnlyLastName"}""" });

        // A real API should answer 400; Restful-Booker answers 500. Either way it must not be a 2xx.
        Assert.That(response.Ok, Is.False, $"Expected an error, got {response.Status}");
    }
}
