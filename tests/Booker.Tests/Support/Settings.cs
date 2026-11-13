namespace Booker.Tests.Support;

/// <summary>
/// Where the tests point. Order of precedence: environment variable → booker.runsettings → default.
///   $env:BOOKER_URL="https://restful-booker.herokuapp.com"; dotnet test
/// </summary>
public static class Settings
{
    public static string BookerUrl => Get("BOOKER_URL", "BookerUrl", "http://localhost:3001");
    public static string UiUrl => Get("UI_URL", "UiUrl", "https://automationintesting.online");

    // Demo credentials published by Restful-Booker itself; override for other environments.
    public static string AdminUser => Get("BOOKER_USER", "BookerUser", "admin");
    public static string AdminPassword => Get("BOOKER_PASSWORD", "BookerPassword", "password123");

    private static string Get(string envName, string runSettingsName, string fallback) =>
        Environment.GetEnvironmentVariable(envName) is { Length: > 0 } fromEnv
            ? fromEnv
            : TestContext.Parameters.Get(runSettingsName, fallback);
}
