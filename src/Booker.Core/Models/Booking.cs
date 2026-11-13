using System.Text.Json.Serialization;

namespace Booker.Core.Models;

/// <summary>A booking as Restful-Booker sends and receives it (all-lowercase JSON names).</summary>
public sealed class Booking
{
    [JsonPropertyName("firstname")] public string FirstName { get; init; } = "";
    [JsonPropertyName("lastname")] public string LastName { get; init; } = "";
    [JsonPropertyName("totalprice")] public int TotalPrice { get; init; }
    [JsonPropertyName("depositpaid")] public bool DepositPaid { get; init; }
    [JsonPropertyName("bookingdates")] public BookingDates BookingDates { get; init; } = new();
    [JsonPropertyName("additionalneeds")] public string? AdditionalNeeds { get; init; }
}

public sealed class BookingDates
{
    [JsonPropertyName("checkin")] public DateOnly CheckIn { get; init; }
    [JsonPropertyName("checkout")] public DateOnly CheckOut { get; init; }
}

/// <summary>Response of POST /booking: the new id plus the stored booking.</summary>
public sealed class CreatedBooking
{
    [JsonPropertyName("bookingid")] public int BookingId { get; init; }
    [JsonPropertyName("booking")] public Booking Booking { get; init; } = new();
}
