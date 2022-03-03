using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ErsatzTV.Extensions;

public static class NavigationManagerExtensions
{
    public static ValueTask NavigateToFragmentAsync(this NavigationManager navigationManager, IJSRuntime jSRuntime)
    {
        Uri uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

        if (uri.Fragment.Length == 0)
        {
            return default;
        }

        return jSRuntime.InvokeVoidAsync("blazorHelpers.scrollToFragment", uri.Fragment.Substring(1));
    }
}