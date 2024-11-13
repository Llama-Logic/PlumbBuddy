#!/bin/zsh

rm -rf bin/ obj/ ../PlumbBuddy.Data/bin/ ../PlumbBuddy.Data/obj/
dotnet build -c Release && codesign --deep --force --verify --verbose --sign "Developer ID Application: Daniel Henry (UA3C4N249P)" --options runtime --timestamp "bin/Release/net9.0-maccatalyst/PlumbBuddy.app/Contents/MonoBundle/libe_sqlite3.dylib" && codesign --deep --force --verify --verbose --sign "Developer ID Application: Daniel Henry (UA3C4N249P)" --options runtime --timestamp "bin/Release/net9.0-maccatalyst/PlumbBuddy.app"