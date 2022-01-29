#! /bin/bash

SCRIPT_FOLDER=$(dirname ${BASH_SOURCE[0]})
REPO_ROOT=$(realpath "$SCRIPT_FOLDER/../..")

APP_NAME="$REPO_ROOT/ErsatzTV.app"
PUBLISH_OUTPUT_DIRECTORY="$REPO_ROOT/publish/."

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

cd "$REPO_ROOT/ErsatzTV-macOS" || exit
xcodebuild build
cd "$REPO_ROOT" || exit
cp -R "$REPO_ROOT/ErsatzTV-macOS/build/Release/ErsatzTV-macOS.app" "$APP_NAME"

cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"
