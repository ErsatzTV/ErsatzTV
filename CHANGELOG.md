# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [0.8.2-beta] - 2023-09-14
### Added
- Automatically rebuild search index after improper shutdown
- Add *experimental* support for Elasticsearch as search index backend
  - No query changes should be needed since ES is backed by lucene and supports the same query syntax
  - This can be configured using the following env vars (note the double underscore separator `__`)
    - `ELASTICSEARCH__URI` (e.g. `http://localhost:9200`)
    - `ELASTICSEARCH__INDEXNAME` (default is `ersatztv`)
- Add *experimental* support for MySQL/MariaDB database provider
  - ***There is no functionality to migrate data between providers***
  - This can be configured using the following env vars (note the double underscore separator `__`)
    - `PROVIDER` - set to `MySql`
    - `MYSQL__CONNECTIONSTRING` - (e.g. `Server=localhost;Database=ErsatzTV;Uid=root;Pwd=ersatztv;`)
- Add option to use shared Plex servers, not just owned servers
  - This can be enabled by setting the env var `ETV_ALLOW_SHARED_PLEX_SERVERS` to any non-empty value
- Show Plex server names in Libraries page

### Fixed
- Fix subtitle scaling when using QSV hardware acceleration
- Fix log viewer crash when log file contains invalid data
- Clean channel guide cache on startup (delete channels that no longer exist)
- Fix Emby movie libraries so local file access is not required
- Fix adding alternate schedule
- Fix parsing show title from NFO file that also contains season information

### Changed
- Optimize transcoding session to only work ahead (at max speed) for 3 minutes before throttling to realtime
  - This should *greatly* reduce cpu/gpu use when joining a channel, particularly with long content
- Allow manually editing (typing) schedule item fixed start time
- Use different control for editing schedule item duration, and allow 24-hour duration
  - This is needed if you want a default/fallback alternate schedule to fill the entire day with one schedule item
  - The schedule item should have a fixed start time of midnight (00:00) and a duration of 24 hours
- Use Direct3D 11 for QSV acceleration on Windows

## [0.8.1-beta] - 2023-08-07
### Added
- Add custom resolution management to `Settings` page

### Fixed
- Only allow a single instance of ErsatzTV to run
  - This fixes some cases where the search index would become unusable
- Fix VAAPI rate control mode capability check 

### Changed
- Rework startup process to show UI as early as possible
  - A minimal UI will indicate when the database and search index are initializing
  - The UI will automatically refresh when the initialization processes have completed
- Force ffmpeg to use one thread when hardware acceleration is used since hardware acceleration does not support multiple threads

## [0.8.0-beta] - 2023-06-23
### Added
- Disable playout buttons and show spinning indicator when a playout is being modified (built/extended, or subtitles are being extracted)
- Automatically reload playout details table when playout build is complete
- Add `Discard To Fill Attempts` setting to duration playout mode
  - This setting only has an effect when it's configured to be greater than zero and when using `Shuffle` or `Random` playback order
  - When the current item is longer than the remaining duration, it will be discarded and ETV will try to fit the next item in the collection, up to the configured number of times
  - When the remaining duration is shorter than all items in the collection, the normal filler logic will be used
- Add `Finish` column to playout detail table

### Fixed
- Skip checking for subtitles to extract when subtitles are not enabled on a channel/schedule item
- Properly scale subtitles when using hardware acceleration
- Fix color normalization of content with missing color metadata when using NVIDIA acceleration
- `VAAPI`: explicitly use `CQP` rate control mode when it's the only compatible mode
- Fix scaling anamorphic Emby content that Emby claims is not anamorphic

### Changed
- `HLS Direct` streaming mode
    - Use `MPEG-TS` container/output format by default to maintain v0.7.8 compatibility
      - `MP4` and `MKV` container/output format can still be configured in `Settings`
    - Improve `MP4` compatibility with certain content
- For `Pad` and `Duration` filler - prioritize filling the configured pad/duration
  - This will skip filler that is too long in an attempt to avoid unscheduled time
  - You may see the same filler more often, which means you may want to add more filler to your library so ETV has more options
- Update ffmpeg, libraries and drivers in all docker images

## [0.7.9-beta] - 2023-06-10
### Added
- Synchronize actor metadata from Jellyfin and Emby television libraries
  - New libraries and new episodes will get actor data automatically
  - Existing libraries can deep scan (one time) to retrieve actor data for existing episodes
- `HLS Direct` streaming mode
    - Use `MP4` container/output format by default, with new global option to use `MKV` container/output format
    - `MP4` output format: stream copy dvd subtitles
    - `MKV` output format: stream copy any embedded subtitles

### Fixed
- Fix extracting embedded text subtitles that had been incompletely extracted in the past 
- Fix fallback filler looping by forcing software mode for this content
  - Other content will still use hardware acceleration as configured
  - Hardware-accelerated fallback filler may be re-enabled in the future
- Fix playout building when shuffle in order is used with a single media item
- Fix pgs subtitle burn in from media server libraries
- Fix subtitle and watermark overlays with RadeonSI VAAPI driver
- Fix NVIDIA pipeline to use hardware-accelerated decoder with 8-bit h264 content   

### Changed
- Timeout playout builds after 2 minutes; this should prevent playout bugs from blocking other functionality

## [0.7.8-beta] - 2023-04-29
### Added
- Add `Season, Episode` playback order
  - This is currently *only* available when a show is added directly to a schedule
  - This will ignore release date and sort exclusively by season number and then by episode number
- Add `Show Media Info` button to movie and episode detail pages for troubleshooting

### Fixed
- Limit `HLS Direct` streams to realtime speed
- Fix `Reset Playout` button to use worker thread instead of UI thread
  - This fixes potential UI hangs and database concurrency bugs
- Maintain watermark alpha channel (built-in transparency) using QSV acceleration
- Properly extract and burn in embedded text subtitles using Jellyfin, Emby and Plex libraries
- Fix bug where deleting a channel would not remove its data from XMLTV
- Fix colorspace filter for some files with invalid color metadata
- Fix playback of external subtitles on Windows
- Fix vobsub subtitle burn in from media server libraries

### Changed
- Remove duplicate items from smart collections before scheduling
  - i.e. shows no longer need to be filtered out if search results also include episodes
  - Certain multi-collection scenarios may still include duplicates across multiple collections
- Use autocomplete fields for collection searching in schedule items editor
  - This greatly improves the editor performance
- Ignore dot underscore files

## [0.7.7-beta] - 2023-04-07
### Added
- Use `plot` field from Other Video NFO metadata as XMLTV description
- Add detailed warning log when a file is added to ErsatzTV more than once

### Fixed
- Fix updating (re-adding) Trakt lists to properly use new metadata ids that were not present when originally added
- Fix local show library scanning with non-english season folder names, e.g. `Staffel 02`
- Fix bug where local libraries would merge with media server libraries when the same file was added to both libraries
- Fix transcoding some 10-bit content from media servers using VAAPI acceleration
- Fix decoding of MPEG-4 Part 2 (e.g. DivX) content using NVIDIA acceleration
- Fix color normalization from `bt470bg` to `bt709` using QSV acceleration
- Fix adding files to search index with unknown video codec
- Fix subtitle burn-in (embedded or external) using Jellyfin, Emby and Plex libraries
    - **This requires a one-time full library scan, which may take a long time with large libraries.**

