#! /usr/bin/env bash

cd "$(git rev-parse --show-cdup)" || exit

dotnet tool restore
dotnet jb cleanupcode ErsatzTV.sln --exclude='generated/**;ErsatzTV/Shared/ContentTable.razor'
