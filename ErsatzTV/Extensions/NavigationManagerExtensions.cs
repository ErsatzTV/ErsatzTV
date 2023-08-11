using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ErsatzTV.Extensions;

public static class NavigationManagerExtensions
{
    public static async Task NavigateToFragmentAsync(this NavigationManager navigationManager, IJSRuntime jSRuntime)
    {
        Uri uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

        if (uri.Fragment.Length > 0)
        {
            await Task.Delay(250);
            await jSRuntime.InvokeVoidAsync("blazorHelpers.scrollToFragment", uri.Fragment.Substring(1));
        }
    }
}
