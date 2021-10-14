# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]
### Fixed
- Fix error message/offline stream continuity with channels that use HLS Segmenter
- Fix removing items from search index when folders are removed from local libraries

### Added
- Add `Other Video` local libraries
  - Other video items require no metadata or particular folder layout, and will have tags added for each containing folder
  - For Example, a video at `commercials/sd/1990/whatever.mkv` will have the tags `commercials`, `sd` and `1990`, and the title `whatever`
- Add filler `Tail Mode` option to `Duration` playout mode (in addition to existing `Offline` option)
  - Filler collection will always be randomized (to fill as much time as possible)
  - Filler will be hidden from channel guide, but visible in playout details in ErsatzTV
  - Unfilled time will show offline image
- Add `Guide Mode` option to all schedule items
  - `Normal` guide mode will show all scheduled items in the channel guide (xmltv)
  - `Filler` guide mode will hide all scheduled items from the channel guide, and extend the end time for the previous item in the guide

## [0.1.3-alpha] - 2021-10-12
### Fixed
- Fix startup bug for some docker installations

## [0.1.2-alpha] - 2021-10-12
### Added
- Include more cuda (nvidia) filters in docker image
- Enable deinterlacing with nvidia using new `yadif_cuda` filter
- Add two HLS Segmenter settings: idle timeout and work-ahead limit
  - `HLS Segmenter Idle Timeout` - the number of seconds to keep transcoding a channel while no requests have been received from any client
    - This setting must be greater than or equal to 30 (seconds)
  - `Work-Ahead HLS Segmenter Limit` - the number of segmenters (channels) that will work-ahead simultaneously (if multiple channels are being watched)
    - "working ahead" means transcoding at full speed, which can take a lot of resources
    - This setting must be greater than or equal to 0
- Add more watermark locations ("middle" of each side)
- Add `VAAPI Device` setting to ffmpeg profile to support installations with multiple video cards
- Add *experimental* `RadeonSI` option for `VAAPI Driver` and include mesa drivers in vaapi docker image

### Changed
- Upgrade ffmpeg from 4.3 to 4.4 in all docker images
  - Upgrading from 4.3 to 4.4 is recommended for all installations
- Move `VAAPI Driver` from settings page to ffmpeg profile to support installations with multiple video cards

### Fixed
- Fix some transcoding edge cases with nvidia and pixel format `yuv420p10le`

## [0.1.1-alpha] - 2021-10-10
### Added
- Add music video album to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Remove forced initial delay from `HLS Segmenter` streaming mode
- Upgrade nvidia docker image from 18.04 to 20.04

## [0.1.0-alpha] - 2021-10-08
### Added
- Add *experimental* streaming mode `HLS Segmenter` (most similar to `HLS Hybrid`) 
  - This mode is intended to increase client compatibility and reduce issues at program boundaries
  - If you want the temporary transcode files to be located on a particular drive, the docker path is `/root/.local/share/etv-transcode`
- Store frame rate with media statistics; this is needed to support HLS Segmenter
  - This requires re-ingesting statistics for all media items the first time this version is launched

### Changed
- Use latest iHD driver (21.2.3 vs 20.1.1) in vaapi docker images

### Fixed
- Add downsampling to support transcoding 10-bit HEVC content with the h264_vaapi encoder
- Fix updating statistics when media items are replaced
- Fix XMLTV generation when scheduled episode is missing metadata

## [0.0.62-alpha] - 2021-10-05
### Added
- Support IMDB ids from Plex libraries, which may improve Trakt matching for some items

### Fixed
- Include Specials/Season 0 `episode-num` entry in XMLTV
- Fix some transcoding edge cases with VAAPI and pixel formats `yuv420p10le`, `yuv444p10le` and `yuv444p`
- Update Plex movie and episode paths when they are changed within Plex
- Always use `libx264` software encoder for error messages

## [0.0.61-alpha] - 2021-09-30
### Fixed
- Revert nvenc/cuda filter change from v60

## [0.0.60-alpha] - 2021-09-25
### Added
- Add Trakt list support under `Lists` > `Trakt Lists`
  - Trakt lists can be added by url or by `user/list`
  - To re-download a Trakt list, simply add it again (no need to delete)
  - See `Logs` for unmatched item details
  - Trakt lists can only be scheduled by using Smart Collections
- Add seasons to search index
  - This is needed because Trakt lists can contain seasons 
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Fixed
- Fix local television scanner to properly update episode metadata when NFO files have been added/changed
- Properly detect ffmpeg nvenc (cuda) support in Hardware Acceleration health check
- Fix nvenc/cuda filter for some yuv420p content

