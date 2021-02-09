# ErsatzTV

ErsatzTV is pre-alpha software for configuring and streaming custom live channels using your own media. The software is currently unstable and under active development.

## Features

- Multiple content sources
  - Local files and [NFO metadata](https://kodi.wiki/view/NFO_files) (mostly working)
  - [Plex](https://www.plex.tv/) content and metadata (under active development)
  - [Jellyfin](https://jellyfin.org/) content and metadata (future)
- IPTV server and [HDHomeRun](https://info.hdhomerun.com/info/http_api) emulation support a wide range of client applications
  - Plex Live TV (via HDHomeRun emulation)
  - [Channels](https://getchannels.com/) (via IPTV server)
  - [TiviMate IPTV Player](https://play.google.com/store/apps/details?id=ar.tvplayer.tv)

## Screenshots

### Plex Live TV

![Plex Live TV Stream](docs/plex-live-tv-stream.png)
Sintel is © copyright Blender Foundation | durian.blender.org

## Development

### Requirements

- [.NET 5.0](https://dotnet.microsoft.com/download)
- [ffmpeg and ffprobe](https://ffmpeg.org/download.html)

### Run server

```shell
cd ErsatzTV
dotnet run
```

### Run tests

```shell
dotnet test
```

### Cleanup code

```shell
./scripts/cleanup-code.sh
```

## License

This project is inspired by [pseudotv-plex](https://github.com/DEFENDORe/pseudotv) and
the [dizquetv](https://github.com/vexorian/dizquetv) fork and is released under the [zlib license](LICENSE).