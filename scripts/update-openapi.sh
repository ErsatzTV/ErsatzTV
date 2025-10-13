#! /usr/bin/env bash

cd "$(git rev-parse --show-toplevel)" || exit
cd ErsatzTV && dotnet build -t:GenerateOpenApiDocuments
