## Search Box

Movies, Shows, Episodes, Artists and Music Videos can be searched using the search box next to the ErsatzTV logo.

![Search Box](../images/search-box.png)

## Search Fields

The `title` field of all media types is searched by default if no other field is specified.

### Movies

The following fields are available for searching movies:

- `title`: The movie title
- `genre`: The movie genre
- `tag`: The movie tag (not available with Plex metadata)
- `plot`: The movie plot
- `studio`: The movie studio
- `actor`: An actor from the movie
- `director`: A director from the movie
- `writer`: A writer from the movie
- `library_name`: The name of the library that contains the movie
- `content_rating`: The movie content rating (case-sensitive) 
- `language`: The movie audio stream language
- `release_date`: The movie release date (YYYYMMDD)
- `added_date`: The date the movie was added to ErsatzTV (YYYYMMDD)
- `minutes`: The rounded-up whole number duration of the movie in minutes
- `height`: The movie height
- `width`: The movie width
- `type`: Always `movie`

### Shows

The following fields are available for searching shows:

- `title`: The show title
- `genre`: The show genre
- `tag`: The show tag (not available with Plex metadata)
- `plot`: The show plot
- `studio`: The show studio
- `actor`: An actor from the show
- `library_name`: The name of the library that contains the show
- `content_rating`: The show content rating (case-sensitive)
- `language`: The show audio stream language
- `release_date`: The show release date (YYYYMMDD)
- `added_date`: The date the show was added to ErsatzTV (YYYYMMDD)
- `type`: Always `show`

### Seasons

The following fields are available for searching seasons:

- `title`: The season title
- `library_name`: The name of the library that contains the season
- `season_number`: The season number
- `show_title`: The title of the show that contains the season
- `type`: Always `season`

### Episodes

The following fields are available for searching episodes:

- `title`: The episode title
- `plot`: The episode plot
- `director`: A director from the episode
- `writer`: A writer from the episode
- `library_name`: The name of the library that contains the episode
- `language`: The episode audio stream language
- `release_date`: The episode release date (YYYYMMDD)
- `added_date`: The date the episode was added to ErsatzTV (YYYYMMDD)
- `minutes`: The rounded-up whole number duration of the episode in minutes
- `height`: The episode height
- `width`: The episode width
- `season_number`: The episode season number
- `episode_number`: The episode number
- `show_title`: The title of the show that contains the episode
- `type`: Always `episode`

### Artists

The following fields are available for searching artists:

- `title`: The artist name
- `genre`: The artist genre
- `style`: The artist style
- `mood`: The artist mood
- `added_date`: The date the artist was added to ErsatzTV (YYYYMMDD)
- `library_name`: The name of the library that contains the artist
- `type`: Always `artist`

### Music Videos

The following fields are available for searching music videos:

- `title`: The music video title
- `artist`: The music video artist
- `album`: The music video album
- `genre`: The music video genre
- `library_name`: The name of the library that contains the music video
- `language`: The music video audio stream language
- `release_date`: The music video release date (YYYYMMDD)
- `added_date`: The date the music video was added to ErsatzTV (YYYYMMDD)
- `minutes`: The rounded-up whole number duration of the music video in minutes
- `height`: The music video height
- `width`: The music video width
- `type`: Always `music_video`

### Other Videos

The following fields are available for searching other videos:

- `title`: The filename of the video (without extension)
- `tag`: All of the video's parent folders
- `minutes`: the rounded-up whole number duration of the video in minutes
- `added_date`: The date the other video was added to ErsatzTV (YYYYMMDD)
- `height`: The other video height
- `width`: The other video width
- `type`: Always `other_video`

### Songs

The following fields are available for searching songs:

- `title`: The song title, or the filename of the song (without extension)
- `album`: The song album
- `artist`: The song/track artist
- `album_artist`: The album artist
- `genre`: The song genre
- `tag`: All of the song's parent folders
- `minutes`: the rounded-up whole number duration of the song in minutes
- `added_date`: The date the song was added to ErsatzTV (YYYYMMDD)
- `type`: Always `song`

## Special Search Fields

- `released_inthelast`: For any media type that supports `release_date`, `released_inthelast` takes a number and a unit (days, weeks, months, years) and returns items released between the specified time ago and now
- `released_notinthelast`: For any media type that supports `release_date`, `released_notinthelast` takes a number and a unit (days, weeks, months, years) and returns items released before the specified time ago
- `released_onthisday`: For any media type that supports `release_date`, `released_onthisday` takes any value (ignored) and will return items released on this month number and day number in previous years
- `added_inthelast`: For any media type that supports `added_date`, `added_inthelast` takes a number and a unit (days, weeks, months, years) and returns items added to ErsatzTV between the specified time ago and now
- `added_notinthelast`: For any media type that supports `added_date`, `added_notinthelast` takes a number and a unit (days, weeks, months, years) and returns items added to ErsatzTV before the specified time ago

## Sample Searches

### Christmas

`plot:christmas`

### Christmas without Horror

`plot:christmas NOT genre:horror`

### 1970's Movies

`type:movie AND release_date:197*`

### 1970's-1980's Comedies

`genre:comedy AND (release_date:197* OR release_date:198*)`

### Lush Music (Artists)

`mood:lush`

### Episodes from the past week

`type:episode AND released_inthelast:"1 week"`

### Episodes older than the past week

`type:episode AND released_notinthelast:"1 week"`

### Episodes released on this day

`type:episode AND released_onthisday:1`