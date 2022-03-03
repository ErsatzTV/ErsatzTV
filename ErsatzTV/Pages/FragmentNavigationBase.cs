using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ErsatzTV.Pages
{
    public class FragmentNavigationBase : ComponentBase, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        protected CancellationToken CancellationToken => _cts.Token;
        
        [Inject]
        private NavigationManager NavManager { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        public void Dispose()
        {
            NavManager.LocationChanged -= TryFragmentNavigation;

            _cts?.Cancel();
            _cts?.Dispose();
        }

        protected override void OnInitialized() => NavManager.LocationChanged += TryFragmentNavigation;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
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
