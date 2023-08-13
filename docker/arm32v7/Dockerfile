FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy-arm32v7 AS dotnet-runtime

FROM jasongdove/ersatztv-ffmpeg:6.0-arm AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
RUN apt-get update && DEBIAN_FRONTEND="noninteractive" apt-get install -y libicu-dev \
    tzdata \
    fontconfig \
    fonts-dejavu \
    libgdiplus

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy-amd64 AS build
RUN apt-get update && apt-get install -y ca-certificates
RUN curl -fsSL https://deb.nodesource.com/setup_14.x | bash -
RUN apt-get install -y nodejs
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY nuget.config .
COPY lib/nuget/* ./lib/nuget/
COPY artwork/* ./artwork/
COPY ErsatzTV/*.csproj ./ErsatzTV/
COPY ErsatzTV/client-app/package*.json ./ErsatzTV/client-app/
COPY ErsatzTV.Application/*.csproj ./ErsatzTV.Application/
COPY ErsatzTV.Core/*.csproj ./ErsatzTV.Core/
COPY ErsatzTV.FFmpeg/*.csproj ./ErsatzTV.FFmpeg/
COPY ErsatzTV.Infrastructure/*.csproj ./ErsatzTV.Infrastructure/
COPY ErsatzTV.Infrastructure.Sqlite/*.csproj ./ErsatzTV.Infrastructure.Sqlite/
COPY ErsatzTV.Infrastructure.MySql/*.csproj ./ErsatzTV.Infrastructure.MySql/
COPY ErsatzTV.Scanner/*.csproj ./ErsatzTV.Scanner/
RUN dotnet restore -r linux-arm ErsatzTV/

# copy everything else and build app
COPY ErsatzTV/. ./ErsatzTV/
COPY ErsatzTV.Application/. ./ErsatzTV.Application/
COPY ErsatzTV.Core/. ./ErsatzTV.Core/
COPY ErsatzTV.FFmpeg/. ./ErsatzTV.FFmpeg/
COPY ErsatzTV.Infrastructure/. ./ErsatzTV.Infrastructure/
COPY ErsatzTV.Infrastructure.Sqlite/. ./ErsatzTV.Infrastructure.Sqlite/
COPY ErsatzTV.Infrastructure.MySql/. ./ErsatzTV.Infrastructure.MySql/
COPY ErsatzTV.Scanner/. ./ErsatzTV.Scanner/
WORKDIR /source/ErsatzTV.Scanner
ARG INFO_VERSION="unknown"
RUN dotnet publish ErsatzTV.Scanner.csproj --framework net7.0 -c release -o /app --runtime linux-arm --no-self-contained --no-restore -p:DebugType=Embedded -p:PublishSingleFile=false -p:PublishTrimmed=false -p:InformationalVersion=${INFO_VERSION}
WORKDIR /source/ErsatzTV
RUN sed -i '/Scanner/d' ErsatzTV.csproj
RUN dotnet publish ErsatzTV.csproj --framework net7.0 -c release -o /app --runtime linux-arm --no-self-contained --no-restore -p:DebugType=Embedded -p:PublishSingleFile=false -p:PublishTrimmed=false -p:InformationalVersion=${INFO_VERSION}
          
# final stage/image
FROM runtime-base
ENV FONTCONFIG_PATH=/etc/fonts
RUN fc-cache update
WORKDIR /app
EXPOSE 8409
COPY --from=build /app ./
ENTRYPOINT ["./ErsatzTV"]
