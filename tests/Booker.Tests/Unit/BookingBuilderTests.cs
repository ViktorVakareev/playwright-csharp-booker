using Booker.Tests.TestData;

namespace Booker.Tests.Unit;

/// <summary>Test data helpers are code too: if the builder is wrong, every test that uses it is wrong.</summary>
[TestFixture, Category("Unit")]
public class BookingBuilderTests
{
    [Test]
    public void Defaults_produce_a_valid_booking() =>
        Assert.That(BookingValidator.IsValid(new BookingBuilder().Build()), Is.True);

    [Test]
    public void Each_build_gets_a_unique_first_name()
    {
        var first = new BookingBuilder().Build();
        var second = new BookingBuilder().Build();

        Assert.That(first.FirstName, Is.Not.EqualTo(second.FirstName));
    }

    [Test]
    public void Check_out_follows_from_check_in_and_nights()
    {
        var booking = new BookingBuilder().WithCheckIn(new DateOnly(2026, 6, 1)).WithNights(2).Build();

        Assert.Multiple(() =>
        {
            Assert.That(booking.BookingDates.CheckIn, Is.EqualTo(new DateOnly(2026, 6, 1)));
            Assert.That(booking.BookingDates.CheckOut, Is.EqualTo(new DateOnly(2026, 6, 3)));
        });
    }
}
