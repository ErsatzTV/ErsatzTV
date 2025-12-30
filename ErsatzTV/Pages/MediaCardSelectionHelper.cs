using ErsatzTV.Application.MediaCards;
using System.Collections.Generic;
using System.Linq;

namespace ErsatzTV.Pages;

public static class MediaCardSelectionHelper
{
    public static MediaCardViewModel SelectAllPageItems(
        ISet<MediaCardViewModel> selectedItems,
        IEnumerable<MediaCardViewModel> cards)
    {
        selectedItems.Clear();

        MediaCardViewModel last = default;
        foreach (MediaCardViewModel card in cards ?? Enumerable.Empty<MediaCardViewModel>())
        {
            last = card;
            selectedItems.Add(card);
        }

        return last;
    }
}
