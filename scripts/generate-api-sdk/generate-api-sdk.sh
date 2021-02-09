#! /usr/bin/env bash
set -e

TARGET_FOLDER="../../generated/ErsatzTV.Api.Sdk"

rm -f swagger.json

dotnet tool restore

dotnet build ../../ErsatzTV/ErsatzTV.csproj
dotnet swagger tofile --output swagger.json ../../ErsatzTV/bin/Debug/net5.0/ErsatzTV.dll v1

rm -rf "$TARGET_FOLDER"
mkdir "$TARGET_FOLDER"
cp .openapi-generator-ignore "$TARGET_FOLDER/"

openapi-generator-cli generate -i swagger.json \
    -g csharp-netcore \
    -o $TARGET_FOLDER \
    --additional-properties packageName=ErsatzTV.Api.Sdk \
    --additional-properties=targetFramework=netcoreapp3.1

rm -f "$TARGET_FOLDER/appveyor.yml"
rm -rf "$TARGET_FOLDER/src/ErsatzTV.Api.Sdk.Test"
