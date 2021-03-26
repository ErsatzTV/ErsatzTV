## Required Urls

For all clients, the `M3U` and/or the `XMLTV` urls are needed and can be copied from the top right of the ErsatzTV UI.

![ErsatzTV M3U and XMLTV Links](../images/etv-m3u-xmltv-links.png)

## Supported Clients

- [Plex](#plex)
- [Jellyfin](#jellyfin)
- [TiviMate](#tivimate)
- [Channels DVR](#channels-dvr)

## Plex

A [Plex Pass](https://www.plex.tv/plex-pass/) is required for ErsatzTV to work with Plex.

## Jellyfin

Jellyfin requires two steps to configure Live TV:

- [Add Tuner Device](#add-tuner-device)
- [Add TV Guide Data](#add-tv-guide-data)

### Add Tuner Device

From the Admin Dashboard in Jellyfin, click `Live TV` and `+` to add a new tuner device:

![Jellyfin Add Tuner Device](../images/jellyfin-add-tuner-device.png)

For `Tuner Type` select `HD Homerun`, and for `Tuner IP Address` enter ErsatzTV's IP address and port, like `192.168.1.100:8409` (use your server IP, not necessarily 192.168.1.100).

![Jellyfin Live TV Tuner Setup](../images/jellyfin-live-tv-tuner-setup.png)

### Add TV Guide Data

From the Admin Dashboard in Jellyfin, click `Live TV` and `+` to add a tv guide data provider and select `XMLTV`.

![Jellyfin Add TV Guide Data Provider](../images/jellyfin-add-tv-guide-data-provider.png)

Enter the `XMLTV` url from ErsatzTV (see [required urls](#required-urls)) and click `Save`.

![Jellyfin XMLTV Settings](../images/jellyfin-xmltv-settings.png)

## TiviMate

### Add Playlist

Start by adding a playlist under `Settings` > `Playlists` > `Add playlist`.
The playlist type is `M3U Playlist` and the url is the `M3U` url from ErsatzTV (see [required urls](#required-urls)).

![TiviMate Playlist URL](../images/tivimate-playlist-url.png)

Change the playlist name if desired, and leave `TV playlist` selected.

### Add EPG

The EPG url should be automatically detected by TiviMate, but can be manually entered as the `XMLTV` url from ErsatzTV (see [required urls](#required-urls)).

![TiviMate EPG URL](../images/tivimate-epg-url.png)

## Channels DVR

