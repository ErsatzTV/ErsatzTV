using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaCollections.Commands;
using ErsatzTV.Core;
using ErsatzTV.Shared;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MudBlazor;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Pages
{
    public class MultiSelectBase<T> : FragmentNavigationBase
    {
        private readonly System.Collections.Generic.HashSet<MediaCardViewModel> _selectedItems;
        private Option<MediaCardViewModel> _recentlySelected;

        public MultiSelectBase()
        {
            _recentlySelected = None;
            _selectedItems = new System.Collections.Generic.HashSet<MediaCardViewModel>();
        }

        [Inject]
        protected IDialogService Dialog { get; set; }

        [Inject]
        protected ISnackbar Snackbar { get; set; }

        [Inject]
        protected ILogger<T> Logger { get; set; }

        [Inject]
        protected IMediator Mediator { get; set; }

        protected bool IsSelected(MediaCardViewModel card) =>
            _selectedItems.Contains(card);

        protected bool IsSelectMode() =>
            _selectedItems.Any();

        protected string SelectionLabel() =>
            $"{_selectedItems.Count} {(_selectedItems.Count == 1 ? "Item" : "Items")} Selected";

        protected void ClearSelection()
        {
            _selectedItems.Clear();
            _recentlySelected = None;
        }

        protected virtual Task RefreshData() => Task.CompletedTask;

        protected void SelectClicked(
            Func<List<MediaCardViewModel>> getSortedItems,
            MediaCardViewModel card,
            MouseEventArgs e)
        {
            if (_selectedItems.Contains(card))
            {
                _selectedItems.Remove(card);
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
                        int temp = start;
                        start = finish;
                        finish = temp;
                    }

                    for (int i = start; i < finish; i++)
                    {
                        _selectedItems.Add(sorted[i]);
                    }
                }

                _recentlySelected = card;
                _selectedItems.Add(card);
            }
        }

        protected Task AddSelectionToCollection() => AddItemsToCollection(
            _selectedItems.OfType<MovieCardViewModel>().Map(m => m.MovieId).ToList(),
            _selectedItems.OfType<TelevisionShowCardViewModel>().Map(s => s.TelevisionShowId).ToList(),
            _selectedItems.OfType<MusicVideoCardViewModel>().Map(mv => mv.MusicVideoId).ToList());

        protected async Task AddItemsToCollection(
            List<int> movieIds,
            List<int> showIds,
            List<int> musicVideoIds,
            string entityName = "selected items")
        {
            int count = movieIds.Count + showIds.Count + musicVideoIds.Count;

            var parameters = new DialogParameters
                { { "EntityType", count.ToString() }, { "EntityName", entityName } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

            IDialogReference dialog = Dialog.Show<AddToCollectionDialog>("Add To Collection", parameters, options);
            DialogResult result = await dialog.Result;
            if (!result.Cancelled && result.Data is MediaCollectionViewModel collection)
            {
                var request = new AddItemsToCollection(collection.Id, movieIds, showIds, musicVideoIds);

                Either<BaseError, Unit> addResult = await Mediator.Send(request);
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
                { { "EntityType", _selectedItems.Count.ToString() }, { "EntityName", "selected items" } };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

            IDialogReference dialog = Dialog.Show<RemoveFromCollectionDialog>(
                "Remove From Collection",
                parameters,
                options);
            DialogResult result = await dialog.Result;
            if (!result.Cancelled)
            {
                var itemIds = new List<int>();
                itemIds.AddRange(_selectedItems.OfType<MovieCardViewModel>().Map(m => m.MovieId));
                itemIds.AddRange(_selectedItems.OfType<TelevisionShowCardViewModel>().Map(s => s.TelevisionShowId));
                itemIds.AddRange(_selectedItems.OfType<TelevisionSeasonCardViewModel>().Map(s => s.TelevisionSeasonId));
                itemIds.AddRange(_selectedItems.OfType<TelevisionEpisodeCardViewModel>().Map(e => e.EpisodeId));
                itemIds.AddRange(_selectedItems.OfType<MusicVideoCardViewModel>().Map(mv => mv.MusicVideoId));

                await Mediator.Send(
                    new RemoveItemsFromCollection(collectionId)
                    {
                        MediaItemIds = itemIds
                    });

                await RefreshData();
                ClearSelection();
            }
        }
    }
}
