using System.Net;
using System.Text.Json;
using Booker.Core.Models;
using RestSharp;

namespace Booker.Core;

/// <summary>Thrown when Restful-Booker answers with a status the client didn't expect.</summary>
public sealed class BookerApiException(string message, HttpStatusCode statusCode, string? body)
    : Exception($"{message} (HTTP {(int)statusCode}): {body}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>
/// A small typed client for Restful-Booker, built on RestSharp.
/// It's the kind of wrapper a framework gives its tests: one place for URLs, headers, auth and error handling.
/// The component tests check it against a fake HTTP handler; the API tests check the real service.
/// </summary>
public sealed class BookerApiClient : IDisposable
{
    private readonly RestClient _client;
    private readonly string _host;

    public BookerApiClient(string baseUrl, HttpMessageHandler? handler = null)
    {
        var options = new RestClientOptions(baseUrl);
        if (handler is not null) options.ConfigureMessageHandler = _ => handler;   // component tests swap the network for a fake
        _client = new RestClient(options);
        _host = new Uri(baseUrl).Host;
    }

    public async Task<string> CreateTokenAsync(string username, string password)
    {
        var request = Json("auth", Method.Post).AddJsonBody(new { username, password });
        var response = await _client.ExecuteAsync(request);
        EnsureStatus(response, HttpStatusCode.OK, "Authentication failed");

        // Restful-Booker answers bad credentials with 200 OK and {"reason":"Bad credentials"}, not with a 401.
        using var json = JsonDocument.Parse(response.Content ?? "{}");
        if (json.RootElement.TryGetProperty("token", out var token)) return token.GetString()!;
        var reason = json.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : "no token in response";
        throw new BookerApiException($"Authentication failed: {reason}", response.StatusCode, response.Content);
    }

    public async Task<CreatedBooking> CreateBookingAsync(Booking booking)
    {
        var response = await _client.ExecuteAsync<CreatedBooking>(Json("booking", Method.Post).AddJsonBody(booking));
        EnsureStatus(response, HttpStatusCode.OK, "Create booking failed");
        return response.Data!;
    }

    /// <summary>Returns null when the booking doesn't exist (404).</summary>
    public async Task<Booking?> GetBookingAsync(int id)
    {
        var response = await _client.ExecuteAsync<Booking>(Json($"booking/{id}", Method.Get));
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        EnsureStatus(response, HttpStatusCode.OK, "Get booking failed");
        return response.Data;
    }

    public async Task<Booking> UpdateBookingAsync(int id, Booking booking, string token)
    {
        var request = Json($"booking/{id}", Method.Put).AddJsonBody(booking).AddCookie("token", token, "/", _host);
        var response = await _client.ExecuteAsync<Booking>(request);
        EnsureStatus(response, HttpStatusCode.OK, "Update booking failed");
        return response.Data!;
    }

    /// <summary>Partial update: only the fields in <paramref name="changes"/> are sent and changed (PATCH).</summary>
    public async Task<Booking> PatchBookingAsync(int id, object changes, string token)
    {
        var request = Json($"booking/{id}", Method.Patch).AddJsonBody(changes).AddCookie("token", token, "/", _host);
        var response = await _client.ExecuteAsync<Booking>(request);
        EnsureStatus(response, HttpStatusCode.OK, "Patch booking failed");
        return response.Data!;
    }

    public async Task DeleteBookingAsync(int id, string token)
    {
        var request = Json($"booking/{id}", Method.Delete).AddCookie("token", token, "/", _host);
        var response = await _client.ExecuteAsync(request);
        EnsureStatus(response, HttpStatusCode.Created, "Delete booking failed");   // yes, Restful-Booker returns 201 for a delete
    }

    public void Dispose() => _client.Dispose();

    // Restful-Booker returns 418 (I'm a teapot) when Accept isn't JSON or XML, so every request sets it.
    private static RestRequest Json(string resource, Method method) =>
        new RestRequest(resource, method).AddHeader("Accept", "application/json");

    private static void EnsureStatus(RestResponse response, HttpStatusCode expected, string message)
    {
        if (response.ResponseStatus == ResponseStatus.Error && response.StatusCode == 0)
            throw new BookerApiException($"{message}: no response ({response.ErrorMessage})", 0, null);
        if (response.StatusCode != expected)
            throw new BookerApiException(message, response.StatusCode, response.Content);
    }
}
