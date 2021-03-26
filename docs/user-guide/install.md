ErsatzTV is available as Docker images and as pre-built binary packages for Windows, MacOS and Linux. 

## Docker Images

<a href="https://hub.docker.com/r/jasongdove/ersatztv"><img alt="Docker Pull Count" src="https://img.shields.io/docker/pulls/jasongdove/ersatztv"></a>

### Latest Release Tags

Base (software transcoding): `jasongdove/ersatztv:latest`

Nvidia hardware-accelerated transcoding: `jasongdove/ersatztv:latest-nvidia`

VAAPI hardware-accelerated transcoding: `jasongdove/ersatztv:latest-vaapi`

### Development Tags

Development tags update much more frequently, but have the potential to be less stable than releases. 

Base (software transcoding): `jasongdove/ersatztv:develop`

Nvidia hardware-accelerated transcoding: `jasongdove/ersatztv:develop-nvidia`

VAAPI hardware-accelerated transcoding: `jasongdove/ersatztv:develop-vaapi`

### Docker

1. Download the latest container image

```
docker pull jasongdove/ersatztv
```

2. Create a directory to store configuration data

```
mkdir /path/to/config
```

3. Create and run a container

```
docker run -d \
  --name ersatztv \
  -e TZ=America/Chicago \
  -p 8409:8409 \
  -v /path/to/config:/root/.local/share/ersatztv \
  -v /path/to/shared/media:/path/to/shared/media:ro \
  --restart unless-stopped \
  jasongdove/ersatztv
```

### Unraid Docker

## Windows

### Manual Installation

1. Create a folder `ersatztv` at your preferred install location.
2. Download and extract the latest version to the `ersatztv` folder.
3. Run `ErsatzTV.exe`
4. Open your browser to `http://[server-ip]:8409`

## MacOS

### Manual Installation

1. Create a folder `ersatztv` at your preferred install location.
2. Download and extract the latest version to the `ersatztv` folder.
3. Run `ErsatzTV`
4. Open your browser to `http://[server-ip]:8409`

## Linux

### Manual Installation

1. Create a folder `ersatztv` at your preferred install location.
2. Download and extract the latest version to the `ersatztv` folder.
3. Run `ErsatzTV`
4. Open your browser to `http://[server-ip]:8409`
