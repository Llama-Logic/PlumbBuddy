#!/bin/zsh

# Load environment variables from the .env file
if [ -f .env ]; then
  export $(grep -v '^#' .env | xargs)
else
  echo "No .env file found! Exiting."
  exit 1
fi

# Variables (taken from .env)
APP_BUNDLE="bin/Release/net8.0-maccatalyst/PlumbBuddy.app"
ZIP_FILE="PlumbBuddy.zip"

# Step 1: Remove old zip file if it exists
if [ -f "$ZIP_FILE" ]; then
  echo "Removing old zip file..."
  rm "$ZIP_FILE"
fi

# Step 2: Zip the app bundle
echo "Zipping the app bundle..."
/usr/bin/ditto -c -k --keepParent "$APP_BUNDLE" "$ZIP_FILE"

# Step 3: Submit for notarization using notarytool
echo "Submitting for notarization with notarytool..."
xcrun notarytool submit "$ZIP_FILE" --apple-id "$APPLE_ID" --password "$APP_PASSWORD" --team-id UA3C4N249P --wait

# Step 4: If notarization is successful, staple the app
if [ $? -eq 0 ]; then
  echo "Notarization succeeded! Stapling the ticket to the app..."
  xcrun stapler staple "$APP_BUNDLE"
  echo "App stapled successfully!"
else
  echo "Notarization failed."
  exit 1
fi

echo "All done! 😏"