### Changed
- Use Poster artwork for XMLTV if available
  - If Poster artwork is unavailable, use Thumbnail
- Improve XMLTV response time by caching data as playouts are updated 

## [0.7.6-beta] - 2023-03-24
### Added
- Add `Troubleshooting` page with aggregated settings/hardware accel info for easy reference
- Read `director` fields from music video NFO metadata
- Pass `directors` and `studios` to music video credit templates
- Add optional JSON Web Token (JWT) query string auth for streaming endpoints (everything under `/iptv`)
  - This can be configured using the following env var (note the double underscore separator `__`)
    - `JWT__ISSUERSIGNINGKEY`
  - When configured, a JWT signed with the configured signing key is required to be passed in the query string as `access_token`, for example:
    - `http://localhost:8409/iptv/channels.m3u?access_token=ABCDEF`
    - `http://localhost:8409/iptv/xmltv.xml?access_token=ABCDEF`
  - When channels are retrieved this way, the access token will automatically be passed through to all necessary urls
  - Note that ONLY the `/iptv` endpoints will require auth when JWT is configured

### Fixed
- Fix scaling anamorphic content from non-local libraries
- Fix direct streaming content from Jellyfin that has external subtitles
  - Note that these subtitles are not currently supported in ETV, but they did cause a playback issue
- Fix Jellyfin, Emby and Plex library scans that wouldn't work in certain timezones
- Fix song normalization to match FFmpeg Profile bit depth
- Fix bug playing some external subtitle files (e.g. with an apostrophe in the file name)
- Fix bug detecting VAAPI capabilities when no device is selected in active FFmpeg Profile
- Fix playout mode duration bugs in XMLTV
  - Tail mode filler will properly include filler duration in XMLTV
  - Duration that wraps across midnight will no longer have overlapping items in XMLTV
- Maintain collection progress across all alternate schedules on a playout
- Fix color normalization from `bt470bg` to `bt709`

### Changed
- Ignore case of video and audio file extensions in local folder scanner
  - For example, the scanner will now find `movie.MKV` as well as `movie.mkv` on case-sensitive filesystems
- Include multiple `display-name` entries in generated XMLTV
  - Plex should now display the channel number instead of the channel id (e.g. `1.2` instead of `1.2.etv`)
- Rework concurrency a bit
  - Playout builds are no longer blocked by library scans
  - Adding Trakt lists is no longer blocked by library scans
  - All library scans (local and media servers) run sequentially
- Emby collection scanning will no longer happen after every (automatic or forced) library scan
  - Automatic/periodic scans will check collections one time after all libraries have been scanned
  - There is a new table in the `Media` > `Libraries` page with a button to manually re-scan Emby collections as needed
- For performance reasons, limit console log output to errors on Windows
  - Other platforms are unchanged
  - Log file behavior is unchanged

## [0.7.5-beta] - 2023-03-05
### Added
- Use AV1 hardware-accelerated decoder with VAAPI, QSV, NVIDIA when available
- Use VP9 hardware-accelerated decoder with VAAPI when available

