FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal-amd64 AS runtime-base
RUN apt-get update && apt-get install -y ffmpeg

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
RUN apt-get update && apt-get install -y ca-certificates
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY ErsatzTV/*.csproj ./ErsatzTV/
COPY generated/ErsatzTV.Api.Sdk/src/ErsatzTV.Api.Sdk/*.csproj ./generated/ErsatzTV.Api.Sdk/src/ErsatzTV.Api.Sdk/
COPY ErsatzTV.Application/*.csproj ./ErsatzTV.Application/
COPY ErsatzTV.CommandLine/*.csproj ./ErsatzTV.CommandLine/
COPY ErsatzTV.Core/*.csproj ./ErsatzTV.Core/
COPY ErsatzTV.Core.Tests/*.csproj ./ErsatzTV.Core.Tests/
COPY ErsatzTV.Infrastructure/*.csproj ./ErsatzTV.Infrastructure/
RUN dotnet restore -r linux-x64

# copy everything else and build app
COPY ErsatzTV/. ./ErsatzTV/
COPY generated/ErsatzTV.Api.Sdk/src/ErsatzTV.Api.Sdk/. ./generated/ErsatzTV.Api.Sdk/src/ErsatzTV.Api.Sdk/
COPY ErsatzTV.Application/. ./ErsatzTV.Application/
COPY ErsatzTV.CommandLine/. ./ErsatzTV.CommandLine/
COPY ErsatzTV.Core/. ./ErsatzTV.Core/
COPY ErsatzTV.Core.Tests/. ./ErsatzTV.Core.Tests/
COPY ErsatzTV.Infrastructure/. ./ErsatzTV.Infrastructure/
WORKDIR /source/ErsatzTV
RUN dotnet publish -c release -o /app -r linux-x64 --self-contained false --no-restore

# final stage/image
FROM runtime-base
WORKDIR /app
EXPOSE 8409
COPY --from=build /app ./
ENTRYPOINT ["./ErsatzTV"]
