FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-amd64 AS dotnet-runtime

FROM jasongdove/ersatztv-ffmpeg:7.1.1-nvidia AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble-amd64 AS build
RUN apt-get update && apt-get install -y ca-certificates gnupg
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY artwork/* ./artwork/
COPY ErsatzTV/*.csproj ./ErsatzTV/
COPY ErsatzTV.Application/*.csproj ./ErsatzTV.Application/
COPY ErsatzTV.Core/*.csproj ./ErsatzTV.Core/
COPY ErsatzTV.FFmpeg/*.csproj ./ErsatzTV.FFmpeg/
COPY ErsatzTV.Infrastructure/*.csproj ./ErsatzTV.Infrastructure/
COPY ErsatzTV.Infrastructure.Sqlite/*.csproj ./ErsatzTV.Infrastructure.Sqlite/
COPY ErsatzTV.Infrastructure.MySql/*.csproj ./ErsatzTV.Infrastructure.MySql/
COPY ErsatzTV.Scanner/*.csproj ./ErsatzTV.Scanner/
RUN dotnet restore -r linux-x64 ErsatzTV/

# copy everything else and build app
COPY ErsatzTV/. ./ErsatzTV/
COPY ErsatzTV.Application/. ./ErsatzTV.Application/
COPY ErsatzTV.Core/. ./ErsatzTV.Core/
COPY ErsatzTV.FFmpeg/. ./ErsatzTV.FFmpeg/
COPY ErsatzTV.Infrastructure/. ./ErsatzTV.Infrastructure/
COPY ErsatzTV.Infrastructure.Sqlite/. ./ErsatzTV.Infrastructure.Sqlite/
COPY ErsatzTV.Infrastructure.MySql/. ./ErsatzTV.Infrastructure.MySql/
COPY ErsatzTV.Scanner/. ./ErsatzTV.Scanner/
ARG INFO_VERSION="unknown"
WORKDIR /source/ErsatzTV.Scanner
RUN dotnet publish ErsatzTV.Scanner.csproj -c release -o /app -r linux-x64 --self-contained false --no-restore /p:DebugType=Embedded /p:InformationalVersion=${INFO_VERSION}
WORKDIR /source/ErsatzTV
RUN sed -i '/Scanner/d' ErsatzTV.csproj
RUN dotnet publish ErsatzTV.csproj -c release -o /app -r linux-x64 --self-contained false --no-restore /p:DebugType=Embedded /p:InformationalVersion=${INFO_VERSION}

# final stage/image
FROM runtime-base
ENV FONTCONFIG_PATH=/etc/fonts
RUN fc-cache update
WORKDIR /app
COPY --from=build /app ./
ENV ETV_CONFIG_FOLDER=/config
ENV ETV_TRANSCODE_FOLDER=/transcode
ENV ETV_DISABLE_VULKAN=1
ENTRYPOINT ["./ErsatzTV"]
