using ErsatzTV.Application.MediaCards;
using ErsatzTV.Pages;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace ErsatzTV.Core.Tests.Pages;

[TestFixture]
public class MediaCardSelectionHelperTests
{
    [Test]
    public void Should_replace_existing_selection_and_return_last_card()
    {
        var existingCard = new MediaCardViewModel(1, "Existing", "Sub", "Existing", "", Core.Domain.MediaItemState.Normal, false);
        var selected = new HashSet<MediaCardViewModel> { existingCard };

        var first = new MediaCardViewModel(2, "First", "Sub", "First", "", Core.Domain.MediaItemState.Normal, false);
        var second = new MediaCardViewModel(3, "Second", "Sub", "Second", "", Core.Domain.MediaItemState.Normal, false);

        MediaCardViewModel last = MultiSelectBase<Search>.SelectAllPageItems(selected, new[] { first, second });

        selected.ShouldBe(new[] { first, second }, ignoreOrder: true);
        last.ShouldBe(second);
    }

    [Test]
    public void Should_clear_selection_when_no_cards()
    {
        var existingCard = new MediaCardViewModel(1, "Existing", "Sub", "Existing", "", Core.Domain.MediaItemState.Normal, false);
        var selected = new HashSet<MediaCardViewModel> { existingCard };

        MediaCardViewModel last = MultiSelectBase<Search>.SelectAllPageItems(selected, []);

        selected.ShouldBeEmpty();
        last.ShouldBeNull();
    }
}
