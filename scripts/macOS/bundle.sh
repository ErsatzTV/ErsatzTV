#! /bin/bash

SCRIPT_FOLDER=$(dirname ${BASH_SOURCE[0]})
REPO_ROOT="$SCRIPT_FOLDER/../.."

APP_NAME="$REPO_ROOT/ErsatzTV.app"
PUBLISH_OUTPUT_DIRECTORY="$REPO_ROOT/publish/."
INFO_PLIST="$SCRIPT_FOLDER/Info.plist"
ICON_SOURCE="$REPO_ROOT/artwork/ErsatzTV.icns"
ICON_FILE="ErsatzTV.icns"

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME"

mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
cp "$ICON_SOURCE" "$APP_NAME/Contents/Resources/$ICON_FILE"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"
cp "$SCRIPT_FOLDER/launcher.sh" "$APP_NAME/Contents/MacOS/"