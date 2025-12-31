using ErsatzTV.Application.MediaCards;
using ErsatzTV.Pages;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Tests.Pages;

[TestFixture]
public class MultiSelectBaseTests
{
    [Test]
    public void Should_replace_existing_selection_and_return_last_card()
    {
        var existingCard = new MediaCardViewModel(1, "Existing", "Sub", "Existing", "", Core.Domain.MediaItemState.Normal, false);
        var selected = new HashSet<MediaCardViewModel> { existingCard };

        var first = new MediaCardViewModel(2, "First", "Sub", "First", "", Core.Domain.MediaItemState.Normal, false);
        var second = new MediaCardViewModel(3, "Second", "Sub", "Second", "", Core.Domain.MediaItemState.Normal, false);

        MultiSelectBase<Search>.ResetSelectionWithCards(selected, [first, second]);

        selected.ShouldBe([first, second], ignoreOrder: true);
    }

    [Test]
    public void Should_clear_selection_when_no_cards()
    {
        var existingCard = new MediaCardViewModel(1, "Existing", "Sub", "Existing", "", Core.Domain.MediaItemState.Normal, false);
        var selected = new HashSet<MediaCardViewModel> { existingCard };

        MultiSelectBase<Search>.ResetSelectionWithCards(selected, []);

        selected.ShouldBeEmpty();
    }
}
