FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal-amd64 AS dotnet-runtime

FROM jasongdove/ffmpeg-base:5.0-vaapi2004 AS runtime-base
COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
RUN apt-get update && DEBIAN_FRONTEND="noninteractive" apt-get install -y libicu-dev \
    tzdata \
    fontconfig \
    fonts-dejavu \
    libgdiplus \
    autoconf \
    libtool \
    libdrm-dev \
    libmfx-dev \
    git \
    pkg-config \
    build-essential \
    cmake \
    wget \
    mesa-va-drivers \
    && mkdir /tmp/intel && cd /tmp/intel \
    && wget -O - https://github.com/intel/libva/archive/refs/tags/2.14.0.tar.gz | tar zxf - \
    && cd libva-2.14.0 \
    && ./autogen.sh \
    && ./configure \
    && make -j$(nproc) \
    && make -j$(nproc) install \
    && cd /tmp/intel \
    && wget -O - https://github.com/intel/gmmlib/archive/refs/tags/intel-gmmlib-22.1.2.tar.gz | tar zxf - \
    && mv gmmlib-intel-gmmlib-22.1.2 gmmlib \
    && cd gmmlib \
    && mkdir build && cd build \
    && cmake .. \
    && make -j$(nproc) \
    && make install \
    && cd /tmp/intel \
    && git clone --depth 1 --branch intel-media-22.3 https://github.com/intel/media-driver \
    && mkdir build_media && cd build_media \
    && cmake ../media-driver \
    && make -j$(nproc) \
    && make install \
    && DEBIAN_FRONTEND="noninteractive" apt-get purge -y autoconf libtool git build-essential cmake wget \
    && apt autoremove -y \
    && rm -rf /tmp/intel \
    && rm -rf /var/lib/apt/lists/* \
    && mv /usr/lib/x86_64-linux-gnu/dri/* /usr/local/lib/dri/
