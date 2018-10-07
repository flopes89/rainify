# rainify

Rainmeter plugin for Spotify Web API.

This plugin uses the Spotify Web API to retrieve information about the current playback
status of your Spotify account.

## Usage

**Tested with**: Windows 10 Home x64, .NET 4.6.1, Rainmeter 4.2

- [Create a Spotify App](https://developer.spotify.com/dashboard/applications)
- Download the current [Console](https://github.com/flopes89/rainify/releases) package and extract it
- Open a command prompt and navigate to the extracted release package
- Execute `rainifyConsole.exe authorize -c <clientId> -s <clientSecret>`
	- Optionally add  `-p <port>` in case your port `:80` is blocked by something
- Install the current [rmskin](https://github.com/flopes89/rainify/releases) package and install it
- Paste the configuration output of the rainifyConsole into the `raininfy-base.ini` Skin at the marked location
- (Adjust and) reload the skin
