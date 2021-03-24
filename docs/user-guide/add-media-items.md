# Add Media Items

ErsatzTV needs to know about your media items in order to create channels.
Two library kinds are currently supported: [Local](#local-libraries) and [Plex](#plex-libraries).

## Local Libraries

ErsatzTV provides two local libraries, one for each supported media kind: `Movies` and `Shows`.

### Metadata

With local libraries, ErsatzTV will read metadata from [NFO files](https://kodi.wiki/view/NFO_files), falling back to a *minimal* amount of metadata if NFO files are not found.

### Add Media Items

To add media items to a local library under `Media` > `Libraries`, click the edit button for the library:

![Libraries Edit Icon](../images/libraries-edit-icon.png)

Then click the `Add Library Path` button and enter the path where your media files of the appropriate kind are stored:

![Add Local Library Path](../images/shows-add-local-library-path.png)

Finally, click `Add Local Library Path` and ErsatzTV will scan and import your media items.

## Plex Libraries

Plex libraries provide a way to synchronize your media from Plex to ErsatzTV.
This synchronization process is one-way: changes made within Plex are synchronized to ErsatzTV.
ErsatzTV will never make any modifications to your Plex configuration or media.

### Metadata

With Plex libraries, Plex provides all metadata.

### Add Media Items