## [0.0.59-alpha] - 2021-09-18
### Added
- Add `Health Checks` table to home page to identify and surface common misconfigurations
  - `FFmpeg Version` checks `ffmpeg` and `ffprobe` versions
  - `FFmpeg Reports` checks whether ffmpeg troubleshooting reports are enabled since they can use a lot of disk space over time
  - `Hardware Acceleration` checks whether channels that transcode are using acceleration methods that ffmpeg claims to support
  - `Movie Metadata` checks whether all movies have metadata (fallback metadata counts as metadata)
  - `Episode Metadata` checks whether all episodes have metadata (fallback metadata counts as metadata)
  - `Zero Duration` checks whether all movies and episodes have a valid (non-zero) duration
  - `VAAPI Driver` checks whether a vaapi driver preference is configured when using the vaapi docker image
- Add setting to each playout to schedule an automatic daily rebuild
  - This is useful if the playout uses a smart collection with `released_onthisday`

### Fixed
- Fix docker vaapi support for newer Intel platforms (Gen 8+)
  - This includes a new setting to force a particular vaapi driver (`iHD` or `i965`), as some Gen 8 or 9 hardware that is supported by both drivers will perform better with one or the other
- Fix scanning and indexing local movies and episodes without NFO metadata
- Fix displaying seasons for shows with no year (in metadata or in folder name)
- Fix "direct play" in MPEG-TS mode (copy audio and video stream when `Transcode` is unchecked)

## [0.0.58-alpha] - 2021-09-15
### Added
- Add `released_notinthelast` search field for relative release date queries
  - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`
- Add `released_onthisday` search field for historical queries
  - Syntax is `released_onthisday:1` and will search for items released on this month number and day number in prior years
- Add tooltip explaining `Keep Multi-Part Episodes Together`

### Fixed
- Properly display watermark when no other video filters (like scaling or padding) are required
- Fix building some playouts in timezones with positive offsets (like UTC+2)
- Fix `Shuffle In Order` so all collections/shows start from the earliest episode
  - You may need to rebuild playouts to see this fixed behavior more quickly

## [0.0.57-alpha] - 2021-09-10
### Added
- Add `released_inthelast` search field for relative release date queries
  - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`
- Allow adding smart collections to multi collections

### Fixed
- Fix loading artwork in Kodi
  - Use fake image extension (`.jpg`) for artwork in M3U and XMLTV since Kodi detects MIME type from URL
  - Enable HEAD requests for IPTV image paths since Kodi requires those

## [0.0.56-alpha] - 2021-09-10
### Added
- Add Smart Collections
  - Smart Collections use search queries and can be created from the search result page
  - Smart Collections are re-evaluated every time playouts are extended or rebuilt to automatically include newly-matching items
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete
- Allow `Shuffle In Order` with Collections and Smart Collections
  - Episodes will be grouped by show, and music videos will be grouped by artist
  - All movies will be a single group (multi-collections are probably better if `Shuffle In Order` is desired for movies)
  - All groups will be be ordered chronologically (custom ordering is only supported in multi-collections)

### Fixed
- Generate XMLTV that validates successfully
  - Properly order elements
  - Omit channels with no programmes
  - Properly identify channels using the format number.etv like `15.etv`
- Fix building playouts when multi-part episode grouping is enabled and episodes are missing metadata
- Fix incorrect total items count in `Multi Collections` table

## [0.0.55-alpha] - 2021-09-03
### Fixed
- Fix all local library scanners to ignore dot underscore files (`._`)

## [0.0.54-alpha] - 2021-08-21
### Added
- Add `Shuffle In Order` playback order for multi-collections.
  - This is useful for randomizing multiple collections/shows on a single channel, while each collection maintains proper ordering (custom or chronological)

### Fixed
- Fix bug parsing ffprobe output in cultures where `.` is a group/thousands separator
  - This bug likely prevented ETV from scheduling correctly or working at all in those cultures
  - After installing a version with this fix, affected content will need to be removed from ETV and re-added

## [0.0.53-alpha] - 2021-08-01
### Fixed
- Fix error message displayed after building empty playout
- Fix Emby and Jellyfin links

### Changed
- Always proxy Jellyfin and Emby artwork; this fixes artwork in some networking scenarios

## [0.0.52-alpha] - 2021-07-22
### Added
- Add multiple local libraries to better organize your media
- Add `Move Library Path` function to support reorganizing existing local libraries

### Fixed
- Fix bug preventing playouts from rebuilding after an empty collection is encountered within a multi-collection