### Fixed
- Align default docker image (no acceleration) with new images from [ErsatzTV-ffmpeg](https://github.com/ErsatzTV/ErsatzTV-ffmpeg)
- Fix some transcoding pipelines that use software decoders
- Improve VAAPI encoder capability detection on newer hardware
- Fix trash page to properly display episodes with missing metadata or titles
- Fix playback of content with yuv444p10le pixel format
- Fix case where some multi-episode files from Plex would crash the scanner

### Changed
- Upgrade all docker images and windows builds to ffmpeg 6.0
- Plex, Jellyfin and Emby libraries now retrieve all metadata and statistics from the media server
  - File systems will no longer be periodically scanned for libraries using these media sources
- Plex, Jellyfin and Emby libraries now direct stream content when files are not found on ErsatzTV's file system
  - Content will still be normalized according to the Channel and FFmpeg Profile settings
  - Streaming from disk is preferred, so every playback attempt will first check the local file system
- Use libvpl instead of libmfx to provide intel acceleration in vaapi docker images
- Search queries no longer remove duplicate results as this was causing incorrect behavior
- Prioritize audio streams that are flagged as "default" over number of audio channels
    - For example, a video with a stereo commentary track and a mono "default" track will now prefer the "default" track
- Support many more season folder names with local television libraries

## [0.7.4-beta] - 2023-02-12
### Added
- Add button to copy/clone schedule from schedules table
- Synchronize episode tags and genres from Jellyfin, Emby and Local show libraries
- Add `Deep Scan` button to Jellyfin and Emby libraries
  - This is now required to update some metadata for existing libraries, when targeted updates are not possible
  - For example, if you already have tags and genres on your episodes in Jellyfin or Emby, you will need to deep scan each library to update that metadata on existing items in ErsatzTV

### Fixed
- Fix many QSV pipeline bugs
- Fix MPEG2 video format with QSV and VAAPI acceleration
- Fix playback of content with undefined colorspace
- Fix NVIDIA color normalization with VP9 sources
- Fix fallback filler looping
- Fix bug where some libraries would never scan
- Fix filler ordering so post-roll is properly scheduled after padded mid-roll
- Fix pre/post-roll filler padding when used with mid-roll
  - This caused overlapping schedule items, fallback filler that was too long, etc.

### Changed
- Merge generated `Other Video` folder tags with tags from sidecar NFO
- Prioritize audio streams that are flagged as "default" when multiple candidate streams are available
  - For example, a video with a stereo commentary track and a stereo "default" track will now prefer the "default" track 

## [0.7.3-beta] - 2023-01-25
### Added
- Attempt to release memory periodically
- Add OpenID Connect (OIDC) support (e.g. Keycloak, Authelia, Auth0)
  - This only protects the management UI; all streaming endpoints will continue to allow anonymous access 
  - This can be configured with the following env vars (note the double underscore separator `__`)
    - `OIDC__AUTHORITY`
    - `OIDC__CLIENTID`
    - `OIDC__CLIENTSECRET`
    - `OIDC__LOGOUTURI` (optional, needed for Auth0, use `https://{auth0-domain}/v2/logout?client_id={auth0-client-id}` with proper values for domain and client-id)
- Add *experimental* alternate schedule system
  - This allows a single playout to dynamically select a schedule based on date criteria, for example:
    - Weekday vs weekend schedules
    - Summer vs fall schedules
    - Shark week schedules
  - Alternate schedules can be managed by clicking the calendar icon in the playout list
  - Playouts contain a prioritized (top to bottom) list of alternate schedules
  - Whenever a playout is built for a given day, ErsatzTV will check for a matching schedule from top to bottom
  - A given day must match all alternate schedule parameters; wildcards (`*any*`) will always match
    - Day of week
    - Day of month
    - Month
  - The lowest priority (bottom) item will always match all parameters, and can be considered a "default" or "fallback" schedule 

### Fixed
- Fix schedule editor crashing due to bad music video artist data
- Fix bug where playouts would not maintain smart collection progress on schedules that use multiple smart collections
- Fix library scanning on osx-arm64
- Fix ability to remove some media server libraries from ErsatzTV

### Changed
- Always use software pipeline for error display
  - This ensures errors will display even when hardware acceleration is misconfigured
- Call scanner process only when scanning is required based on library refresh interval
- Use lower process priority for scanner process with unforced (automatic) library scans
- Disable V2 UI and APIs by default
  - V2 UI can be re-enabled by setting the env var `ETV_UI_V2` to any value

## [0.7.2-beta] - 2023-01-05
### Fixed
- Fix VAAPI encoding in docker by switching to non-free driver

### Changed
- Rewrite log page to read directly from log files instead of sqlite

## [0.7.1-beta] - 2023-01-03
### Added
- Add new music video credit templates

### Fixed
- Fix many transcoding failures caused by the colorspace filter
- Fix song playback with VAAPI and NVENC
- Fix edge case where some local movies would not automatically be restored from trash
- Fix synchronizing Jellyfin and Emby collection items
- Fix saving some external subtitle records to database 

### Changed
- Upgrade to dotnet 7
- Upgrade all docker images to ubuntu jammy and ffmpeg 5.1.2
- Limit library scan interval between 0 and 1,000,000
  - 0 means do not automatically scan libraries
  - 1 to 999,999 means scan if it has been that many hours since the last scan
- Use new `ErsatzTV.Scanner` process for scanning all libraries
  - This should reduce the ongoing memory footprint

## [0.7.0-beta] - 2022-12-11
### Fixed
- Fix removing Jellyfin and Emby libraries that have been deleted from the source media server
- Fix `Work-Ahead HLS Segmenter Limit` setting to properly limit number of channels that can work-ahead at once
- Include base path value in generated channel playlist (M3U) and channel guide (XMLTV) links
- Fix parsing song metadata from OGG audio files
- Properly unlock/re-enable trakt list operations after an operation is canceled

### Added
- Add (required) bit depth normalization option to ffmpeg profile
  - This can help if your card only supports e.g. h264 encoding, normalizing to 8 bits will allow the hardware encoder to be used
- Extract font attachments after extracting text subtitles
  - This should improve SubStation Alpha subtitle rendering
- Detect VAAPI capabilities and fallback to software decoding/encoding as needed
- Add audio stream selector scripts for episodes and movies
  - This will let you customize which audio stream is selected for playback
  - Episodes are passed the following data:
    - `channelNumber`
    - `channelName`
    - `showTitle`
    - `showGuids`: array of string ids like `imdb_1234` or `tvdb_1234`
    - `seasonNumber`
    - `episodeNumber`
    - `episodeGuids`: array of string ids like `imdb_1234` or `tvdb_1234`
    - `preferredLanguageCodes`: array of string preferred language codes configured for the channel
    - `audioStreams`: array of audio stream data, each containing
      - `index`: the stream's index number, this is what the function needs to return
      - `channels`: the number of audio channels
      - `codec`: the audio codec
      - `isDefault`: bool indicating whether the stream is flagged as default
      - `isForced`: bool indicating whether the stream is flagged as forced
      - `language`: the stream's language
      - `title`: the stream's title
  - Movies are passed the following data:
      - `channelNumber`
      - `channelName`
      - `title`
      - `guids`: array of string ids like `imdb_1234` or `tvdb_1234`
    - `preferredLanguageCodes`: array of string preferred language codes configured for the channel
    - `audioStreams`: array of audio stream data, each containing
        - `index`: the stream's index number, this is what the function needs to return
        - `channels`: the number of audio channels
        - `codec`: the audio codec
        - `isDefault`: bool indicating whether the stream is flagged as default
        - `isForced`: bool indicating whether the stream is flagged as forced
        - `language`: the stream's language
        - `title`: the stream's title
- Add new fields to search index
  - `video_codec`: the video codec
  - `video_bit_depth`: the number of bits in the video stream's pixel format, e.g. 8 or 10
  - `video_dynamic_range`: the video's dynamic range, either `sdr` or `hdr`

### Changed
- Change `Multi-Episode Shuffle` scripting system to use Javascript instead of Lua

## [0.6.9-beta] - 2022-10-21
### Fixed
- Fix bug where tail or fallback filler would sometimes schedule much longer than expected
  - This only happened with fixed start schedule items following a schedule item with tail or fallback filler
- Fix NFO reader bug that caused inaccurate warning messages about invalid XML and incomplete metadata
- Fix reverse proxy SSL termination support by supporting `X-Forwarded-Proto` header
- Fix automatic playout reset scheduling
  - Playouts would reset every 30 minutes between midnight and the configured time, instead of only at the configured time
- XMLTV: properly group schedule items with `Custom Title` followed by item(s) with `Guide Mode` set to `Filler`

### Added
- Add music video credits template system
  - Templates are selected in each channel's settings 
  - Templates should be copied from `_default.ass.sbntxt` which is located in the config subfolder `templates/music-video-credits`
    - Copy the file, give it any name ending with `.ass.sbntext`, and only make edits to the copied file
  - The default template will be extracted and overwritten every time ErsatzTV is started
  - The template is an [Advanced SubStation Alpha](http://www.tcax.org/docs/ass-specs.htm) file using [scribian](https://github.com/scriban/scriban/tree/master/doc) template syntax
  - The following fields are available for use in the template:
    - `resolution`: the ffmpeg profile's resolution, which is used for margin calculations
    - `title`: the title of the music video
    - `track`: the music video's track number
    - `album`: the music video's album
    - `plot`: the music video's plot
    - `release_date`: the music video's release date
    - `artist`: the music videos artist (the parent folder)
    - `all_artists`: a list of additional artists from the music video's sidecar NFO metadata file
    - `duration`: the timespan duration of the music video, which can be used to calculate timing of additional subtitles
    - `stream_seek`: the timespan that ffmpeg will seek into the media item before beginning playback
- Add `Multi-Episode Shuffle` playout order for `Television Show` schedule items
  - The purpose of this playout order is to improve randomization for shows that normally have intro, multiple episodes, and outro
  - This playout order requires splitting the parts into individual files (e.g. splitting `s01e01-03.mkv` into `s01e01.mkv`, `s01e02.mkv` and `s01e03.mkv`)
  - This playout order requires a lua script in the config subfolder `scripts/multi-episode-shuffle`
  - The lua script should be named for the television show's guid, e.g. `tvdb_12345.lua` or `imdb_tt123456789.lua`
  - The script defines the number of parts that each un-split file typically contains
  - The script also defines a function to map each episode to a part number (or no part number i.e. `nil` if an episode has not been split)
  - All groups of part numbers (i.e. all part 1s, all part 2s) will be shuffled
  - The playout order will then schedule a random part 1 followed by a random part 2, etc
    - Un-split (`nil`) episodes will be randomly placed between re-combined parts (e.g. part1, part2, part3, un-split, part1, part2, part3)
- Add `ETV_BASE_URL` environment variable to support reverse proxies that use paths (e.g. `/ersatztv`)

### Changed
- No longer place watermarks within content by default (e.g. within 4:3 content padded to a 16:9 resolution)
  - This can be re-enabled if desired using the `Place Within Source Content` checkbox in watermark settings

## [0.6.8-beta] - 2022-10-05
### Fixed
- Fix typo introduced in `0.6.7-beta` that stopped QSV HEVC encoder from working
- Fix scaling logic for `Nvidia` acceleration and software mode
- Attempt to position watermarks within content (not over added black padding)
- Fix search results for `Other Videos` when NFO metadata is used
- Properly synchronize tags from Emby movies and shows
- Properly sync updated file paths from Plex
- Fix numeric range search queries (e.g. `minutes:[5 TO 10]`, `minutes:[* TO 3]`)

### Added
- Add `QSV Device` option to ffmpeg profile on linux
- Add guids to search index (e.g. `imdb:tt000000`, `tvdb:12345`)

## [0.6.7-beta] - 2022-09-05
### Fixed
- When all audio streams are selected with `HLS Direct`, explicitly copy them without transcoding
  - This only happens when the channel does not have a `Preferred Audio Language`
- Fix scanner crash caused by invalid mtime
- `VAAPI`: Downgrade libva from 2.15 to 2.14
- Fix bug with XMLTV that caused some filler to display with primary content details
- Multiple fixes for content scaling with `Nvidia`, `Qsv` and `Vaapi` accelerations
- Properly scale image-based subtitles
- Fix bug where a schedule containing a single item (fixed start and flood) would never finish building a playout
  - Logic was also added to detect infinite playout build loops in the future and stop them
- Fix bug where `Other Videos` wouldn't be included in scheduling mode `Shuffle In Order`

### Added
- Add `Preferred Audio Title` feature
    - Preference can be configured in channel settings and overridden on schedule items
    - When a title is specified, audio streams that contain that title (case-insensitive search) will be prioritized
    - This can be helpful for creating channels that use commentary tracks
    - External tooling exists to easily update title/name metadata if your audio streams don't already have this metadata
- Add `Amf` hardware acceleration option for AMD GPUs on Windows
- Add `QSV Extra Hardware Frames` parameter for tuning QSV acceleration
  - Performance may improve on some systems after doubling or halving the default value of `64`

## [0.6.6-beta] - 2022-08-17
### Fixed
- Use MIME Type `application/x-mpegurl` for all playlists instead of `application/vnd.apple.mpegurl`
- Replace `setsar` filter with `setdar` filter
  - `setsar` caused issues scaling between two different aspect ratios
    - For example, some 4:3 content would appear stretched when scaled to a 16:9 resolution
  - `setdar` is now only used when aspect ratios match 
- Prioritize aspect ratio from container when video stream contains conflicting aspect ratio
  - This is usually caused by bad authoring, but the change should improve scaling behavior for edge cases

### Added
- Support DSD audio file formats (DFF and DSF) in local song libraries
- Support OGG audio file formats (OGG, OPUS, OGA, OGX, SPX) in local song libraries

### Changed
- Always return playlist after a maximum of 8 seconds while starting up an HLS Segmenter session
- Use multi-variant playlists instead of redirects for HLS Segmenter sessions
- Upgrade ffmpeg from 5.0 to 5.1 in most docker images (not ARM variants)
    - Upgrading from 5.0 to 5.1 is also recommended for other installations (Windows, Linux)

## [0.6.5-beta] - 2022-08-02
### Fixed
- Fix database initializer; fresh installs with v0.6.4-beta are missing some config data and should upgrade

## [0.6.4-beta] - 2022-07-28
### Fixed
- Fix subtitle stream selection when subtitle language is different than audio language
- Fix bug with unsupported AAC channel layouts
- Fix NVIDIA second-gen maxwell capabilities detection
- Return distinct search results for episodes and other videos that have the same title
  - For example, two other videos both named `Trailer` would previously have displayed as one item in search results
- Fix schedules that would begin to repeat the same content in the same order after a couple of days

### Added
- Add `640x480` resolution

## [0.6.3-beta] - 2022-07-04
### Fixed
- Maintain stream continuity when playout is rebuilt for a channel that is actively being streamed
- Properly apply changes to episode title, sort title, outline and plot from Plex
- Fix search index for other videos and songs
  - In previous versions, some libraries would incorrectly display only one item
- Properly display old versions of renamed items in trash

### Added
- Add `Minimum Log Level` option to `Settings` page
  - Other methods of configuring the log level will no longer work

## [0.6.2-beta] - 2022-06-18
### Fixed
- Fix content repeating for up to a minute near the top of every hour
- Check whether hardware-accelerated hevc codecs are supported by the NVIDIA card
  - Software codecs will be used if they are unsupported by the NVIDIA card
- Fix sorting of channel contents in EPG
- Fix Jellyfin admin user id sync
  - Ignore disabled admins and admins who do not have access to all libraries

### Added
- Add 32-bit `arm` docker tags (`develop-arm` and `latest-arm`)

### Changed
- Regularly delete old segments from transcode folder while content is actively transcoding
  - This should help reduce required disk space
  - To further minimize required disk space, set `Work-Ahead HLS Segmenter Limit` to `0` in `Settings`

## [0.6.1-beta] - 2022-06-03
### Fixed
- Fix Jellyfin show library paging
- Properly locate and identify multiple Plex servers
- Properly restore `Unavailable`/`File Not Found` items when they are located on disk

### Added
- Add basic music video credits subtitle generation
  - This can be enabled in channel settings

## [0.6.0-beta] - 2022-06-01
### Fixed
- Additional fix for duplicate `Other Videos` entries; trash may need to be emptied one last time after upgrading
- Fix watermark opacity in cultures where `,` is a decimal separator
- Rework playlist filtering to avoid empty playlist responses
- Fix some QSV/VAAPI memory errors by always requesting 64 extra hardware frames

### Added
- Enable QSV hardware acceleration for vaapi docker images

### Changed
- Use paging to synchronize all media from Plex, Jellyfin and Emby
  - This will reduce memory use and improve reliability of synchronizing large libraries
- Disable low power mode for `h264_qsv` and `hevc_qsv` encoders

## [0.5.8-beta] - 2022-05-20
### Fixed
- Fix error display with `HLS Segmenter` and `MPEG-TS` streaming modes
- Remove erroneous log messages about normalizing framerate on channels where framerate normalization is disabled
- Fix unscheduled filler gaps that sometimes happen as playouts are automatically extended each hour

### Added
- Clean transcode cache folder on startup and after `HLS Segmenter` session terminates for any reason

### Changed
- Remove thread limitation for scenarios where it is not required
  - This should give a performance boost to installations that don't use hardware acceleration
- Use hardware acceleration to display error messages where configured

## [0.5.7-beta] - 2022-05-14
### Fixed
- Reduce memory use due to library scan operations
- Fix some instances of filler getting "stuck" when a filler item is encountered that's too long for the gap
- Properly ignore Plex `Other Videos` libraries (`movie` libraries where agent is `com.plexapp.agents.none`)
- Fix `Custom Title` for schedule items with `One`, `Multiple` and `Flood` playout modes 
- Fix scheduling bug where flood items would sometimes fail to continue after midnight

### Added
- Add `metadata_kind` field to search index to allow searching for items with a particular metdata source
  - Valid metadata kinds are `fallback`, `sidecar` (NFO), `external` (from a media server) and `embedded` (songs)
- Add autocomplete functionality to search bar to quickly navigate to channels, ffmpeg profiles, collections and schedules by name
- Add global setting to skip missing (file-not-found or unavailable) items when building playouts
- Add filler preset option to allow watermarks to overlay on top of filler (disabled by default)
  - This option is applied when new items are added to a playout; rebuilding is needed if you want the change to take effect immediately
- Read `track` field from music video NFO metadata and use it for chronological sorting (after release date)
- Add `Random Start Point` option to schedules
  - When this option is enabled, all `Chronological` or `Shuffle In Order` content groups will have their start points randomized
  - When this option is disabled, all `Chronological` or `Shuffle In Order` content groups will start with the chronologically earliest item

### Changed
- Replace invalid (control) characters in NFO metadata with replacement character `�` before parsing
- Store partial (incomplete) NFO metadata results when invalid XML is encountered
  - Previously, no metadata would be stored if the XML within the NFO failed to validate

## [0.5.6-beta] - 2022-05-06
### Fixed
- Fix processing local movie NFO metadata without a `year` value
- Fix processing local movie fallback metadata
- Fix search edge case where very recently added items (hours) would not be returned by relative date queries
- Fix search index validation on startup; improper validation was causing a rebuild with every startup
- Block library scanning until search index has been recreated/upgraded
- Fix occasional erroneous log messages when HLS channel playback times out because all clients have left
- Fix fallback filler playback
- Fix stream continuity when error messages are displayed
- Fix duplicate scanning within `Other Video` libraries (i.e. folders would be scanned multiple times) 

### Added
- Add `show_genre` and `show_tag` to search index for seasons and episodes
- Use `aired` value to source release date from music video nfo metadata
- Add NFO metadata support to `Other Video` libraries
  - `Other Video` NFO metadata must be in the movie NFO metadata format

## [0.5.5-beta] - 2022-05-03
### Fixed
- Fix adding episodes with no title to the search index
  - This behavior was preventing some items from being removed from the trash
- Support combination NFO metadata for movies, shows, artists and music videos
  - Note that ErsatzTV does not scrape any metadata; any URLs after the XML will be ignored
- Fix bug causing some Jellyfin and Emby content to incorrectly show as unavailable
- Fix extracting embedded `mov_text` subtitles
- Properly extract embedded subtitles on playouts where subtitles are only enabled on schedule items (and not on the channel itself)

### Added
- Add experimental `arm64` docker tags (`develop-arm64` and `latest-arm64`)
- Use `Sort Title` from Movie NFO metadata if available
- Support multiple `Artist` entries in music video NFO metadata

## [0.5.4-beta] - 2022-04-29
### Fixed
- Cleanly stop all library scans when service termination is requested
- Fix health check crash when trash contains a show or a season
- Fix ability of health check crash to crash home page
- Remove and ignore Season 0/Specials from Plex shows that have no specials
- Automatically delete and rebuild the search index on startup if it has become corrupt
- Automatically scan Jellyfin and Emby libraries on startup and periodically
- Properly remove un-synchronized Plex, Jellyfin and Emby items from the database and search index
- Fix synchronizing movies within a collection from Jellyfin

### Changed
- Update Plex, Jellyfin and Emby movie and show library scanners to share a significant amount of code
  - This should help maintain feature parity going forward
- Optimize search-index rebuilding to complete 100x faster
- **No longer use network paths to source content from Jellyfin and Emby**
  - **If you previously used path replacements to convert network paths to local paths, you should remove them**

### Added
- Add `unavailable` state for Jellyfin and Emby movie and show libraries
- Add `height` and `width` to search index for all videos
- Add `season_number` and `episode_number` to search index for all episodes
- Add `season_number` to search index for seasons
- Add `show_title` to search index for seasons and episodes

## [0.5.3-beta] - 2022-04-24
### Fixed
- Cleanly stop Plex library scan when service termination is requested
- Fix bug introduced with 0.5.2-beta that prevented some Plex content from being played
- Fix spammy subtitle error message
- Fix generating blur hashes for song backgrounds in Docker

### Changed
- No longer remove Plex movies and episodes from ErsatzTV when they do not exist on disk
  - Instead, a new `unavailable` media state will be used to indicate this condition
  - After updating mounts, path replacements, etc - a library scan can be used to resolve this state

## [0.5.2-beta] - 2022-04-22
### Fixed
- Fix unlocking libraries when scanning fails for any reason
- Fix software overlay of actual size watermark

### Added
- Add support for burning in embedded and external text subtitles
  - **This requires a one-time full library scan, which may take a long time with large libraries.**
- Sync Plex, Jellyfin and Emby collections as tags on movies, shows, seasons and episodes
  - This allows smart collections that use queries like `tag:"Collection Name"`
  - Note that Emby has an outstanding collections bug that prevents updates when removing items from a collection
- Sync Plex labels as tags on movies and shows
  - This allows smart collections that use queries like `tag:"Plex Label Name"`
- Add `Deep Scan` button for Plex libraries
  - This scanning mode is *slow* but is required to detect some changes like labels

### Changed
- Improve the speed and change detection of the Plex library scanners

## [0.5.1-beta] - 2022-04-17
### Fixed
- Fix subtitles edge case with NVENC
- Only select picture subtitles (text subtitles are not yet supported)
  - Supported picture subtitles are `hdmv_pgs_subtitle` and `dvd_subtitle`
- Fix subtitles using software encoders, videotoolbox, VAAPI
- Fix setting VAAPI driver name
- Fix ffmpeg troubleshooting reports
- Fix bug where filler would behave as if it were configured to pad even though a different mode was selected
- Fix bug where mid-roll count filler would skip scheduling the final chapter in an episode 

### Added
- Add `Empty Trash` button to `Trash` page

## [0.5.0-beta] - 2022-04-13
### Fixed
- Fix `HLS Segmenter` bug where it would drift off of the schedule if a playout was changed while the segmenter was running
- Ensure clients that use HDHomeRun emulation (like Plex) always get an `MPEG-TS` stream, regardless of the configured streaming mode
- Fix scheduling bug that caused some days to be skipped when fixed start times were used with fallback filler

### Added
- Add `Preferred Subtitle Language` and `Subtitle Mode` to channel settings
  - `Preferred Subtitle Language` will filter all subtitle streams based on language
  - `Subtitle Mode` will further filter subtitle streams based on attributes (forced, default)
  - If picture-based subtitles are found after filtering, they will be burned into the video stream
- Detect non-zero ffmpeg exit code from `HLS Segmenter` and `MPEG-TS`, log error output and display error output on stream
- Add `Watermark` setting to schedule items; this allows override the channel watermark. Watermark priority is:
  - Schedule Item
  - Channel
  - Global

### Changed
- Remove legacy transcoder logic option; all channels will use the new transcoder logic
- Renamed channel setting `Preferred Language` to `Preferred Audio Language`
- Reworked playout build logic to maintain collection progress in some scenarios. There are now three build modes:
  - `Continue` - add new items to the end of an existing playout
    - This mode is used when playouts are automatically extended in the background
  - `Refresh` - this mode will try to maintain collection progress while rebuilding the entire playout
    - This mode is used when a schedule is updated, or when collection modifications trigger a playout rebuild
  - `Reset` - this mode will rebuild the entire playout and will NOT maintain progress
    - This mode is only used when the `Reset Playout` button is clicked on the Playouts page
  - **This requires rebuilding all playouts, which will happen on startup after upgrading**
- Use ffmpeg to resize images; this should help reduce ErsatzTV's memory use
- Use ffprobe to check for animated logos and watermarks; this should help reduce ErsatzTV's memory use
- Allow two decimals in channel numbers (e.g. `5.73`)

## [0.4.5-alpha] - 2022-03-29
### Fixed
- Fix streaming mode inconsistencies when `mode` parameter is unspecified
- Fix startup on Windows 7

### Added
- Add option to automatically deinterlace video when transcoding
  - Previously, this was always enabled; the purpose of the option is to allow disabling any deinterlace filters
  - Note that there is no performance gain to disabling the option with progressive content; filters are only ever applied to interlaced content

### Changed
- Change FFmpeg Profile video codec and audio codec text fields to select fields
  - The appropriate video encoder will be determined based on the video format and hardware acceleration selections
- Remove FFmpeg Profile `Transcode`, `Normalize Video` and `Normalize Audio` settings
  - All content will be transcoded and have audio and video normalized
  - The only exception to this rule is `HLS Direct` streaming mode, which directly copies video and audio streams
- Always try to connect to Plex at `http://localhost:32400` even if that address isn't advertised by the Plex API
  - If Plex isn't on the localhost, all other addresses will be checked as with previous releases

## [0.4.4-alpha] - 2022-03-10
### Fixed
- Fix `HLS Direct` streaming mode
- Fix bug with `HLS Segmenter` (and `MPEG-TS`) on Windows that caused errors at program boundaries

### Added
- Perform additional duration analysis on files with missing duration metadata
- Add `nouveau` VAAPI driver option

## [0.4.3-alpha] - 2022-03-05
### Fixed
- Fix song sorting with `Chronological` and `Shuffle In Order` playback orders
- Fix watermark on scaled and/or padded video with NVIDIA acceleration
- Fix playback of interlaced mpeg2video content with NVIDIA acceleration
- Fix playback of all interlaced content with QSV acceleration
- Fix adding songs to collections from search results page
- Fix bug scheduling mid-roll filler with content that contains one chapter
  - No mid-roll filler will be inserted for content with zero or one chapters
- Fix thread sync bug with `HLS Segmenter` (and `MPEG-TS`) streaming modes
- Fix path replacement bug when media server path is left blank

### Added
- Add automated error reporting via Bugsnag
  - This can be disabled by editing the `appsettings.json` file or by setting the `Bugsnag:Enable` environment variable to `false`
- Add `album_artist` to song metadata and to search index
- Display `album_artist` on some song videos when it's different than the `artist`

### Changed
- Framerate normalization will never normalize framerate below 24fps
  - Instead, content with a lower framerate will be normalized up to 24fps
- `Shuffle In Order` will group songs by album artist instead of by track artist

## [0.4.2-alpha] - 2022-02-26
### Fixed
- Add improved but experimental transcoder logic, which can be toggled on and off in `Settings`
- Fix `HLS Segmenter` bug when source video packet contains no duration (`N/A`)
- Fix green line at the bottom of some content scaled using QSV acceleration

### Added
- Add configurable channel group (M3U) and categories (XMLTV)
- Add `Shuffle Schedule Items` option to schedule configuration
  - When this is enabled, schedule items will be shuffled rather than looped in order
  - **To support this, all playouts will be rebuilt (one time) after upgrading to this version**

### Changed
- Disable framerate normalization by default and on all ffmpeg profiles
  - If framerate normalization is desired (not typically needed), it can be re-enabled manually
- Show watermarks over songs
- Hide unused local libraries

## [0.4.1-alpha] - 2022-02-10
### Fixed
- Normalize smart quotes in search queries as they are unsupported by the search library
- Fix incorrect watermark time calculations caused by working ahead in `HLS Segmenter`
- Fix ui crash adding empty path to local library
- Fix ui crash loading collection editor
- Properly flag items as `File Not Found` when local library path (folder) is missing from disk
- Fix playback bug with unknown pixel format
- Fix playback of interlaced mpeg2video on NVIDIA, VAAPI

### Added
- Include `Series` category tag for all episodes in XMLTV
- Include movie, episode (show), music video (artist) genres as `category` tags in XMLTV
- Add framerate normalization to `HLS Segmenter` and `MPEG-TS` streaming modes
- Add `HLS Segmenter Initial Segment Count` setting to allow segmenter to work ahead before allowing client playback

### Changed
- Intermittent watermarks will now fade in and out
- Show collection name in some playout build error messages
- Use hardware-accelerated filter for watermarks on NVIDIA
- Use hardware-accelerated deinterlace for some content on NVIDIA

## [0.4.0-alpha] - 2022-01-29
### Fixed
- Fix m3u `mode` query param to properly override streaming mode for all channels
  - `segmenter` for `HLS Segmenter`
  - `hls-direct` for `HLS Direct`
  - `ts` for `MPEG-TS`
  - `ts-legacy` for `MPEG-TS (Legacy)`
  - omitting the `mode` parameter returns each channel as configured
- Link `File Not Found` health check to `Trash` page to allow deletion
- Fix `HLS Segmenter` streaming mode with multiple ffmpeg-based clients
  - Jellyfin (web) and TiviMate (Android) were specifically tested

### Added
- Hide console window on macOS and Windows; tray menu can be used to access UI, logs and to stop the app
- Also write logs to text files in the `logs` config subfolder
- Add `added_date` to search index
    - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete
- Add `added_inthelast`, `added_notinthelast` search field for relative added date queries
    - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`

## [0.3.8-alpha] - 2022-01-23
### Fixed
- Fix issue preventing some versions of ffmpeg (usually 4.4.x) from streaming MPEG-TS (Legacy) channels at all
  - The issue appears to be caused by using a thread count other than `1`
  - Thread count is now forced to `1` for all streaming modes other than HLS Segmenter
- Fix bug with HLS Segmenter in cultures where `.` is a group/thousands separator
- Fix search results page crashing with some media kinds
- Always use MPEG-TS or MPEG-TS (Legacy) streaming mode with HDHR (Plex)
  - Other configured modes will fall back to MPEG-TS when accessed by Plex

### Changed
- Upgrade ffmpeg from 4.4 to 5.0 in all docker images
    - Upgrading from 4.4 to 5.0 is recommended for all installations

## [0.3.7-alpha] - 2022-01-17
### Fixed
- Fix local folder scanners to properly detect removed/re-added folders with unchanged contents
- Fix double-click startup on mac
- Fix trakt list sync when show does not contain a year
- Properly unlock libraries when a scan is unable to be performed because ffmpeg or ffprobe have not been found

### Added
- Add trash system for local libraries to maintain collection and schedule integrity through media share outages
  - When items are missing from disk, they will be flagged and present in the `Media` > `Trash` page
  - The trash page can be used to permanently remove missing items from the database
  - When items reappear at the expected location on disk, they will be unflagged and removed from the trash
- Add basic Mac hardware acceleration using VideoToolbox

### Changed
- Local libraries only: when items are missing from disk, they will be added to the trash and no longer removed from collections, etc.
- Show song thumbnail in song list

## [0.3.6-alpha] - 2022-01-10
### Fixed
- Properly index `minutes` field when adding new items during scan (vs when rebuilding index)
- Fix some nvenc edge cases where only padding is needed for normalization
- Properly overwrite environment variables for ffmpeg processes (`LIBVA_DRIVER_NAME`, `FFREPORT`)

### Added
- Add music video `artist` to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Remove `HLS Hybrid` streaming mode; all channels have been reconfigured to use the superior `HLS Segmenter` streaming mode
- Update `MPEG-TS` streaming mode to internally use the HLS segmenter
  - This improves compatibility with many clients and also improves performance at program boundaries
- Renamed existing `MPEG-TS` mode as `MPEG-TS (Legacy)`
  - This mode will be removed in a future release

## [0.3.5-alpha] - 2022-01-05
### Fixed
- Fix bundled ffmpeg version in base docker image (NOT nvidia or vaapi) which prevented playback since v0.3.0-alpha
- Use software decoding for mpeg4 content when VAAPI acceleration is enabled
- Fix hardware acceleration health check to recognize QSV on non-Windows platforms

### Changed
- Treat `setsar` as a hardware filter, avoiding unneeded `hwdownload` and `hwupload` steps when padding isn't required

## [0.3.4-alpha] - 2021-12-21
### Fixed
- Fix other video and song scanners to include videos contained directly in top-level folders that are added to a library
- Allow saving ffmpeg troubleshooting reports on Windows

## [0.3.3-alpha] - 2021-12-12
### Fixed
- Fix bug with saving multiple blurhash versions for cover art; all cover art will be automatically rescanned
- Fix song detail margin when no cover art exists and no watermark exists
- Fix synchronizing virtual shows and seasons from Jellyfin
- Properly sort channels in M3U

### Changed
- Use blurhash of ErsatzTV colors instead of solid colors for default song backgrounds
- Use select control instead of autocomplete control in many places
    - The autocomplete control is not intuitive to use and has focus bugs

## [0.3.2-alpha] - 2021-12-03
### Fixed
- Fix artwork upload on Windows
- Fix unicode song metadata on Windows
- Fix unicode console output on Windows
- Fix TV Show NFO metadata processing when `year` is missing
- Fix song detail outline to help legibility on white backgrounds
- Optimize song artwork scanning to prevent re-processing album artwork for each song

### Changed
- Use custom log database backend which should be more portable (i.e. work in osx-arm64)
- Use cover art blurhashes for song backgrounds instead of solid colors or box blur

## [0.3.1-alpha] - 2021-11-30
### Fixed
- Fix song page links in UI
- Show song artist in playout detail
- Include song artist and cover art in channel guide (xmltv)
- Use subtitles to display errors, which fixes many edge cases of unescaped characters
- Properly split song genre tags
- Properly display all songs that have an identical album and title
- Fix channel logo and watermark uploads
- Fix regression introduced with `v0.2.4-alpha` that caused some filler edge cases to crash the playout builder

### Added
- Add song genres to search index
- Use embedded song cover art when sidecar cover art is unavailable

### Changed
- Randomly place song cover art on left or right side of screen
- Randomly use a solid color from the cover art instead of blurred cover art for song background
- Randomly select song detail layout (large title/small artist or small artist/title/album)

## [0.3.0-alpha] - 2021-11-25
### Fixed
- Properly fix database incompatibility introduced with `v0.2.4-alpha` and partially fixed with `v0.2.5-alpha`
  - The proper fix requires rebuilding all playouts, which will happen on startup after upgrading
- Fix local library locking/progress display when adding paths
- Fix grouping duration items in EPG when custom title is configured

### Added
- Add *experimental* `Songs` local libraries
    - Like `Other Videos`, `Songs` require no metadata or particular folder layout, and will have tags added for each containing folder
    - For Example, a song at `rock/band/1990 - Album/01 whatever.flac` will have the tags `rock`, `band` and `1990 - Album`, and the title `01 whatever`
    - Songs will also have basic metadata read from embedded tags (album, artist, title)
    - Video will be automatically generated for songs using metadata and cover art or watermarks if available
- Add support for `.webm` video files

## [0.2.5-alpha] - 2021-11-21
### Fixed
- Include other video title in channel guide (xmltv)
- Fix bug introduced with 0.2.4-alpha that caused some playouts to build from year 0
- Use less memory matching Trakt list items

### Added
- Build osx-arm64 packages on release

### Changed
- No longer warn about local Plex guids; they aren't used for Trakt matching and can be ignored

## [0.2.4-alpha] - 2021-11-13
### Changed
- Upgrade to dotnet 6
- Use `scale_cuda` instead of `scale_npp` for NVIDIA scaling in all cases

## [0.2.3-alpha] - 2021-11-03
### Fixed
- Fix bug with audio filter in cultures where `.` is a group/thousands separator
- Fix bug where flood playout mode would only schedule one item
  - This would happen if the flood was followed by another flood with a fixed start time

### Added
- Support empty `.etvignore` file to instruct local movie scanner to ignore the containing folder

## [0.2.2-alpha] - 2021-10-30
### Fixed
- Fix EPG entries for Duration schedule items that play multiple items
- Fix EPG entries for Multiple schedule items that play more than one item

### Added
- Add fallback filler settings to Channel and global FFmpeg Settings
  - When streaming is attempted during an unscheduled gap, the resulting video will be determined using the following priority:
    - Channel fallback filler
    - Global fallback filler
    - Generated `Channel Is Offline` error message video 

### Changed
- Allow per-episode folders for local show libraries
  - e.g. `Show Name\Season #\Episode #\Show Name - s#e#.mkv`

## [0.2.1-alpha] - 2021-10-24
### Fixed
- Fix saving dynamic start time on schedule items

## [0.2.0-alpha] - 2021-10-23
### Fixed
- Fix generated streams with mpeg2video
- Fix incorrect row count in playout detail table
- Fix deleting movies that have been removed from Jellyfin and Emby
- Fix bug that caused large unscheduled gaps in playouts
  - This was caused by schedule items with a fixed start of midnight

### Added
- Add new filler system
    - `Pre-Roll Filler` plays before each media item
    - `Mid-Roll Filler` plays between media item chapters
    - `Post-Roll Filler` plays after each media item
    - `Tail Filler` plays after all media items, until the next media item
    - `Fallback Filler` loops instead of default offline image to fill any remaining gaps
- Store chapter details with media statistics; this is needed to support mid-roll filler
    - This requires re-ingesting statistics for all media items the first time this version is launched
- Add switch to show/hide filler in playout detail table
- Add `minutes` field to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Change some debug log messages to info so they show by default again
- Remove tail collection options from `Duration` playout mode
- Show localized start time in schedule items tables

## [0.1.5-alpha] - 2021-10-18
### Fixed
- Fix double scheduling; this could happen if the app was shutdown during a playout build
- Fix updating Jellyfin and Emby TV seasons
- Fix updating Jellyfin and Emby artwork
- Fix Plex, Jellyfin, Emby worker crash attempting to sync library that no longer exists
- Fix bug with `Duration` mode scheduling when media items are too long to fit in the requested duration

### Added
- Include music video thumbnails in channel guide (xmltv)

### Changed
- Automatically find working Plex address on startup
- Automatically select schedule item in schedules that contain only one item
- Change default log level from `Debug` to `Information`
  - The `Debug` log level can be enabled in the `appsettings.json` file for non-docker installs
  - The `Debug` log level can be enabled by setting the environment variable `Serilog:MinimumLevel=Debug` for docker installs

## [0.1.4-alpha] - 2021-10-14
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

## [0.1.3-alpha] - 2021-10-13
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


[Unreleased]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.8.2-beta...HEAD
[0.8.2-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.8.1-beta...v0.8.2-beta
[0.8.1-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.8.0-beta...v0.8.1-beta
[0.8.0-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.9-beta...v0.8.0-beta
[0.7.9-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.8-beta...v0.7.9-beta
[0.7.8-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.7-beta...v0.7.8-beta
[0.7.7-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.6-beta...v0.7.7-beta
[0.7.6-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.5-beta...v0.7.6-beta
[0.7.5-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.4-beta...v0.7.5-beta
[0.7.4-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.3-beta...v0.7.4-beta
[0.7.3-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.2-beta...v0.7.3-beta
[0.7.2-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.1-beta...v0.7.2-beta
[0.7.1-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.7.0-beta...v0.7.1-beta
[0.7.0-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.9-beta...v0.7.0-beta
[0.6.9-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.8-beta...v0.6.9-beta
[0.6.8-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.7-beta...v0.6.8-beta
[0.6.7-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.6-beta...v0.6.7-beta
[0.6.6-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.5-beta...v0.6.6-beta
[0.6.5-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.4-beta...v0.6.5-beta
[0.6.4-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.3-beta...v0.6.4-beta
[0.6.3-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.2-beta...v0.6.3-beta
[0.6.2-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.1-beta...v0.6.2-beta
[0.6.1-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.6.0-beta...v0.6.1-beta
[0.6.0-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.8-beta...v0.6.0-beta
[0.5.8-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.7-beta...v0.5.8-beta
[0.5.7-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.6-beta...v0.5.7-beta
[0.5.6-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.5-beta...v0.5.6-beta
[0.5.5-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.4-beta...v0.5.5-beta
[0.5.4-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.3-beta...v0.5.4-beta
[0.5.3-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.2-beta...v0.5.3-beta
[0.5.2-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.1-beta...v0.5.2-beta
[0.5.1-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.5.0-beta...v0.5.1-beta
[0.5.0-beta]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.5-alpha...v0.5.0-beta
[0.4.5-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.4-alpha...v0.4.5-alpha
[0.4.4-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.3-alpha...v0.4.4-alpha
[0.4.3-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.2-alpha...v0.4.3-alpha
[0.4.2-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.1-alpha...v0.4.2-alpha
[0.4.1-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.4.0-alpha...v0.4.1-alpha
[0.4.0-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.8-alpha...v0.4.0-alpha
[0.3.8-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.7-alpha...v0.3.8-alpha
[0.3.7-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.6-alpha...v0.3.7-alpha
[0.3.6-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.5-alpha...v0.3.6-alpha
[0.3.5-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.4-alpha...v0.3.5-alpha
[0.3.4-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.3-alpha...v0.3.4-alpha
[0.3.3-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.2-alpha...v0.3.3-alpha
[0.3.2-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.1-alpha...v0.3.2-alpha
[0.3.1-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.3.0-alpha...v0.3.1-alpha
[0.3.0-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.5-alpha...v0.3.0-alpha
[0.2.5-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.4-alpha...v0.2.5-alpha
[0.2.4-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.3-alpha...v0.2.4-alpha
[0.2.3-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.2-alpha...v0.2.3-alpha
[0.2.2-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.1-alpha...v0.2.2-alpha
[0.2.1-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.2.0-alpha...v0.2.1-alpha
[0.2.0-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.5-alpha...v0.2.0-alpha
[0.1.5-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.4-alpha...v0.1.5-alpha
[0.1.4-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.3-alpha...v0.1.4-alpha
[0.1.3-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.2-alpha...v0.1.3-alpha
[0.1.2-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.1-alpha...v0.1.2-alpha
[0.1.1-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.1.0-alpha...v0.1.1-alpha
[0.1.0-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.62-alpha...v0.1.0-alpha
[0.0.62-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.61-alpha...v0.0.62-alpha
[0.0.61-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.60-alpha...v0.0.61-alpha
[0.0.60-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.59-alpha...v0.0.60-alpha
[0.0.59-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.58-alpha...v0.0.59-alpha
[0.0.58-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.57-alpha...v0.0.58-alpha
[0.0.57-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.56-alpha...v0.0.57-alpha
[0.0.56-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.55-alpha...v0.0.56-alpha
[0.0.55-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.54-alpha...v0.0.55-alpha
[0.0.54-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.53-alpha...v0.0.54-alpha
[0.0.53-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.52-alpha...v0.0.53-alpha
[0.0.52-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.51-alpha...v0.0.52-alpha
[0.0.51-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.50-alpha...v0.0.51-alpha
[0.0.50-alpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.49-prealpha...v0.0.50-alpha
[0.0.49-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.48-prealpha...v0.0.49-prealpha
[0.0.48-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.47-prealpha...v0.0.48-prealpha
[0.0.47-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.46-prealpha...v0.0.47-prealpha
[0.0.46-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.45-prealpha...v0.0.46-prealpha
[0.0.45-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.44-prealpha...v0.0.45-prealpha
[0.0.44-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.43-prealpha...v0.0.44-prealpha
[0.0.43-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.42-prealpha...v0.0.43-prealpha
[0.0.42-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.41-prealpha...v0.0.42-prealpha
[0.0.41-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.40-prealpha...v0.0.41-prealpha
[0.0.40-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.39-prealpha...v0.0.40-prealpha
[0.0.39-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.38-prealpha...v0.0.39-prealpha
[0.0.38-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.37-prealpha...v0.0.38-prealpha
[0.0.37-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.36-prealpha...v0.0.37-prealpha
[0.0.36-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.35-prealpha...v0.0.36-prealpha
[0.0.35-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.34-prealpha...v0.0.35-prealpha
[0.0.34-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.33-prealpha...v0.0.34-prealpha
[0.0.33-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.32-prealpha...v0.0.33-prealpha
[0.0.32-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.31-prealpha...v0.0.32-prealpha
[0.0.31-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.29-prealpha...v0.0.31-prealpha
[0.0.29-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.28-prealpha...v0.0.29-prealpha
[0.0.28-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.27-prealpha...v0.0.28-prealpha
[0.0.27-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.26-prealpha...v0.0.27-prealpha
[0.0.26-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.25-prealpha...v0.0.26-prealpha
[0.0.25-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.24-prealpha...v0.0.25-prealpha
[0.0.24-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.23-prealpha...v0.0.24-prealpha
[0.0.23-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.22-prealpha...v0.0.23-prealpha
[0.0.22-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.21-prealpha...v0.0.22-prealpha
[0.0.21-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.20-prealpha...v0.0.21-prealpha
[0.0.20-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.19-prealpha...v0.0.20-prealpha
[0.0.19-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.18-prealpha...v0.0.19-prealpha
[0.0.18-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.17-prealpha...v0.0.18-prealpha
[0.0.17-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.16-prealpha...v0.0.17-prealpha
[0.0.16-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.15-prealpha...v0.0.16-prealpha
[0.0.15-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.14-prealpha...v0.0.15-prealpha
[0.0.14-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.13-prealpha...v0.0.14-prealpha
[0.0.13-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.12-prealpha...v0.0.13-prealpha
[0.0.12-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.11-prealpha...v0.0.12-prealpha
[0.0.11-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.10-prealpha...v0.0.11-prealpha
[0.0.10-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.9-prealpha...v0.0.10-prealpha
[0.0.9-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.8-prealpha...v0.0.9-prealpha
[0.0.8-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.7-prealpha...v0.0.8-prealpha
[0.0.7-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.6-prealpha...v0.0.7-prealpha
[0.0.6-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.5-prealpha...v0.0.6-prealpha
[0.0.5-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.4-prealpha...v0.0.5-prealpha
[0.0.4-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.3-prealpha...v0.0.4-prealpha
[0.0.3-prealpha]: https://github.com/ErsatzTV/ErsatzTV/compare/v0.0.1-prealpha...v0.0.3-prealpha
[0.0.1-prealpha]: https://github.com/ErsatzTV/ErsatzTV/releases/tag/v0.0.1-prealpha
