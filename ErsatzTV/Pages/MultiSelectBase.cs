using ErsatzTV.Application.MediaCards;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core;
using ErsatzTV.Shared;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ErsatzTV.Pages;

public class MultiSelectBase<T> : FragmentNavigationBase
{
    private Option<MediaCardViewModel> _recentlySelected;

    public MultiSelectBase()
    {
        _recentlySelected = None;
        SelectedItems = new System.Collections.Generic.HashSet<MediaCardViewModel>();
    }

    [Inject]
    protected IDialogService Dialog { get; set; }

    [Inject]
    protected ISnackbar Snackbar { get; set; }

    [Inject]
    protected ILogger<T> Logger { get; set; }

    [Inject]
    protected IMediator Mediator { get; set; }

    protected System.Collections.Generic.HashSet<MediaCardViewModel> SelectedItems { get; }

    protected bool IsSelected(MediaCardViewModel card) =>
        SelectedItems.Contains(card);

    protected bool IsSelectMode() =>
        SelectedItems.Any();

    protected string SelectionLabel() =>
        $"{SelectedItems.Count} {(SelectedItems.Count == 1 ? "Item" : "Items")} Selected";

    protected void ClearSelection()
    {
        SelectedItems.Clear();
        _recentlySelected = None;
    }

    protected virtual Task RefreshData() => Task.CompletedTask;

    protected void SelectClicked(
        Func<List<MediaCardViewModel>> getSortedItems,
        MediaCardViewModel card,
        MouseEventArgs e)
    {
        if (SelectedItems.Contains(card))
        {
            SelectedItems.Remove(card);
        }
        else
        {
            if (e.ShiftKey && _recentlySelected.IsSome)
            {
                List<MediaCardViewModel> sorted = getSortedItems();

                int start = sorted.IndexOf(_recentlySelected.ValueUnsafe());
                int finish = sorted.IndexOf(card);
                if (start > finish)
                {
                    (start, finish) = (finish, start);
                }

                for (int i = start; i < finish; i++)
                {
                    SelectedItems.Add(sorted[i]);
                }
            }

            _recentlySelected = card;
            SelectedItems.Add(card);
        }
    }

    protected Task AddSelectionToCollection() => AddItemsToCollection(
        SelectedItems.OfType<MovieCardViewModel>().Map(m => m.MovieId).ToList(),
        SelectedItems.OfType<TelevisionShowCardViewModel>().Map(s => s.TelevisionShowId).ToList(),
        SelectedItems.OfType<TelevisionSeasonCardViewModel>().Map(s => s.TelevisionSeasonId).ToList(),
        SelectedItems.OfType<TelevisionEpisodeCardViewModel>().Map(e => e.EpisodeId).ToList(),
        SelectedItems.OfType<ArtistCardViewModel>().Map(a => a.ArtistId).ToList(),
        SelectedItems.OfType<MusicVideoCardViewModel>().Map(mv => mv.MusicVideoId).ToList(),
        SelectedItems.OfType<OtherVideoCardViewModel>().Map(ov => ov.OtherVideoId).ToList(),
        SelectedItems.OfType<SongCardViewModel>().Map(s => s.SongId).ToList());

    protected async Task AddItemsToCollection(
        List<int> movieIds,
        List<int> showIds,
        List<int> seasonIds,
        List<int> episodeIds,
        List<int> artistIds,
        List<int> musicVideoIds,
        List<int> otherVideoIds,
        List<int> songIds,
        string entityName = "selected items")
    {
        int count = movieIds.Count + showIds.Count + seasonIds.Count + episodeIds.Count + artistIds.Count +
                    musicVideoIds.Count + otherVideoIds.Count + songIds.Count;

        var parameters = new DialogParameters
            { { "EntityType", count.ToString() }, { "EntityName", entityName } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

        IDialogReference dialog =
            await Dialog.ShowAsync<AddToCollectionDialog>("Add To Collection", parameters, options);
        DialogResult result = await dialog.Result;
        if (!result.Canceled && result.Data is MediaCollectionViewModel collection)
        {
            var request = new AddItemsToCollection(
                collection.Id,
                movieIds,
                showIds,
                seasonIds,
                episodeIds,
                artistIds,
                musicVideoIds,
                otherVideoIds,
                songIds);

            Either<BaseError, Unit> addResult = await Mediator.Send(request, CancellationToken);
            addResult.Match(
                Left: error =>
                {
                    Snackbar.Add($"Unexpected error adding items to collection: {error.Value}");
                    Logger.LogError("Unexpected error adding items to collection: {Error}", error.Value);
                },
                Right: _ =>
                {
                    Snackbar.Add(
                        $"Added {count} items to collection {collection.Name}",
                        Severity.Success);
                    ClearSelection();
                });
        }
    }

    protected async Task RemoveSelectionFromCollection(int collectionId)
    {
        var parameters = new DialogParameters
            { { "EntityType", SelectedItems.Count.ToString() }, { "EntityName", "selected items" } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

        IDialogReference dialog = await Dialog.ShowAsync<RemoveFromCollectionDialog>(
            "Remove From Collection",
            parameters,
            options);
        DialogResult result = await dialog.Result;
        if (!result.Canceled)
        {
            var itemIds = SelectedItems.Map(vm => vm.MediaItemId).ToList();

            await Mediator.Send(
                new RemoveItemsFromCollection(collectionId)
                {
                    MediaItemIds = itemIds
                },
                CancellationToken);

            await RefreshData();
            ClearSelection();
        }
    }
}