## [0.0.51-alpha] - 2021-07-18
### Added
- Add `Multi Collection` to support shuffling multiple collections within a single schedule item
  - Collections within a multi collection are optionally grouped together and ordered when scheduling; this can be useful for franchises
- Add `Playout Days To Build` setting to control how many days of playout data/program guide data should be built into the future

### Changed
- Move `Playback Order` from schedule to schedule items
  - This allows different schedule items to have different playback orders within a single schedule

### Fixed
- Fix release notes on home page with `-alpha` suffix
- Fix linux-arm release by including SQLite interop artifacts
- Fix issue where cached Plex credentials may become invalid when multiple servers are used

## [0.0.50-alpha] - 2021-07-13
### Added
- Add Linux ARM release artifacts which can be used on Raspberry Pi devices

### Fixed
- Fix bug preventing ingestion of local movies with fallback metadata (without NFO files)
- Fix extra spaces in titles of local movies with fallback metadata (without NFO files)

## [0.0.49-prealpha] - 2021-07-11
### Added
- Include audio language metadata in all streaming modes
- Add special zero-count case to `Multiple` playout mode
  - This configuration will automatically maintain the multiple count so that it is equal to the number of items in the collection
  - This configuration should be used if you want to play every item in a collection exactly once before advancing

### Changed
- Use case-insensitive sorting for collections page and `Add to Collection` dialog
- Use case-insensitive sorting for all collection lists in schedule items editor
- Use natural sorting for schedules page and `Add to Schedule` dialog

### Fixed
- Fix flooding schedule items that have a fixed start time

## [0.0.48-prealpha] - 2021-06-22
### Added
- Store pixel format with media statistics; this is needed to support normalization of 10-bit media items
  - This requires re-ingesting statistics for all media items the first time this version is launched

### Changed
- Use ffprobe to retrieve statistics for Plex media items (Local, Emby and Jellyfin libraries already use ffprobe)

### Fixed
- Fix playback of transcoded 10-bit media items (pixel format `yuv420p10le`) on Nvidia hardware
- Emby and Jellyfin scanners now respect library refresh interval setting
- Fix adding new seasons to existing Emby and Jellyfin shows
- Fix adding new episodes to existing Emby and Jellyfin seasons

## [0.0.47-prealpha] - 2021-06-15
### Added
- Add warning during playout rebuild when schedule has been emptied
- Save Logs, Playout Detail, Schedule Detail table page sizes

### Changed
- Show all log entries in log viewer, not just most recent 100 entries
- Use server-side paging and sorting for Logs table
- Use server-side paging for Playout Detail table
- Remove pager from Schedule Items editor (all schedule items will always be displayed)

### Fixed
- Fix ui crash adding a channel without a watermark
- Clear playout detail table when playout is deleted
- Fix blazor error font color
- Fix some audio stream languages missing from UI and search index
- Fix audio stream selection for languages with multiple codes
- Fix searching when queries contain non-ascii characters

## [0.0.46-prealpha] - 2021-06-14
### Added
- Add watermark opacity setting to allow blending with content
- Add global watermark setting; channel-specific watermarks have precedence over global watermarks
- Save Schedules, Playouts table page sizes

### Changed
- Remove unused API and SDK project; may reintroduce in the future but for now they have fallen out of date
- Rework watermarks to be separate from channels (similar to ffmpeg profiles)
  - **All existing watermarks have been removed and will need to be recreated using the new page**
  - This allows easy watermark reuse across channels

### Fixed
- Fix ui crash adding or editing schedule items due to Artist with no name
- Fix many potential sources of inconsistent data in UI

## [0.0.45-prealpha] - 2021-06-12
### Added
- Add experimental `HLS Hybrid` channel mode
  - Media items are transcoded using the channel's ffmpeg profile and served using HLS
- Add optional channel watermark

### Changed
- Remove framerate normalization; it caused more problems than it solved
- Include non-US (and unknown) content ratings in XMLTV

### Fixed
- Fix serving channels.m3u with missing content ratings
- Fix percent progress indicator for Jellyfin and Emby show library scans

## [0.0.44-prealpha] - 2021-06-09
### Added
- Add artists directly to schedules
- Include MPAA and VCHIP content ratings in XMLTV guide data
- Quickly skip missing files during Plex library scan

### Fixed
- Ignore unsupported plex guids (this prevented some libraries from scanning correctly)
- Ignore unsupported STRM files from Jellyfin

