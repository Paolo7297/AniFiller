# AniFiller

> [!WARNING]
> This project was made with the help of AI as I have very little knowledge of C# and .NET.
> While AI was primarily used for the initial setup, I have since made significant modifications to ensure the project worked well.

## About

This plugin adds an indicator to the Jellyfin UI to show if an episode is filler or not.
This is done by using the TheTVDB API to get the absolute episode number and then checking the AnimeFillerList API to see if the episode is filler or not.

## Requirements

- Using TheTVDB Metadata for your Jellyfin Anime library
- Create a TheTVDB API key [here](https://www.thetvdb.com/api-information).
- [Jellyfin-JavaScript-Injector](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector) installed.

## Configuration

1. Open the `Configuration` page in the Jellyfin web interface.
2. Scroll down to the `AniFiller` section.
3. Enter your TheTVDB API key in the `TheTVDB API Key` field.
4. Save the configuration.
5. Enjoy!
