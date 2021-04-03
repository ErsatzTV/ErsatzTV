using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ErsatzTV.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ErsatzTV.Pages
{
    public class FragmentNavigationBase : ComponentBase, IDisposable
    {
        [Inject]
        private NavigationManager NavManager { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        public void Dispose() => NavManager.LocationChanged -= TryFragmentNavigation;

        protected override void OnInitialized() => NavManager.LocationChanged += TryFragmentNavigation;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await NavManager.NavigateToFragmentAsync(JsRuntime);
            }
        }

        [SuppressMessage("ReSharper", "VSTHRD100")]
        private async void TryFragmentNavigation(object sender, LocationChangedEventArgs args)
        {
            try
            {
                await NavManager.NavigateToFragmentAsync(JsRuntime);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
