using Booker.Core.Models;

namespace Booker.Core;

/// <summary>
/// Business rules for a booking, kept as pure code so they can be unit-tested without a browser or a server.
/// </summary>
public static class BookingValidator
{
    public static IReadOnlyList<string> Validate(Booking booking)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(booking.FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(booking.LastName)) errors.Add("Last name is required.");
        if (booking.TotalPrice < 0) errors.Add("Total price can't be negative.");
        if (booking.BookingDates.CheckOut <= booking.BookingDates.CheckIn) errors.Add("Check-out must be after check-in.");
        return errors;
    }

    public static bool IsValid(Booking booking) => Validate(booking).Count == 0;

    /// <summary>Number of nights between check-in and check-out.</summary>
    public static int Nights(BookingDates dates) => dates.CheckOut.DayNumber - dates.CheckIn.DayNumber;
}
