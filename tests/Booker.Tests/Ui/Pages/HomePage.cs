namespace Booker.Tests.Ui.Pages;

/// <summary>
/// Page object for the Restful Booker Platform home page (automationintesting.online), contact section.
/// It knows *how* to find things; tests say *what* the user does. Locators are lazy, so defining them here is safe.
/// </summary>
public sealed class HomePage(IPage page)
{
    public ILocator Heading => page.GetByRole(AriaRole.Heading, new() { Level = 1 });

    // The form has data-testid hooks, which are stable even if labels or layout change.
    public ILocator Name => page.GetByTestId("ContactName");
    public ILocator Email => page.GetByTestId("ContactEmail");
    public ILocator Phone => page.GetByTestId("ContactPhone");
    public ILocator Subject => page.GetByTestId("ContactSubject");
    public ILocator Message => page.GetByTestId("ContactDescription");
    public ILocator SubmitButton => page.GetByRole(AriaRole.Button, new() { Name = "Submit" });

    public ILocator SuccessHeading => page.GetByRole(AriaRole.Heading, new() { Name = "Thanks for getting in touch" });

    public async Task GotoAsync() => await page.GotoAsync("/");

    public async Task SendMessageAsync(ContactMessage message)
    {
        await Name.FillAsync(message.Name);
        await Email.FillAsync(message.Email);
        await Phone.FillAsync(message.Phone);
        await Subject.FillAsync(message.Subject);
        await Message.FillAsync(message.Body);
        await SubmitButton.ClickAsync();
    }
}

/// <summary>Valid by the site's rules: phone 11–21 characters, subject 5–100, message 20–2000.</summary>
public sealed record ContactMessage(string Name, string Email, string Phone, string Subject, string Body)
{
    public static ContactMessage Valid() => new(
        Name: $"Test Visitor {Guid.NewGuid():N}"[..19],
        Email: "visitor@example.com",
        Phone: "01234567890",
        Subject: "Room availability",
        Body: "Hello, is the double room free next month? Thank you.");
}
