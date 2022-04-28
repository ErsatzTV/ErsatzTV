#! /usr/bin/env bash

cd "$(git rev-parse --show-cdup)" || exit

dotnet tool restore
CHANGED=$(git status --porcelain | sed 's/^...//' | paste -sd ";" -)
dotnet jb cleanupcode ErsatzTV.sln --exclude='CHANGELOG.md;scripts/**;generated/**;ErsatzTV/client-app/**' --include="$CHANGED"
