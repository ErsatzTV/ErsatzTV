## Search Box

Movies, Shows, Artists and Music Videos can be searched using the search box next to the ErsatzTV logo.

![Search Box](../images/search-box.png)

## Search Fields

The following fields are available for searching:

- `title`: The movie, show, artist or music video name/title
- `genre`: The movie, show, or artist genre
- `tag`: The movie or show tag (not available with Plex metadata)
- `style`: The artist style
- `mood`: The artist mood
- `plot`: The movie or show plot
- `studio`: The movie or show studio
- `library_name`: The name of the library
- `language`: The movie, show or music video audio stream language
- `release_date`: The movie or show release date (YYYYMMDD)
- `type`: The media item type: `movie`, `show`, `artist` or `music_video`

Note that the `title` field is searched by default if no other field is specified.

## Sample Searches

### Christmas Movies

`plot:christmas`

### Christmas Movies without Horror

`plot:christmas NOT genre:horror`

### 1970's Movies

`type:movie AND release_date:197*`

### 1970's-1980's Comedies

`genre:comedy AND (release_date:197* OR release_date:198*)`

### Lush Music

`mood:lush`