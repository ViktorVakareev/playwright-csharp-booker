using Booker.Tests.Support;
using Booker.Tests.Ui.Pages;

namespace Booker.Tests.Ui;

/// <summary>
/// UI test: the top of the pyramid, one critical user journey end to end in a real browser.
/// Business rules are covered more cheaply in the unit and API tests.
/// </summary>
[TestFixture, Category("Ui")]
public class ContactFormTests : UiTestBase
{
    [Test]
    public async Task Visitor_can_send_a_message_and_sees_a_confirmation()
    {
        var home = new HomePage(Page);
        var message = ContactMessage.Valid();

        await home.GotoAsync();
        await Expect(home.Heading).ToContainTextAsync("Shady Meadows");
        await home.SendMessageAsync(message);

        // Web-first assertions wait for the confirmation, so no sleeps are needed.
        await Expect(home.SuccessHeading).ToBeVisibleAsync();
        await Expect(home.SuccessHeading).ToContainTextAsync(message.Name);
        await Expect(home.SubmitButton).Not.ToBeVisibleAsync();
    }
}
