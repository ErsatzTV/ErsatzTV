## Create Channel

Create a Channel by navigating to the `Channels` page and clicking `Add Channel`.

### Channel Number

Channel numbers can be whole numbers or can contain one decimal, like `500` or `500.5`.

### Streaming Mode

Two streaming modes are currently supported: `Transport Stream` and `HttpLiveStreaming`.
`Transport Stream` is considered stable and is recommended for most purposes.
`HttpLiveStreaming` is unstable and is not recommended for general use.

### FFmpeg Profile

FFmpeg Profiles are collections of encoding settings that are applied to all content on a channel.
The default FFmpeg Profile is probably "good enough" for initial testing.

### Logo

Channel logos can be added using the `Upload Logo` button and the logos will display in most client program guides.

## Create Schedule

Schedules are used to control the playback order of media items on a channel.
Create a Schedule by navigating to the `Schedules` page, clicking `Add Schedule` and giving your schedule a name, a collection playback order, and clicking `Add Schedule` to confirm your selections.

### Collection Playback Order

Select the desired playback order for media items within each collection in the schedule:

- `Chronological`: Items are ordered by release date, then by season and episode number.
- `Random`: Items are randomly ordered and may contain repeats.
- `Shuffle`: Items are randomly ordered and no item will be played a second time until every item from the collection has been played once.

### Schedule Items

Schedules contain an ordered list of items (collections), and will play back one or more items from each collection before advancing to the next schedule item.

## Create Playout

