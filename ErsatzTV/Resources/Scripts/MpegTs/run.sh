#! /bin/sh

"{{ FFmpegPath }}" -nostdin -threads 1 -hide_banner -loglevel error -nostats -fflags +genpts+discardcorrupt+igndts -readrate 1.0 -i "{{ HlsUrl }}" -map 0 -c copy -metadata service_provider="ErsatzTV" -metadata service_name="{{ ChannelName }}" -f mpegts pipe:1
