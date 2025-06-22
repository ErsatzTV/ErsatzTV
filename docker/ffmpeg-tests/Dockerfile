FROM jasongdove/ersatztv-ffmpeg:7.1.1
RUN apt-get update && apt-get install -y ca-certificates gnupg dotnet9 dotnet-sdk-9.0 aspnetcore-runtime-9.0 mkvtoolnix
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
COPY ErsatzTV.Scanner.Tests/*.csproj ./ErsatzTV.Scanner.Tests/
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
COPY ErsatzTV.Scanner.Tests/. ./ErsatzTV.Scanner.Tests/
ARG INFO_VERSION="unknown"
WORKDIR /source/ErsatzTV.Scanner.Tests
RUN dotnet build

# final stage/image
ENV FONTCONFIG_PATH=/etc/fonts
RUN fc-cache update
WORKDIR /source
ENV ETV_CONFIG_FOLDER=/config
ENV ETV_TRANSCODE_FOLDER=/transcode
ENV ETV_DISABLE_VULKAN=1
ENTRYPOINT ["dotnet", "test", "-v", "quiet", "--no-build", "--tl", "--nologo", "ErsatzTV.Scanner.Tests/ErsatzTV.Scanner.Tests.csproj", "--logger", "console;verbosity=minimal", "--filter", "FullyQualifiedName=ErsatzTV.Scanner.Tests.Core.FFmpeg.TranscodingTests.Transcode"]
