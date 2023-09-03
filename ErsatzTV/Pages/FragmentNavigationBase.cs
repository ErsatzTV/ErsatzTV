using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ErsatzTV.Pages;

public class FragmentNavigationBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private bool _disposedValue;

    protected CancellationToken CancellationToken => _cts.Token;

    [Inject]
    private NavigationManager NavManager { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                NavManager.LocationChanged -= TryFragmentNavigation;

                _cts?.Cancel();
                _cts?.Dispose();
            }

            _disposedValue = true;
        }
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
