#! /bin/bash

SCRIPT_FOLDER=$(dirname ${BASH_SOURCE[0]})
REPO_ROOT="$SCRIPT_FOLDER/../.."

APP_NAME="$REPO_ROOT/ErsatzTV.app"
ENTITLEMENTS="$SCRIPT_FOLDER/ErsatzTV.entitlements"
SIGNING_IDENTITY="C3BBCFB2D6851FF0DCA6CAC06A3EF1ECE71F9FFF"

find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"