## [0.0.43-prealpha] - 2021-06-05
### Added
- Support `(Part #)` name suffixes for multi-part episode grouping
- Support multi-episode files in local and Plex libraries
- Save Channels table page size
- Add optional query string parameter to M3U channel playlist to allow some customization per client
    - `?mode=ts` will force `MPEG-TS` mode for all channels
    - `?mode=hls-direct` will force `HLS Direct` mode for all channels
    - `?mode=mixed` or no parameter will maintain existing behavior

### Changed
- Rename channel mode `TransportStream` to `MPEG-TS` and `HttpLiveStreaming` to `HLS Direct`
- Improve `HLS Direct` mode compatibility with Channels DVR Server

### Fixed
- Fix search result crashes due to missing season metadata

## [0.0.42-prealpha] - 2021-05-31
### Added
- Support roman numerals and english integer names for multi-part episode grouping
- Add option to treat entire collection as a single show with multi-part episode grouping
    - This is useful for multi-part episodes that span multiple shows (crossovers)

### Changed
- Skip zero duration items when building a playout, rather than aborting the playout build

### Fixed
- Fix edge case where a playout rebuild would get stuck and block all other playouts and local library scans

## [0.0.41-prealpha] - 2021-05-30
### Added
- Add button to refresh list of Plex, Jellyfin, Emby libraries without restarting app
- Add episodes to search index
- Add director and writer metadata to episodes
- Add unique id/provider id metadata, which will support future features
- Allow grouping multi-part episodes with titles ending in `Part X`, `Part Y`, etc.

### Changed
- Change home page link from release notes to full changelog

### Fixed
- Fix missing channel logos after restart
- Fix multi-part episode grouping with missing episodes/parts
- Fix multi-part episode grouping in collections containing multiple shows
- Fix updating modified seasons and episodes from Jellyfin and Emby

## [0.0.40-prealpha] - 2021-05-28
### Added
- Add content rating metadata to movies and shows
- Add director and writer metadata to movies
- Sync tv show thumbnail artwork in Local, Jellyfin and Emby libraries (*not* Plex)
- Prioritize tv show thumbnail artwork over tv show posters in XMLTV
- Include tv show artwork in XMLTV when grouped items with custom title are all from the same show
- Cache resized local artwork on disk

### Fixed
- Recursively retrieve Jellyfin and Emby items
- Fix incorrect search item counts
- Fix stack trace information in non-docker releases
- Fix crash opening `Add to Schedule` dialog
- Disable FFmpeg troubleshooting reports on Windows as they do not work properly

## [0.0.39-prealpha] - 2021-05-25
### Added
- Show Jellyfin and Emby artwork in XMLTV

### Fixed
- Fix path replacements for Jellyfin and Emby, including UNC paths
- Use Emby path replacements for playback
- Fix playback when `fps` is the only required filter
- Fix resources (images, fonts) required to display offline channel message

## [0.0.38-prealpha] - 2021-05-23
### Added
- Add support for Emby
- Use "single-file" deployments for releases
    - Non-docker releases will have significantly fewer files
    - It is recommended to empty your installation folder before copying in the latest release.

### Fixed
- Fix some cases where Jellyfin artwork would not display
- Fix saving schedule items with duration less than one hour
- Use ffmpeg 4.3 in docker images; there was a performance regression with 4.4 (only in docker)

## [0.0.37-prealpha] - 2021-05-21
### Added
- Add option to keep multi-part episodes together when shuffling (i.e. two-part season finales)
- Optimize Plex TV Scanner to quickly process shows that have not been updated since the last scan
- Optimize local Movie, Show, Music Video scanners to quickly skip unchanged folders, and to notice any mtime change
- Add server binding configuration to `appsettings.json` which lets non-docker installations bind to localhost or change the port number

### Fixed
- Properly ignore `Other` Jellyfin libraries
- Fix bug where search index would try to re-initialize unnecessarily
- Fix one cause of green line at bottom of some transcoded videos by forcing even scaling targets

## [0.0.36-prealpha] - 2021-05-16
### Added
- Add support for Jellyfin
- Add support for ffmpeg 4.4, and use ffmpeg 4.4 in all docker images
- Add configurable library refresh interval
- Add button to copy/clone ffmpeg profile

## [0.0.35-prealpha] - 2021-04-27
### Added
- Add search button for each library in `Libraries` page to quickly filter content by library
    - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Fixed
- Fix ingesting actors and actor artwork from newly-added Plex media items
- Only show `movie` and `show` libraries from Plex. Other library types are not supported at this time.
- Fix local movie scanner missing replaced/updated files

