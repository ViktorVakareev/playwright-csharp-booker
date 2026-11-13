using Booker.Tests.Support;
using Booker.Tests.TestData;

namespace Booker.Tests.Api;

/// <summary>
/// The same RestSharp client the component tests check with a stub, now against the real service:
/// an end-to-end lifecycle through our own client code.
/// </summary>
[TestFixture, Category("Api")]
public class BookerApiClientIntegrationTests
{
    [Test]
    public async Task Booking_lifecycle_through_the_RestSharp_client()
    {
        using var client = new BookerApiClient(Settings.BookerUrl);
        var token = await client.CreateTokenAsync(Settings.AdminUser, Settings.AdminPassword);

        var created = await client.CreateBookingAsync(new BookingBuilder().Build());
        var updated = await client.UpdateBookingAsync(created.BookingId, new BookingBuilder().WithPrice(300).Build(), token);
        await client.DeleteBookingAsync(created.BookingId, token);
        var afterDelete = await client.GetBookingAsync(created.BookingId);

        Assert.Multiple(() =>
        {
            Assert.That(updated.TotalPrice, Is.EqualTo(300));
            Assert.That(afterDelete, Is.Null);
        });
    }

    [Test]
    public async Task Partial_update_through_the_RestSharp_client()
    {
        using var client = new BookerApiClient(Settings.BookerUrl);
        var token = await client.CreateTokenAsync(Settings.AdminUser, Settings.AdminPassword);
        var created = await client.CreateBookingAsync(new BookingBuilder().WithPrice(150).Build());

        try
        {
            var patched = await client.PatchBookingAsync(created.BookingId, new { additionalneeds = "Cot" }, token);

            Assert.Multiple(() =>
            {
                Assert.That(patched.AdditionalNeeds, Is.EqualTo("Cot"));
                Assert.That(patched.TotalPrice, Is.EqualTo(150));
            });
        }
        finally
        {
            await client.DeleteBookingAsync(created.BookingId, token);
        }
    }
}
