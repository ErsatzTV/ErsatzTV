#! /bin/bash

SCRIPT_FOLDER=$(dirname ${BASH_SOURCE[0]})
REPO_ROOT=$(realpath "$SCRIPT_FOLDER/../..")

APP_NAME="$REPO_ROOT/ErsatzTV.app"
ENTITLEMENTS="$SCRIPT_FOLDER/ErsatzTV.entitlements"
SIGNING_IDENTITY="C3BBCFB2D6851FF0DCA6CAC06A3EF1ECE71F9FFF"

codesign --force --verbose --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" --deep "$APP_NAME"
