using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace Booker.Tests.Support;

/// <summary>
/// Base for UI tests: a fresh browser context per test (isolated cookies and storage),
/// the base URL from settings, and a Playwright trace kept only when a test fails.
/// </summary>
public abstract class UiTestBase : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = Settings.UiUrl };

    [SetUp]
    public async Task StartTracing() =>
        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

    [TearDown]
    public async Task KeepTraceOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces", $"{TestContext.CurrentContext.Test.Name}.zip");
        await Context.Tracing.StopAsync(new() { Path = path });
        TestContext.AddTestAttachment(path, "Open with: playwright.ps1 show-trace <file>");
    }
}
