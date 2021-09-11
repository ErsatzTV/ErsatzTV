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
- `released_inthelast`: A range for the movie release date (days, weeks, months, years)
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
- `released_inthelast`: A range for the show release date (days, weeks, months, years)
- `type`: Always `show`

### Episodes

The following fields are available for searching episodes:

- `title`: The episode title
- `plot`: The episode plot
- `director`: A director from the episode
- `writer`: A writer from the episode
- `library_name`: The name of the library that contains the episode
- `language`: The episode audio stream language
- `release_date`: The episode release date (YYYYMMDD)
- `released_inthelast`: A range for the episode release date (days, weeks, months, years)
- `type`: Always `episode`

### Artists

The following fields are available for searching artists:

- `title`: The artist name
- `genre`: The artist genre
- `style`: The artist style
- `mood`: The artist mood
- `library_name`: The name of the library that contains the artist
- `type`: Always `artist`

### Music Videos

The following fields are available for searching music videos:

- `title`: The music video title
- `genre`: The music video genre
- `library_name`: The name of the library that contains the music video
- `language`: The music video audio stream language
- `type`: Always `music_video`

## Sample Searches

### Christmas

`plot:christmas`

### Christmas without Horror

`plot:christmas NOT genre:horror`

### 1970's Movies

`type:movie AND release_date:197*`

### 1970's-1980's Comedies

`genre:comedy AND (release_date:197* OR release_date:198*)`

### Lush Music

`mood:lush`

### Episodes from the past week

`type:episode AND released_inthelast:"1 week"`