FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal-amd64 AS runtime-base
RUN apt-get update && apt-get install -y ffmpeg

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY ErsatzTV/*.csproj ./ErsatzTV/
COPY ErsatzTV.Tests/*.csproj ./ErsatzTV.Tests/
RUN dotnet restore -r linux-x64

# copy everything else and build app
COPY ErsatzTV/. ./ErsatzTV/
WORKDIR /source/ErsatzTV
RUN dotnet publish -c release -o /app -r linux-x64 --self-contained false --no-restore

# final stage/image
FROM runtime-base
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["./ErsatzTV"]
