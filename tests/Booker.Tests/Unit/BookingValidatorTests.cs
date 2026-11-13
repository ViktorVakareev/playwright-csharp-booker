using Booker.Tests.TestData;

namespace Booker.Tests.Unit;

/// <summary>
/// Unit tests: pure business rules, no browser, no network, milliseconds per test.
/// The bottom of the test pyramid: most rules should be proven here.
/// </summary>
[TestFixture, Category("Unit")]
public class BookingValidatorTests
{
    [Test]
    public void A_complete_booking_is_valid()
    {
        var booking = new BookingBuilder().Build();

        Assert.That(BookingValidator.Validate(booking), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void A_missing_first_name_is_rejected(string firstName)
    {
        var booking = new BookingBuilder().WithFirstName(firstName).Build();

        Assert.That(BookingValidator.Validate(booking), Does.Contain("First name is required."));
    }

    [TestCase(0, true)]      // boundary: free is allowed
    [TestCase(-1, false)]    // just below the boundary
    [TestCase(150, true)]
    public void Price_must_not_be_negative(int price, bool expectedValid)
    {
        var booking = new BookingBuilder().WithPrice(price).Build();

        Assert.That(BookingValidator.IsValid(booking), Is.EqualTo(expectedValid));
    }

    [TestCase(1, true)]      // one night: the smallest valid stay
    [TestCase(0, false)]     // same-day check-out
    [TestCase(-2, false)]    // check-out before check-in
    public void Check_out_must_be_after_check_in(int nights, bool expectedValid)
    {
        var booking = new BookingBuilder().WithNights(nights).Build();

        Assert.That(BookingValidator.IsValid(booking), Is.EqualTo(expectedValid));
    }

    [Test]
    public void Nights_are_the_days_between_check_in_and_check_out()
    {
        var dates = new BookingDates { CheckIn = new DateOnly(2026, 12, 30), CheckOut = new DateOnly(2027, 1, 2) };

        Assert.That(BookingValidator.Nights(dates), Is.EqualTo(3));   // crosses a year boundary on purpose
    }

    [Test]
    public void Every_problem_is_reported_at_once()
    {
        var booking = new BookingBuilder().WithFirstName("").WithLastName("").WithPrice(-5).WithNights(0).Build();

        Assert.That(BookingValidator.Validate(booking), Has.Count.EqualTo(4));
    }
}
