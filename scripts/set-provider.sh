#! /usr/bin/env bash

if [[ $# -eq 0 ]] ; then
    echo 'Please specify a database provider'
    exit 1
fi

cd "$(git rev-parse --show-cdup)" || exit
cd ErsatzTV && dotnet user-secrets set "Provider" "$1"

cd "$(git rev-parse --show-cdup)" || exit
cd ErsatzTV.Scanner && dotnet user-secrets set "Provider" "$1"
