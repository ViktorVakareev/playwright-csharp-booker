namespace Booker.Tests.TestData;

/// <summary>
/// Builder pattern for test data: sensible, valid defaults, so each test sets only what it's about.
///   var booking = new BookingBuilder().WithPrice(0).Build();
/// The first name is unique per build, so tests never collide on shared data.
/// </summary>
public sealed class BookingBuilder
{
    private string _firstName = $"Test{Guid.NewGuid():N}"[..12];
    private string _lastName = "Booker";
    private int _price = 150;
    private bool _depositPaid = true;
    private DateOnly _checkIn = DateOnly.FromDateTime(DateTime.Today).AddDays(30);
    private int _nights = 3;
    private string? _needs = "Breakfast";

    public BookingBuilder WithFirstName(string value) { _firstName = value; return this; }
    public BookingBuilder WithLastName(string value) { _lastName = value; return this; }
    public BookingBuilder WithPrice(int value) { _price = value; return this; }
    public BookingBuilder WithDepositPaid(bool value) { _depositPaid = value; return this; }
    public BookingBuilder WithCheckIn(DateOnly value) { _checkIn = value; return this; }
    public BookingBuilder WithNights(int value) { _nights = value; return this; }
    public BookingBuilder WithAdditionalNeeds(string? value) { _needs = value; return this; }

    public Booking Build() => new()
    {
        FirstName = _firstName,
        LastName = _lastName,
        TotalPrice = _price,
        DepositPaid = _depositPaid,
        BookingDates = new BookingDates { CheckIn = _checkIn, CheckOut = _checkIn.AddDays(_nights) },
        AdditionalNeeds = _needs,
    };
}
