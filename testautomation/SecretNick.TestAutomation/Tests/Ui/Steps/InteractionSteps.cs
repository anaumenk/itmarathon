using Microsoft.Playwright;
using Reqnroll;
using Tests.Common.TestData;
using Tests.Helpers;

namespace Tests.Ui.Steps
{
    [Binding]
    public class InteractionSteps(ScenarioContext scenarioContext, IPage page) : UiStepsBase(page)
    {
        [When("I click {string} button")]
        public async Task WhenIClickButton(string buttonText)
        {
            await GetBasePage().ClickButtonAsync(buttonText);

            if (buttonText is "Continue" or "Complete" or "Create Your Room" or "Visit Your Room")
            {
                try
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 5000 });
                }
                catch
                {
                }
            }
            
            if (buttonText == "Remove")
            {
                try
                {
                    await page.WaitForSelectorAsync(".pl-list", new() { State = WaitForSelectorState.Detached, Timeout = 5000 });
                }
                catch
                {
                }
            }
        }

        [When("I click {string} icon")]
        public async Task WhenIClickTheIcon(string iconName)
        {
            await GetBasePage().ClickIconButtonAsync(iconName);
            
        }

        [When("I click outside any element")]
        public async Task WhenIClickOutsideAnyElement()
        {
            await GetBasePage().ClickOutsideAsync();
        }

        [When("I select {string} option")]
        public async Task WhenISelectOption(string optionText)
        {
            await GetBasePage().SelectRadioButtonAsync(optionText);
        }

        [When("I fill the following fields:")]
        public async Task WhenIFillTheFollowingFields(Table table)
        {
            foreach (var row in table.Rows)
            {
                await GetAddDetailsPage().FillFieldAsync(row["Field"], row["Value"]);
            }
        }

        [When("I fill room form with:")]
        public async Task WhenIFillRoomFormWith(Table table)
        {
            foreach (var row in table.Rows)
            {
                switch (row[0])
                {
                    case "Date" when row[1] == "today":
                    case "Gift Exchange Date" when row[1] == "today":
                        await GetCreateRoomPage().SelectDateAsync();
                        await GetCreateRoomPage().SelectTodayAsync();
                        break;
                    case "Date":
                    case "Gift Exchange Date":
                        await GetCreateRoomPage().SelectDateAsync();
                        await GetCreateRoomPage().SelectCustomDateAsync(row[1]);
                        break;
                    default:
                        await GetCreateRoomPage().FillFieldAsync(row[0], row[1]);
                        break;
                }
            }
        }

        [When("I fill user form with:")]
        public async Task WhenIFillUserFormWith(Table table)
        {
            foreach (var row in table.Rows)
            {
                await GetCreateRoomPage().FillFieldAsync(row[0], row[1]);
            }
        }

        [When("I fill room form with generated data")]
        public async Task WhenIFillRoomFormWithGeneratedData()
        {
            var room = TestDataGenerator.GenerateRoom();
            scenarioContext.Set(room.Name, "GeneratedRoomName");

            await GetCreateRoomPage().FillFieldAsync("Room Name", room.Name);
            await GetCreateRoomPage().FillFieldAsync("Message", room.Description);
            await GetCreateRoomPage().FillFieldAsync("Budget", room.GiftMaximumBudget.ToString());
            await GetCreateRoomPage().SelectDateAsync();
            await GetCreateRoomPage().SelectTodayAsync();
        }

        [When("I fill user form with generated data")]
        [When("I fill user details with generated data")]
        public async Task WhenIFillUserFormWithGeneratedData()
        {
            var user = TestDataGenerator.GenerateUser();

            await GetCreateRoomPage().FillFieldAsync("First Name", user.FirstName);
            await GetCreateRoomPage().FillFieldAsync("Last Name", user.LastName);
            await GetCreateRoomPage().FillFieldAsync("Phone", user.Phone.TruncatePhone());
            await GetCreateRoomPage().FillFieldAsync("Email", user.Email ?? "");
            await GetCreateRoomPage().FillFieldAsync("Address", user.DeliveryInfo);
        }

        [When("I select today as exchange date")]
        public async Task WhenISelectTodayAsExchangeDate()
        {
            await GetCreateRoomPage().SelectDateAsync();
            await GetCreateRoomPage().SelectTodayAsync();
        }

        [When("I add {int} wishes with names {string}")]
        public async Task WhenIAddWishesWithNames(int count, string names)
        {
            var wishNames = names.Split(',');

            for (int i = 0; i < count && i < wishNames.Length; i++)
            {
                if (i > 0)
                {
                    await GetAddWishesPage().AddWishAsync();
                }
                await GetCreateRoomPage().FillFieldAsync("Wish Name", wishNames[i].Trim());
            }
        }

        [When("I add {int} wishes with generated names")]
        public async Task WhenIAddWishesWithGeneratedNames(int count)
        {
            if (count == 0)
            {
                var interests = TestDataGenerator.GenerateUser(wantSurprise: true).Interests;
                await GetCreateRoomPage().FillFieldAsync("Interests", interests!);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    await GetAddWishesPage().AddWishAsync();
                }
                var wish = TestDataGenerator.GenerateWish();
                await GetCreateRoomPage().FillFieldAsync("Wish Name", wish.Name);
                if (!string.IsNullOrEmpty(wish.InfoLink))
                {
                    await GetCreateRoomPage().FillFieldAsync("Wish Link", wish.InfoLink);
                }
            }
        }
    }
}
