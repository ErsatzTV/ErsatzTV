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
        List<MediaCardViewModel> cardList = (cards ?? Enumerable.Empty<MediaCardViewModel>()).ToList();

        selectedItems.Clear();
        selectedItems.UnionWith(cardList);

        return cardList.LastOrDefault();
    }
}
