using System;
using ErsatzTV.Application.Filler.Commands;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.ViewModels
{
    public class FillerPresetEditViewModel
    {
        private FillerKind _fillerKind;
        private TimeSpan? _duration;
        private FillerMode _fillerMode;
        private int? _count;

        public int Id { get; set; }
        public string Name { get; set; }

        public FillerKind FillerKind
        {
            get => _fillerKind;
            set
            {
                _fillerKind = value;
                if (_fillerKind is FillerKind.Fallback or FillerKind.Tail)
                {
                    FillerMode = FillerMode.None;
                }
            }
        }

        public FillerMode FillerMode
        {
            get => FillerKind is FillerKind.Fallback or FillerKind.Tail ? FillerMode.None : _fillerMode;
            set => _fillerMode = value;
        }

        public TimeSpan? Duration
        {
            get => FillerMode == FillerMode.Duration ? _duration : null;
            set => _duration = value;
        }

        public int? Count
        {
            get => FillerMode == FillerMode.Multiple ? _count : null;
            set => _count = value;
        }

        public int? PadToNearestMinute { get; set; }
        public ProgramScheduleItemCollectionType CollectionType { get; set; }
        public MediaCollectionViewModel Collection { get; set; }
        public NamedMediaItemViewModel MediaItem { get; set; }
        public MultiCollectionViewModel MultiCollection { get; set; }
        public SmartCollectionViewModel SmartCollection { get; set; }

        public IRequest<Either<BaseError, Unit>> ToEdit() =>
            new UpdateFillerPreset(
                Id,
                Name,
                FillerKind,
                FillerMode,
                Duration,
                Count,
                PadToNearestMinute,
                CollectionType,
                Collection?.Id,
                MediaItem?.MediaItemId,
                MultiCollection?.Id,
                SmartCollection?.Id);

        public IRequest<Either<BaseError, Unit>> ToUpdate() =>
            new CreateFillerPreset(
                Name,
                FillerKind,
                FillerMode,
                Duration,
                Count,
                PadToNearestMinute,
                CollectionType,
                Collection?.Id,
                MediaItem?.MediaItemId,
                MultiCollection?.Id,
                SmartCollection?.Id);
    }
}