## [0.0.34-prealpha] - 2021-04-17
### Added
- Allow `enter` key to submit all dialogs
- Add actors to movies and shows (Plex or NFO metadata is required)
    - Note that this requires a one-time full library scan to ingest actor metadata, which may take a long time with large libraries.
- Rework metadata list links in UI (languages, studios, genres, etc)

### Fixed
- Fix EPG generation with music video channels that do not use a custom title
- Fix lag when typing in search bar, `Add To Collection` dialog
- Fix collections paging
- Fix padding odd resolutions (this bug caused some items to always fail playback)
- Only update Plex episode artwork as needed

## [0.0.33-prealpha] - 2021-04-11
### Added
- Add language buttons to movies, shows, artists
- Show release notes on home page

### Fixed
- Re-import missing television metadata that was incorrectly removed with `v0.0.32`
- Fix language indexing; language searches now use full english name
- Fix synchronizing television studios, genres from Plex
- Limit channels to one playout per channel
    - Though more than one playout was previously possible it was unsupported and unlikely to work as expected, if at all
    - A future release may make this possible again
    
## [0.0.32-prealpha] - 2021-04-09
### Added
- `Add All To Collection` button to quickly add all search results to a collection
- Add Artists scanned from Music Video libraries
    - Artist folders are now required, but music videos now have no naming requirements
    - `artist.nfo` metadata is supported along with thumbnail and poster artwork
- Save Collections table page size in local storage

### Fixed
- Fix audio stream language indexing for movies and music videos
- Fix synchronizing list of Plex servers and connection addresses for each server
- Fix `See All` link for music video search results

## [0.0.31-prealpha] - 2021-04-06
### Added
- Add documentation link to UI
- Add `language` search field
- Minor log viewer improvements
- Use fragment navigation with letter bar (clicking a letter will page and scroll until that letter is in view)
- Send all audio streams with HLS when channel has no preferred language
- Move FFmpeg settings to new `Settings` page
- Add HDHR tuner count setting to new `Settings` page

### Fixed
- Fix poster width
- Fix bug that would occasionally prevent items from being added to the search index
- Automatically refresh the Plex Media Sources page after sign in or sign out

## 0.0.30-prealpha [YANKED]

## [0.0.29-prealpha] - 2021-04-04
- No longer require NFO metadata for music videos
    - Instead, the only requirement is that music video files be named `[artist] - [track].[extension]` where the three characters (space dash space) between artist and track are required
- Add library scan progress detail
- Optimize library scans after adding library path to only scan new library path
- Fix bug replacing music videos
- Scan Plex libraries and local libraries on different threads
- Use English names for preferred languages in UI instead of ISO language code

## [0.0.28-prealpha] - 2021-04-03
- Apply audio normalization more consistently; this should further reduce program boundary errors
- Replace unused audio volume setting with audio loudness normalization option
    - This can be particularly helpful with music video channels if media items have inconsistent loudness
    - This setting may be less desirable on movie channels where loudness is intended to be dynamic
- Fix XMLTV containing music videos that do not use a custom title
- Fix channels table sorting, add paging to channels table
- Add sorting and paging to schedules table
- Add paging to playouts table
- Use table instead of cards for collections view

