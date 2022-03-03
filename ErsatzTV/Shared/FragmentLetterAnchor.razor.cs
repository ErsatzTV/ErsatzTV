using ErsatzTV.Application.MediaCards;
using Microsoft.AspNetCore.Components;

namespace ErsatzTV.Shared;

public partial class FragmentLetterAnchor<TCard> where TCard : MediaCardViewModel
{
    [Parameter]
    public RenderFragment<TCard> ChildContent { get; set; }

    [Parameter]
    public List<TCard> Cards { get; set; }
}