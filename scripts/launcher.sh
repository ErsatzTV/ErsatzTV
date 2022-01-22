#! /bin/sh

# Set the working directory
DIR=$(cd "$(dirname "$0")"; pwd)

# Run the application
echo "running from $DIR"

open -a Terminal $DIR/ErsatzTV
