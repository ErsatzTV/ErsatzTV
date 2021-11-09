FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal-amd64 AS dotnet-runtime

FROM jasongdove/ffmpeg-base:4.4-nvidia2004 AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
RUN apt-get update \
    && DEBIAN_FRONTEND="noninteractive" apt-get install -y libicu-dev tzdata \
    && rm -rf /var/lib/apt/lists/* 
