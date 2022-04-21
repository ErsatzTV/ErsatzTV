#! /usr/bin/env bash

cd "$(git rev-parse --show-cdup)" || exit

dotnet tool restore
dotnet jb cleanupcode ErsatzTV.sln --exclude='CHANGELOG.md;scripts/**;generated/**;ErsatzTV/client-app/**'
