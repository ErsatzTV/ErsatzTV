using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    // TODO: this needs a better name
    public class LocalMediaSourcePlanner : ILocalMediaSourcePlanner
    {
        private static readonly Seq<string> ImageFileExtensions = Seq("jpg", "jpeg", "png", "gif", "tbn");
        private readonly ILocalFileSystem _localFileSystem;

        public LocalMediaSourcePlanner(ILocalFileSystem localFileSystem) => _localFileSystem = localFileSystem;

        public Seq<LocalMediaSourcePlan> DetermineActions(
            MediaType mediaType,
            Seq<MediaItem> mediaItems,
            Seq<string> files)
        {
            var results = new IntermediateResults();
            Seq<string> videoFiles = files.Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                .Filter(f => !IsExtra(f));

            (Seq<string> newFiles, Seq<MediaItem> existingMediaItems) = videoFiles.Map(
                    s => mediaItems.Find(i => i.Path == s).ToEither(s))
                .Partition();

            // new files
            foreach (string file in newFiles)
            {
                results.Add(file, new ActionPlan(file, ScanningAction.Add));
                results.Add(file, new ActionPlan(file, ScanningAction.Statistics));

                Option<string> maybeNfoFile = LocateNfoFile(mediaType, files, file);
                maybeNfoFile.BiIter(
                    nfoFile =>
                    {
                        results.Add(file, new ActionPlan(nfoFile, ScanningAction.SidecarMetadata));
                        results.Add(file, new ActionPlan(nfoFile, ScanningAction.Collections));
                    },
                    () =>
                    {
                        results.Add(file, new ActionPlan(file, ScanningAction.FallbackMetadata));
                        results.Add(file, new ActionPlan(file, ScanningAction.Collections));
                    });

                Option<string> maybePoster = LocatePoster(mediaType, files, file);
                maybePoster.IfSome(
                    posterFile => results.Add(file, new ActionPlan(posterFile, ScanningAction.Poster)));
            }

            // existing media items
            foreach (MediaItem mediaItem in existingMediaItems)
            {
                if ((mediaItem.LastWriteTime ?? DateTime.MinValue) < _localFileSystem.GetLastWriteTime(mediaItem.Path))
                {
                    results.Add(mediaItem, new ActionPlan(mediaItem.Path, ScanningAction.Statistics));
                }

                Option<string> maybeNfoFile = LocateNfoFile(mediaType, files, mediaItem.Path);
                maybeNfoFile.IfSome(
                    nfoFile =>
                    {
                        if (mediaItem.Metadata == null || mediaItem.Metadata.Source == MetadataSource.Fallback ||
                            (mediaItem.Metadata.LastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(nfoFile))
                        {
                            results.Add(mediaItem, new ActionPlan(nfoFile, ScanningAction.SidecarMetadata));
                            results.Add(mediaItem, new ActionPlan(nfoFile, ScanningAction.Collections));
                        }
                    });

                Option<string> maybePoster = LocatePoster(mediaType, files, mediaItem.Path);
                maybePoster.IfSome(
                    posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(mediaItem.Poster) ||
                            (mediaItem.PosterLastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            results.Add(mediaItem, new ActionPlan(posterFile, ScanningAction.Poster));
                        }
                    });
            }

            // missing media items
            foreach (MediaItem mediaItem in mediaItems.Where(i => !files.Contains(i.Path)))
            {
                results.Add(mediaItem, new ActionPlan(mediaItem.Path, ScanningAction.Remove));
            }

            return results.Summarize();
        }

        private static bool IsExtra(string path)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            string file = Path.GetFileNameWithoutExtension(path);
            return ExtraDirectories.Contains(folder, StringComparer.OrdinalIgnoreCase)
                   || ExtraFiles.Any(f => file.EndsWith(f, StringComparison.OrdinalIgnoreCase));
        }

        private static Option<string> LocateNfoFile(MediaType mediaType, Seq<string> files, string file)
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    string movieAsNfo = Path.ChangeExtension(file, "nfo");
                    string movieNfo = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, "movie.nfo");
                    return Seq(movieAsNfo, movieNfo)
                        .Filter(s => files.Contains(s))
                        .HeadOrNone();
                case MediaType.TvShow:
                    string episodeAsNfo = Path.ChangeExtension(file, "nfo");
                    return Optional(episodeAsNfo)
                        .Filter(s => files.Contains(s))
                        .HeadOrNone();
            }

            return None;
        }

        private static Option<string> LocatePoster(MediaType mediaType, Seq<string> files, string file)
        {
            string folder = Path.GetDirectoryName(file) ?? string.Empty;

            switch (mediaType)
            {
                case MediaType.Movie:
                    IEnumerable<string> possibleMoviePosters = ImageFileExtensions.Collect(
                            ext => new[] { $"poster.{ext}", Path.GetFileNameWithoutExtension(file) + $"-poster.{ext}" })
                        .Map(f => Path.Combine(folder, f));
                    return possibleMoviePosters.Filter(p => files.Contains(p)).HeadOrNone();
                case MediaType.TvShow:
                    string parentFolder = Directory.GetParent(folder)?.FullName ?? string.Empty;
                    IEnumerable<string> possibleTvPosters = ImageFileExtensions
                        .Collect(ext => new[] { $"poster.{ext}" })
                        .Map(f => Path.Combine(parentFolder, f));
                    return possibleTvPosters.Filter(p => files.Contains(p)).HeadOrNone();
            }

            return None;
        }

        private class IntermediateResults
        {
            private readonly List<Tuple<Either<string, MediaItem>, ActionPlan>> _rawResults = new();

            public void Add(Either<string, MediaItem> source, ActionPlan plan) =>
                _rawResults.Add(Tuple(source, plan));

            public Seq<LocalMediaSourcePlan> Summarize() =>
                _rawResults
                    .GroupBy(t => t.Item1)
                    .Select(g => new LocalMediaSourcePlan(g.Key, g.Select(g2 => g2.Item2).ToList()))
                    .ToSeq();
        }

        // @formatter:off
        private static readonly Seq<string> VideoFileExtensions = Seq(
            ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
            ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts");
        
        private static readonly Seq<string> ExtraDirectories = Seq(
            "behind the scenes", "deleted scenes", "featurettes",
            "interviews", "scenes", "shorts", "trailers", "other",
            "extras", "specials");
        
        private static readonly Seq<string> ExtraFiles = Seq(
            "behindthescenes", "deleted", "featurette",
            "interview", "scene", "short", "trailer", "other");
        // @formatter:on
    }
}
