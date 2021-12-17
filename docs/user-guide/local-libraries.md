## Movies

### Folder Layout

The `Movies` library requires movie subfolders. The following is a (non-exhaustive) list of valid locations for movies:

- `Movie (1999)\Movie (1999).mkv`
- `Movie\Movie.mkv`

### NFO Metadata

Each movie folder may contain a `movie.nfo` file, or an NFO file with exactly the same name as the movie, except for the `.nfo` extension. See [Kodi Wiki](https://kodi.wiki/view/NFO_files/Movies) for more information.
ErsatzTV will read the following fields from the movie NFO:

- Title
- Year
- Premiered
- Plot
- Genre(s)
- Tag(s)
- Studio(s)

### Movie Fallback Metadata

When no movie NFO is found, the movie metadata will only contain a title and a year, both parsed from the movie file name. Example:

- `Movie (1999).mkv`

## Shows

### Folder Layout

The `Shows` library requires show and season subfolders. The following is a (non-exhaustive) list of valid locations for episodes:

- `Show (1999)\Season 01\Show - S01E01.mp4`
- `Show\Season 1\Show - s1e1.mp4`
- `Show\Season 1\Episode 1\Show - s1e1.mp4`

### Show NFO Metadata

Each show folder may contain a `tvshow.nfo` file. See [Kodi Wiki](https://kodi.wiki/view/NFO_files/TV_shows#TV_Show) for more information.
ErsatzTV will read the following fields from the show NFO:

- Title
- Year
- Premiered
- Plot
- Genre(s)
- Tag(s)
- Studio(s)

### Show Fallback Metadata

When no show NFO is found, the show metadata will only contain the title and an optional year, both parsed from the episode file name.
Examples:

- `Title`
- `Title (1999)`

### Season Metadata

The season number is parsed from the season subfolder.
Examples:

- `Season 01`
- `Season 1`

### Episode NFO Metadata

Each episode may have a corresponding NFO file with exactly the same name, except for the `.nfo` extension. See [Kodi Wiki](https://kodi.wiki/view/NFO_files/TV_shows#Episodes) for more information.
ErsatzTV will read the following fields from the episode NFO:

- Title
- Episode
- Aired
- Plot

### Episode Fallback Metadata

When no episode NFO is found, the episode metadata will only contain the title and the episode number, both parsed from the episode file name.
Examples:

- `Title - s01e04.mkv`
- `Title - S1E4.mkv`

## Music Videos

### Folder Layout

The `Music Videos` library requires artist subfolders. The following is a (non-exhaustive) list of valid locations for music videos:

- `Artist\Album\Track.mp4`
- `Artist\Track\Track.mp4`
- `Artist\Track.mp4`

### Artist NFO Metadata

Each artist subfolder may contain an `artist.nfo` file. See [Kodi Wiki](https://kodi.wiki/view/NFO_files/Music#Artists) for more information.
ErsatzTV will read the following fields from the artist NFO:

- Name
- Disambiguation
- Biography
- Genre(s)
- Style(s)
- Mood(s)

### Artist Fallback Metadata

When no artist NFO is found, the artist metadata will only contain a name, which will be the exact name of the artist subfolder.

## Other Videos

### Folder Layout

The `Other Videos` library has no folder requirements, but folders can be a useful source of metadata.

### Other Video Fallback Metadata

Other videos will have a tag added to their metadata for every containing folder, including the top-level folder. As an example, consider adding a commercials folder with the following files:

- `Commercials\1990\Cars\1990 Car Commercial 1.mkv`
- `Commercials\1992\Cars\1992 Car Commercial 1.mkv`

Your other video library will then have two media items with the following metadata:

1. title: `1990 Car Commercial 1`, tags: `Commercials`, `1990`, `Cars`
2. title: `1992 Car Commercial 1`, tags: `Commercials`, `1992`, `Cars`

## Songs

### Folder Layout

The `Songs` library has no folder requirements, but folders can be a useful source of metadata.

### Songs Metadata

Songs will have basic metadata pulled from embedded tags (artist, album, title).

### Songs Fallback Metadata

Songs will have a tag added to their metadata for every containing folder, including the top-level folder. As an example, consider adding a songs folder with the following files:

- `Rock\Awesome Band\Awesome Album\01 Track 1.flac`
- `Rock\Awesome Band\Better Album\05 Track 5.flac`

Your songs library will then have two media items with the following metadata:

1. title: `01 Track 1`, tags: `Rock`, `Awesome Band`, `Awesome Album`
2. title: `05 Track 5`, tags: `Rock`, `Awesome Band`, `Better Album`