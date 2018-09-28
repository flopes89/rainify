# rainify

Rainmeter plugin for Spotify Web API.

This plugin uses the Spotify Web API to retrieve information about the current playback
status of your Spotify account.

## Usage

**Tested with**: Windows 10 Home x64, .NET 4.6.1, Rainmeter 4.2

- [Create a Spotify App](https://developer.spotify.com/dashboard/applications)
- Download the current [Release]() rmskin package and install it in Rainmeter
- Open `cmd` and `cd` to `<skinFolder>\@Resources\Console\<x64-or-x86>\`
- Execute `rainifyConsole.exe authorize -c <clientId> -s <clientSecret>`
	- Optionally add  `-p <port>` in case your port `:80` is blocked by something
- Paste the configuration into the `base.ini` Skin at the marked location
	- Adjust the `ConsolePath` according to your platform architecture as well
- Reload the skin - You should see all playback data (if you're currently playing anything in Spotify)

## Available Fields

All fields that you can use in your Skins are listed in the `base.ini` that ships
with the `rmskin` package from the [Release]() page.
