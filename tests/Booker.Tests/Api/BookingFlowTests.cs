using Booker.Tests.Support;
using Booker.Tests.TestData;

namespace Booker.Tests.Api;

/// <summary>
/// Business flows: what a guest or a front-desk user actually does with bookings,
/// beyond single CRUD calls: partial changes, both auth methods, searching, and multi-step journeys.
/// </summary>
[TestFixture, Category("Api"), Category("Flow")]
public class BookingFlowTests : ApiTestBase
{
    // ---------- Changing a booking ----------

    [Test]
    public async Task Partial_update_changes_only_the_fields_sent()
    {
        var created = await CreateBookingAsync(new BookingBuilder().WithPrice(150).WithAdditionalNeeds("Breakfast").Build());
        var token = await CreateTokenAsync();

        var response = await Api.PatchAsync($"/booking/{created.BookingId}", new()
        {
            DataString = ToJson(new { additionalneeds = "Late check-out" }),
            Headers = AuthCookie(token),
        });
        var updated = await ReadJsonAsync<Booking>(response);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(updated.AdditionalNeeds, Is.EqualTo("Late check-out"));                 // changed
            Assert.That(updated.FirstName, Is.EqualTo(created.Booking.FirstName));              // untouched
            Assert.That(updated.TotalPrice, Is.EqualTo(150));
            Assert.That(updated.BookingDates.CheckIn, Is.EqualTo(created.Booking.BookingDates.CheckIn));
        });
    }

    [TestCase("cookie")]
    [TestCase("basic")]
    public async Task Update_works_with_either_auth_method(string authMethod)
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var changed = new BookingBuilder().WithFirstName(created.Booking.FirstName).WithPrice(480).Build();

        var response = await Api.PutAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(changed), Headers = await AuthAsync(authMethod) });
        Assert.That(response.Status, Is.EqualTo(200), await response.TextAsync());

        var updated = await ReadJsonAsync<Booking>(response);
        Assert.That(updated.TotalPrice, Is.EqualTo(480));
    }

    [TestCase("cookie")]
    [TestCase("basic")]
    public async Task Delete_works_with_either_auth_method(string authMethod)
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var delete = await Api.DeleteAsync($"/booking/{created.BookingId}", new() { Headers = await AuthAsync(authMethod) });
        var get = await Api.GetAsync($"/booking/{created.BookingId}");

        Assert.Multiple(() =>
        {
            Assert.That(delete.Status, Is.EqualTo(201));
            Assert.That(get.Status, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task One_token_can_be_used_for_several_changes()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());
        var token = await CreateTokenAsync();

        var statuses = new List<int>();
        foreach (var needs in new[] { "Cot", "Parking", "Airport pickup" })
        {
            var response = await Api.PatchAsync($"/booking/{created.BookingId}", new() { DataString = ToJson(new { additionalneeds = needs }), Headers = AuthCookie(token) });
            statuses.Add(response.Status);
        }
        var final = await ReadJsonAsync<Booking>(await Api.GetAsync($"/booking/{created.BookingId}"));

        Assert.Multiple(() =>
        {
            Assert.That(statuses, Is.All.EqualTo(200));
            Assert.That(final.AdditionalNeeds, Is.EqualTo("Airport pickup"));   // last change wins
        });
    }

    // ---------- Journeys ----------

    [Test]
    public async Task Guest_extends_their_stay_and_then_cancels()
    {
        // 1. Book 3 nights.
        var created = await CreateBookingAsync(new BookingBuilder().WithNights(3).WithPrice(300).Build());
        var token = await CreateTokenAsync();
        var checkIn = created.Booking.BookingDates.CheckIn;

        // 2. Extend to 5 nights: only the dates change.
        var extend = await Api.PatchAsync($"/booking/{created.BookingId}", new()
        {
            DataString = ToJson(new { bookingdates = new BookingDates { CheckIn = checkIn, CheckOut = checkIn.AddDays(5) } }),
            Headers = AuthCookie(token),
        });
        var extended = await ReadJsonAsync<Booking>(await Api.GetAsync($"/booking/{created.BookingId}"));

        // 3. Cancel.
        var cancel = await Api.DeleteAsync($"/booking/{created.BookingId}", new() { Headers = AuthCookie(token) });
        var afterCancel = await Api.GetAsync($"/booking/{created.BookingId}");

        Assert.Multiple(() =>
        {
            Assert.That(extend.Status, Is.EqualTo(200));
            Assert.That(BookingValidator.Nights(extended.BookingDates), Is.EqualTo(5));
            Assert.That(extended.TotalPrice, Is.EqualTo(300), "price must not change when only the dates do");
            Assert.That(cancel.Status, Is.EqualTo(201));
            Assert.That(afterCancel.Status, Is.EqualTo(404));
        });
    }

    // ---------- Searching ----------

    [Test]
    public async Task Search_by_first_and_last_name_together()
    {
        var created = await CreateBookingAsync(new BookingBuilder().WithLastName("Ivanova").Build());

        var match = await SearchAsync(new() { ["firstname"] = created.Booking.FirstName, ["lastname"] = "Ivanova" });
        var wrongLastName = await SearchAsync(new() { ["firstname"] = created.Booking.FirstName, ["lastname"] = "Petrova" });

        Assert.Multiple(() =>
        {
            Assert.That(match, Does.Contain(created.BookingId));
            Assert.That(wrongLastName, Does.Not.Contain(created.BookingId));
        });
    }

    [Test]
    public async Task Search_for_an_unknown_name_returns_an_empty_list()
    {
        var ids = await SearchAsync(new() { ["firstname"] = $"Nobody{Guid.NewGuid():N}" });

        Assert.That(ids, Is.Empty);
    }

    [Test]
    public async Task Two_guests_with_the_same_name_are_both_found()
    {
        var name = $"Twin{Guid.NewGuid():N}"[..12];
        var first = await CreateBookingAsync(new BookingBuilder().WithFirstName(name).Build());
        var second = await CreateBookingAsync(new BookingBuilder().WithFirstName(name).WithCheckIn(first.Booking.BookingDates.CheckOut).Build());

        var ids = await SearchAsync(new() { ["firstname"] = name });

        Assert.That(ids, Is.EquivalentTo(new[] { first.BookingId, second.BookingId }));
    }

    [Test]
    public async Task Search_by_dates_finds_a_booking_inside_the_range()
    {
        var created = await CreateBookingAsync(new BookingBuilder().WithCheckIn(new DateOnly(2031, 3, 10)).WithNights(5).Build());

        var ids = await SearchAsync(new() { ["checkin"] = "2031-03-09", ["checkout"] = "2031-03-16" });

        Assert.That(ids, Does.Contain(created.BookingId));
    }

    [Test]
    public async Task Date_filters_are_inconsistent_at_the_boundaries()
    {
        var created = await CreateBookingAsync(new BookingBuilder().WithCheckIn(new DateOnly(2031, 4, 10)).WithNights(5).Build());

        var checkInSameDay = await SearchAsync(new() { ["checkin"] = "2031-04-10" });
        var checkOutSameDay = await SearchAsync(new() { ["checkout"] = "2031-04-15" });

        // Quirk worth raising: "checkin" is exclusive (a booking starting ON that day is NOT found),
        // while "checkout" is inclusive (a booking ending ON that day IS found).
        Assert.Multiple(() =>
        {
            Assert.That(checkInSameDay, Does.Not.Contain(created.BookingId), "checkin filter became inclusive: update this test");
            Assert.That(checkOutSameDay, Does.Contain(created.BookingId));
        });
    }

    private async Task<Dictionary<string, string>> AuthAsync(string method) =>
        method == "basic" ? BasicAuth() : AuthCookie(await CreateTokenAsync());
}