## [0.0.27-prealpha] - 2021-04-02
- Add ***basic*** music video library support
    - **NFO metadata is required for music videos** - see [tags](https://kodi.wiki/view/NFO_files/Music_videos#Music_Video_Tags), [template](https://kodi.wiki/view/NFO_files/Music_videos#Template_nfo) and [sample](https://kodi.wiki/view/NFO_files/Music_videos#Sample_nfo)
    - Artists can be searched using the `artist` field, like `artist:daft`
- Clear search query when clicking `Movies` or `TV Shows` from paged search results
- Add show title to playout details
- Let ffmpeg determine thread count by default (signified by `0` threads in ffmpeg profile)
- Save troubleshooting reports for ffmpeg concat process in addition to transcode process
- Simplify ffmpeg normalization options
- Add frame rate setting to ffmpeg profile
    - When video normalization is enabled, all media items will have their frame rate converted to the same value
- Fix some scenarios where streaming would freeze at program boundaries
- Fix bug preventing some Plex libraries from scanning
- Fix bug preventing some local libraries from scanning folders that were recently added

## [0.0.26-prealpha] - 2021-03-30
- Add `Custom Title` option to schedule items
    - When a custom title is set, the schedule item will be grouped in the EPG with the custom title
- Navigate to schedule items after creating new schedule
- Fix channel editor so preferred language is no longer required on every channel
- Fix bug with audio track selection during non-normalized playback
- Fix bug with playout builds where `Multiple` or `Duration` items wouldn't respect the settings over time
- Fix bug that prevented some television folders from scanning

## [0.0.25-prealpha] - 2021-03-29
- Add preferred language feature
    - Global preference can be set in FFmpeg settings; channels can override global preference
    - Preferences require [ISO 639-2](https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes) language codes
    - Audio stream selection will attempt to respect these preferences and prioritize streams with the most channels
    - English (`eng`) will be used as a fallback when no preferences have been configured
    - ***This feature requires a one-time reanalysis of every media item which may take a long time for large libraries and playback may fail until this scan has completed***
- Fix channel sorting in EPG
- Fix mixed-platform path replacements (Plex on Windows with ErsatzTV on Linux, or Plex on Linux with ErsatzTV on Windows)
- Fix local television library scanning; this was broken with `v0.0.23`
- Optimize local library scanning; regular scans should be significantly faster
- Add log warning when a zero-duration media item is encountered
- Fix indexing local shows without NFO metadata.
    - If you have this issue the best way to fix is to:
        - Shutdown ErsatzTV
        - Delete the `search-index` subfolder inside the ErsatzTV config folder
        - Start ErsatzTV; the full search index will be rebuilt on startup
- Fix updating search index when genres, tags, studios are updated in local libraries
- Adjust artwork routes so all IPTV traffic can be proxied with a single path prefix of `/iptv`

## [0.0.24-prealpha] - 2021-03-22
- Fix a critical bug preventing library synchronization with Plex sign ins performed with `v0.0.22` or `v0.0.23`
    - **If you are unable to sync libraries from Plex, please sign out and back in to apply this fix**
- Fallback to `folder.jpg` when `poster.jpg` is not present
- Attach episodes to correct show when adding NFO metadata to existing libraries

## [0.0.23-prealpha] - 2021-03-21
- Remove all Plex items from search index after sign out
- Fix fallback metadata for local episodes (episode number was missing)
- Improve television show year detection where year is missing from nfo metadata
- Fix sorting for titles that start with `A` or `An` in addition to `The`
- Properly escape search links containing special characters (genre, tag)
- Add and index `Studio` metadata

## [0.0.22-prealpha] - 2021-03-20
- Log errors encountered during search index build; attempt to continue with partial index when errors are encountered
- Only search `title` field by default; `genre` and `tag` can be searched with `field:query` syntax
- Allow leading wildcards in searches
- Keep search query in search field to allow easy modification
- Fix default ffmpeg profile when creating new channels
- Fix multiple bugs with updating Plex servers, libraries, path replacements
- Add `release_date` to search index

## [0.0.21-prealpha] - 2021-03-20
- Optimize local library scanning to use less memory
- Duplicate some documentation near the schedule item editor
- Fix bug with updating `Normalize Video Codec` setting
- Rework search functionality
    - Search landing page will show up to 50 items of each type
    - `See All` links can be used to page through all search results
    - Complex search queries supported (`christmas OR santa`)
    - Fields that are searched by default:
        - `title`
        - `genre`
        - `tag`
    - Fields that aren't searched by default, but can be included in queries with syntax like (`plot:whatever`):
        - `plot`
        - `library_name`
        - `type` (`movie` or `show`)
    - Add letter bar to all paged search results to quickly navigate to a particular letter

## [0.0.20-prealpha] - 2021-03-17
- Fix NVIDIA hardware acceleration in `develop-nvidia` and `latest-nvidia` Docker tags
    - This may never have worked correctly in Docker with older releases
- Fix occasional crash rebuilding playout from ui
- Fix crash adding a channel when no channels exist
- Fix playback for media containing attached pictures

## [0.0.19-prealpha] - 2021-03-16
- Regularly scan Plex libraries (same as local libraries)
- Add ability to create new collection from `Add to Collection` dialog
- Fix channel logos in XMLTV
- Add episode posters (show posters) to XMLTV
- Fix shuffled schedules from occasionally having repeated items when reshuffling
    - This was more likely to happen with low-cardinality collections like A B C C A B B C A
- Add optional FFmpeg troubleshooting reports
- Allow synchronizing hidden Plex libraries

## [0.0.18-prealpha] - 2021-03-14
- Plex is now a supported media source
    - Plex is **not** used for transcoding at this point, files are played directly from the filesystem using ErsatzTV transcoding
    - Path replacements will be needed if your shared media folders are mounted differently in Plex and ErsatzTV

## [0.0.17-prealpha] - 2021-03-13
- Fix bug introduced with 0.0.16 that prevented some playouts from building
- Properly set sort title on added tv shows
- Fix loading season pages containing episodes that have incomplete metadata
- Improve XMLTV guide data

## [0.0.16-prealpha] - 2021-03-12
- Fix infinite loop caused by incorrectly configured ffprobe path
- Add more strict ffmpeg and ffprobe settings validation
- Add custom playback order option to collections that contain only movies
    - This custom playback order will override the schedule's configured playback order for the collection

## [0.0.15-prealpha] - 2021-03-11
- Update UI for tv shows
- Fix tv show sorting
- Fix editing channel numbers
- Fix playout timezone bugs
- Add searchable genres and tags from local NFO metadata
- Add multi-select feature to movies, shows, search results and collection items pages

## [0.0.14-prealpha] - 2021-03-09
- New movie layout utilizing fan art (if available)
- New dark UI
- Fix offline stream (displayed when no media is scheduled for playback)
- Add M3U codec hints for Channels DVR
- Allow sub-channel numbers
- Fix bug where ffmpeg wouldn't terminate after a media item completed playback
- Fix time zone in new docker base images
- Fix vaapi pipeline with mpeg4 content by using software decoder with hardware encoder
- Enforce unique schedule name
- Enforce unique channel number
- Fix sorting of collection items in UI

## [0.0.13-prealpha] - 2021-03-07
- Remember selected Collection in `Add To Collection` dialog
- Automatically rebuild Playouts referencing any Collection that has items added or removed from the UI
- Remove Media Items from database when files are removed from disk
- Add hardware-accelerated transcoding support (`qsv`, `nvenc`/`nvidia`, `vaapi`)
    - All flavors support resolution normalization (scaling and padding)
    - This requires support within ffmpeg; see README for new docker image tags

## [0.0.12-prealpha] - 2021-03-02
- Fix a database migration issue introduced with version 0.0.11
- Shutdown app when database migration failures are encountered at startup

## [0.0.11-prealpha] - 2021-03-01
- Add Libraries and Library Paths under Media Sources
    - Two local libraries exist: `Movies` and `Shows`
    - Local Media Sources from prior versions are now found under Library Paths
- Add `Rebuild Playout` buttons to quickly regenerate playouts after modifying collections
- Add `Add to Collection` buttons to most media cards (movies, shows, seasons, episodes)
- Add Search page for searching movies and shows

## [0.0.10-prealpha] - 2021-02-21
- Rework how television media is stored in the database
- Rework how media is linked to a collection
- Add season, episode and movie detail views to UI
- Add media to collections and schedules from detail views
- Easily add and remove media from a collection
- Easily add and reorder schedule items

## [0.0.9-prealpha] - 2021-02-15
- Local media scanner has been rewritten and is much more performant
- Ignore extras in the same folder as movies (`-behindthescenes`, `-trailer`, etc)
- Support `movie.nfo` metadata in addition to matching filename nfo metadata
- Changes to video files, metadata and posters are automatically detected and used

## [0.0.8-prealpha] - 2021-02-14
- Optimize scanning so playouts are only rebuilt when necessary (duration changes, or collection membership changes)
- Automatically add new posters during scanning
- Support more poster file types (jpg, jpeg, png, gif, tbn)
- Add "Refresh All Metadata" button to media sources page; this should only be needed if NFO metadata or posters are modified
- Add progress indicator for media sources that are being actively scanned
- Prevent deleting media source during scan
- Prevent creating playout with empty schedule

## [0.0.7-prealpha] - 2021-02-13
- Rework media items layout - table has been replaced with cards/posters
- Fix bug preventing long folder names from being used as media sources
- Use 24h time pickers in schedule editor

## [0.0.6-prealpha] - 2021-02-12
- Add version information to UI
- Add basic log viewer to UI

## [0.0.5-prealpha] - 2021-02-12
- Fix bug where media scanner could stop prematurely and miss media items
- Add database migrations

## [0.0.4-prealpha] - 2021-02-11
- **Fix HDHomeRun routes** - this version is required to use as a DVR with Plex, older versions will not work
- Improve metadata parsing for tv, add fallback (filename) parsing for movies

## [0.0.3-prealpha] - 2021-02-11
- Fix incomplete XML issue introduced with v0.0.2-prealpha
- Add `.ts` files to local media scanner
- Change M3U, XMLTV, API icons to text links

## 0.0.2-prealpha - 2021-02-11 [YANKED]
- Relax some searches to be case-insensitive
- Improve categorization of tv episodes without sidecar metadata
- Properly escape XML content in XMLTV

## [0.0.1-prealpha] - 2021-02-10
- Initial release to facilitate testing outside of Docker.


[Unreleased]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.3-alpha...HEAD
[0.1.3-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.2-alpha...v0.1.3-alpha
[0.1.2-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.1-alpha...v0.1.2-alpha
[0.1.1-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.0-alpha...v0.1.1-alpha
[0.1.0-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.62-alpha...v0.1.0-alpha
[0.0.62-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.61-alpha...v0.0.62-alpha
[0.0.61-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.60-alpha...v0.0.61-alpha
[0.0.60-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.59-alpha...v0.0.60-alpha
[0.0.59-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.58-alpha...v0.0.59-alpha
[0.0.58-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.57-alpha...v0.0.58-alpha
[0.0.57-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.56-alpha...v0.0.57-alpha
[0.0.56-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.55-alpha...v0.0.56-alpha
[0.0.55-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.54-alpha...v0.0.55-alpha
[0.0.54-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.53-alpha...v0.0.54-alpha
[0.0.53-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.52-alpha...v0.0.53-alpha
[0.0.52-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.51-alpha...v0.0.52-alpha
[0.0.51-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.50-alpha...v0.0.51-alpha
[0.0.50-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.49-prealpha...v0.0.50-alpha
[0.0.49-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.48-prealpha...v0.0.49-prealpha
[0.0.48-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.47-prealpha...v0.0.48-prealpha
[0.0.47-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.46-prealpha...v0.0.47-prealpha
[0.0.46-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.45-prealpha...v0.0.46-prealpha
[0.0.45-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.44-prealpha...v0.0.45-prealpha
[0.0.44-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.43-prealpha...v0.0.44-prealpha
[0.0.43-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.42-prealpha...v0.0.43-prealpha
[0.0.42-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.41-prealpha...v0.0.42-prealpha
[0.0.41-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.40-prealpha...v0.0.41-prealpha
[0.0.40-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.39-prealpha...v0.0.40-prealpha
[0.0.39-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.38-prealpha...v0.0.39-prealpha
[0.0.38-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.37-prealpha...v0.0.38-prealpha
[0.0.37-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.36-prealpha...v0.0.37-prealpha
[0.0.36-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.35-prealpha...v0.0.36-prealpha
[0.0.35-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.34-prealpha...v0.0.35-prealpha
[0.0.34-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.33-prealpha...v0.0.34-prealpha
[0.0.33-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.32-prealpha...v0.0.33-prealpha
[0.0.32-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.31-prealpha...v0.0.32-prealpha
[0.0.31-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.29-prealpha...v0.0.31-prealpha
[0.0.29-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.28-prealpha...v0.0.29-prealpha
[0.0.28-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.27-prealpha...v0.0.28-prealpha
[0.0.27-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.26-prealpha...v0.0.27-prealpha
[0.0.26-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.25-prealpha...v0.0.26-prealpha
[0.0.25-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.24-prealpha...v0.0.25-prealpha
[0.0.24-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.23-prealpha...v0.0.24-prealpha
[0.0.23-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.22-prealpha...v0.0.23-prealpha
[0.0.22-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.21-prealpha...v0.0.22-prealpha
[0.0.21-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.20-prealpha...v0.0.21-prealpha
[0.0.20-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.19-prealpha...v0.0.20-prealpha
[0.0.19-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.18-prealpha...v0.0.19-prealpha
[0.0.18-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.17-prealpha...v0.0.18-prealpha
[0.0.17-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.16-prealpha...v0.0.17-prealpha
[0.0.16-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.15-prealpha...v0.0.16-prealpha
[0.0.15-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.14-prealpha...v0.0.15-prealpha
[0.0.14-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.13-prealpha...v0.0.14-prealpha
[0.0.13-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.12-prealpha...v0.0.13-prealpha
[0.0.12-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.11-prealpha...v0.0.12-prealpha
[0.0.11-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.10-prealpha...v0.0.11-prealpha
[0.0.10-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.9-prealpha...v0.0.10-prealpha
[0.0.9-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.8-prealpha...v0.0.9-prealpha
[0.0.8-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.7-prealpha...v0.0.8-prealpha
[0.0.7-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.6-prealpha...v0.0.7-prealpha
[0.0.6-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.5-prealpha...v0.0.6-prealpha
[0.0.5-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.4-prealpha...v0.0.5-prealpha
[0.0.4-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.3-prealpha...v0.0.4-prealpha
[0.0.3-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.1-prealpha...v0.0.3-prealpha
[0.0.1-prealpha]: https://github.com/jasongdove/ErsatzTV/releases/tag/v0.0.1-prealpha