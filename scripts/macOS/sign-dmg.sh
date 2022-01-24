#! /bin/bash

SCRIPT_FOLDER=$(dirname ${BASH_SOURCE[0]})
REPO_ROOT="$SCRIPT_FOLDER/../.."

DMG_NAME="$REPO_ROOT/ErsatzTV.dmg"
ENTITLEMENTS="$SCRIPT_FOLDER/ErsatzTV.entitlements"
SIGNING_IDENTITY="C3BBCFB2D6851FF0DCA6CAC06A3EF1ECE71F9FFF"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$DMG_NAME"
