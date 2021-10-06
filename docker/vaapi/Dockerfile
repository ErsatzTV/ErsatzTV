FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal-amd64 AS dotnet-runtime

FROM jrottenberg/ffmpeg:4.3-vaapi2004 AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
RUN apt-get update && DEBIAN_FRONTEND="noninteractive" apt-get install -y libicu-dev \
    tzdata \
    autoconf \
    libtool \
    libdrm-dev \
    git \
    pkg-config \
    build-essential \
    cmake \
    wget \
    && mkdir /tmp/intel && cd /tmp/intel \
    && wget -O - https://github.com/intel/libva/archive/refs/tags/2.12.0.tar.gz | tar zxf - \
    && cd libva-2.12.0 \
    && ./autogen.sh \
    && ./configure \
    && make -j$(nproc) \
    && make -j$(nproc) install \
    && cd /tmp/intel \
    && wget -O - https://github.com/intel/gmmlib/archive/refs/tags/intel-gmmlib-21.3.1.tar.gz | tar zxf - \
    && mv gmmlib-intel-gmmlib-21.3.1 gmmlib \
    && cd gmmlib \
    && mkdir build && cd build \
    && cmake .. \
    && make -j$(nproc) \
    && cd /tmp/intel \
    && git clone https://github.com/intel/media-driver && cd media-driver && git checkout intel-media-21.2.3 && cd .. \
    && mkdir build_media && cd build_media \
    && cmake ../media-driver \
    && make -j$(nproc) \
    && make install \
    && DEBIAN_FRONTEND="noninteractive" apt-get purge -y autoconf libtool git build-essential cmake wget \
    && apt autoremove -y \
    && rm -rf /tmp/intel \
    && rm -rf /var/lib/apt/lists/* \
    && mv /usr/lib/x86_64-linux-gnu/dri/i* /usr/local/lib/dri/

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
RUN apt-get update && apt-get install -y ca-certificates
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY ErsatzTV/*.csproj ./ErsatzTV/
COPY ErsatzTV.Application/*.csproj ./ErsatzTV.Application/
COPY ErsatzTV.Core/*.csproj ./ErsatzTV.Core/
COPY ErsatzTV.Core.Tests/*.csproj ./ErsatzTV.Core.Tests/
COPY ErsatzTV.Infrastructure/*.csproj ./ErsatzTV.Infrastructure/
RUN dotnet restore -r linux-x64

# copy everything else and build app
COPY ErsatzTV/. ./ErsatzTV/
COPY ErsatzTV.Application/. ./ErsatzTV.Application/
COPY ErsatzTV.Core/. ./ErsatzTV.Core/
COPY ErsatzTV.Core.Tests/. ./ErsatzTV.Core.Tests/
COPY ErsatzTV.Infrastructure/. ./ErsatzTV.Infrastructure/
WORKDIR /source/ErsatzTV
ARG INFO_VERSION="unknown"
RUN dotnet publish -c release -o /app -r linux-x64 --self-contained false --no-restore /p:InformationalVersion=${INFO_VERSION}

# final stage/image
FROM runtime-base
WORKDIR /app
EXPOSE 8409
COPY --from=build /app ./
ENTRYPOINT ["./ErsatzTV"]
