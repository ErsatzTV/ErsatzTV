FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal-amd64 AS dotnet-runtime

FROM jrottenberg/ffmpeg:4.3-nvidia1804 AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
RUN apt-get update \
    && DEBIAN_FRONTEND="noninteractive" apt-get install -y libicu-dev tzdata \
    && rm -rf /var/lib/apt/lists/* 
