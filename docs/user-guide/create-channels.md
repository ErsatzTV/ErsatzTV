## Create Channel

Create a Channel by navigating to the `Channels` page and clicking `Add Channel`.

### Channel Number

Channel numbers can be whole numbers or can contain one decimal, like `500` or `500.5`.

### Streaming Mode

Three streaming modes are currently supported: `MPEG-TS` (Transport Stream), `HLS Direct` (HTTP Live Streaming Direct) and `HLS Hybrid` (HTTP Live Streaming Hybrid).

* `MPEG-TS` transcodes content and supports watermarks, though some clients will have issues at program boundaries
* `HLS Direct` does not transcode content and can perform better on low power systems, but does not support watermarks and some clients will have issues at program boundaries
* `HLS Hybrid` transcodes content and supports watermarks, and can perform very well at program boundaries with some clients

### FFmpeg Profile

FFmpeg Profiles are collections of transcoding settings that are applied to all content on a channel.
The default FFmpeg Profile is probably "good enough" for initial testing.

### Logo

Channel logos can be added using the `Upload Logo` button and the logos will display in most client program guides.

## Create Schedule

Schedules are used to control the playback order of media items on a channel.
Create a Schedule by navigating to the `Schedules` page, clicking `Add Schedule` and giving your schedule a name, and selecting the appropriate settings:

* `Keep Multi-Part Episodes Together`: This only applies to shuffled schedule items, and will try to intelligently group multi-part episodes (i.e. `s05e02 - whatever part 1` and `s05e03 - whatever part 2`) so they are always scheduled together and always play in the correct order.
* `Treat Collections As Shows`: This only applies when `Keep Multi-Part Episodes Together` is enabled, and will try to group multi-part episodes across shows within the collection (i.e. crossover episodes like `Show 1 - s03e04 - Whatever Part 1` and `Show 2 - s01e07 - Whatever Part 2`).

### Schedule Items

Schedules contain an ordered list of items (collections), and will play back one or more items from each collection before advancing to the next schedule item.

Edit the new schedule's items by clicking the `Edit Schedule Items` button for the schedule:

![Schedules Edit Schedule Items Button](../images/schedules-edit-schedule-items.png)

Add a new item to the schedule by clicking `Add Schedule Item` and configure as desired.

#### Start Type

Items with a `Dynamic` start type will start immediately after the preceding schedule item, while a `Fixed` start type requires a start time.

#### Collection Type

Schedule items can contain the following collection types:

- `Collection`: Select a collection that you have created manually.
- `Television Show`: Select an entire television show.
- `Television Season`: Select a specific season of a television show.
- `Artist`: Select all music videos for a specific artist.
- `Multi Collection`: Select all collections within a multi-collection.

#### Collection

Based on the selected collection type, select the desired collection.

### Playback Order

Select the desired playback order for media items within the selected collection:

- `Chronological`: Items are ordered by release date, then by season and episode number.
- `Random`: Items are randomly ordered and may contain repeats.
- `Shuffle`: Items are randomly ordered and no item will be played a second time until every item from the collection has been played once.

#### Playout Mode

Select how you want this schedule item to behave every time it is selected for playback.

- `One`: Play one media item from the collection before advancing to the next schedule item.
- `Multiple`: Play the specified `Multiple Count` of media items from the collection before advancing to the next schedule item.
- `Duration`: Play the maximum number of complete media items that will fit in the specified `Playout Duration` before either going offline for the remainder of the playout duration (an `Offline Tail`), or immediately advancing to the next schedule item.
- `Flood`: Play media items from the collection either forever or until the next schedule item's start time, if one exists.

Click `Save Changes` to save all changes made to the schedule's items.

## Create Playout

Playouts assign a schedule to a channel and individually track the ordered playback of collection items.
If a schedule is used in multiple playouts (channels), the channels may not have the same content playing at the same time.

To create a Playout, navigate to the `Playouts` page and click the `Add Playout` button. Then, select the appropriate channel and schedule, and click `Add Playout` to save.
