#! /usr/bin/env bash

if [[ $# -eq 0 ]] ; then
    echo 'Please specify a unique migration name'
    exit 1
fi

ROOT="$(git rev-parse --show-toplevel)"

cd "$ROOT/ErsatzTV.Infrastructure" || exit

dotnet ef migrations add $1 \
    --context TvContext \
    --startup-project "$ROOT/ErsatzTV" \
    --project "$ROOT/ErsatzTV.Infrastructure.Sqlite" \
    -- --provider Sqlite && \
    dotnet ef migrations add $1 \
        --context TvContext \
        --startup-project "$ROOT/ErsatzTV" \
        --project "$ROOT/ErsatzTV.Infrastructure.MySql" \
        -- --provider MySql
