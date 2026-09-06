RatioMaster.NET
===============

RatioMaster.NET fakes the upload and download figures a BitTorrent tracker sees for a torrent. It does
not download or upload any data and it does not need a BitTorrent client running: it talks to the
tracker directly, pretending to be one of 41 real client versions.

Version 1.0 is a full rewrite on .NET 10 with an [Avalonia](https://avaloniaui.net) interface. It runs
on Windows, Linux and macOS as a single self-contained file, so there is nothing to install.

## Download

Grab the archive for your system from the [latest release](https://github.com/NikolayIT/RatioMaster.NET/releases).

| System | File | Notes |
|---|---|---|
| Windows 10/11 (x64, arm64) | `RatioMaster.NET-win-x64.zip` | Unzip and run `RatioMaster.NET.exe`. |
| Linux (x64, arm64) | `RatioMaster.NET-linux-x64.tar.gz` | Needs `libfontconfig1`, `libx11-6`, `libice6`, `libsm6`. |
| macOS 12+ (Intel, Apple silicon) | `RatioMaster.NET-osx-arm64.zip` | Unsigned: run `xattr -dr com.apple.quarantine RatioMaster.NET.app` once. |

## What it does

- Runs any number of torrents at once, each with its own client emulation and speeds.
- Emulates 41 client versions across 16 families: uTorrent, BitComet, Azureus, Vuze, BitTorrent,
  Transmission, ABC, BitLord, BTuga, BitTornado, Burst, BitTyrant, BitSpirit, Deluge, KTorrent and Gnome BT.
- Copies the peer id, key, port and peer count out of a running client's memory when that client
  supports it (Windows fully, Linux best effort), otherwise generates values that match the client.
- Upload and download speeds with optional random jitter, plus a fresh random speed on every announce.
- Honours the tracker's announce interval, scrapes for seeders and leechers, and follows redirects.
- Stops automatically after a time, or when seeders, leechers, uploaded, downloaded or the
  leecher/seeder ratio crosses a limit.
- Answers incoming peer handshakes on the announced port so the torrent looks connectable.
- Connects directly or through an HTTP CONNECT, SOCKS4, SOCKS4a or SOCKS5 proxy, and supports
  `https` trackers.
- Keeps a per-torrent log and a record of every tracker exchange.
- Saves and loads sessions, and restores the last one on startup. Session files written by 0.43 still load.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build src/RatioMaster.slnx
dotnet test src/RatioMaster.slnx
dotnet run --project src/RatioMaster.App
```

To produce a single self-contained binary (the publish settings live in the project file):

```
dotnet publish src/RatioMaster.App -c Release -r win-x64    # or linux-x64, osx-arm64, ...
```

## Layout

| Project | What it holds |
|---|---|
| `src/RatioMaster.Core` | Everything except the interface: bencode, torrent files, client profiles, the tracker protocol, the proxy and TLS transport, and the session engine. |
| `src/RatioMaster.App` | The Avalonia interface. |
| `src/RatioMaster.Core.Tests` | Tests for the core, all of them offline. |

The 0.43 WinForms sources were removed in 1.0. They remain in the git history at tag `0.43` if you
need them. The emulations they defined are preserved byte for byte in
`src/RatioMaster.Core.Tests/Tracker/RequestGoldens.txt`, which pins the exact request every client
emulation sends; changing it changes how a tracker fingerprints your traffic.

## Adding a client emulation

Client emulations are data, not code. The built-in set is
`src/RatioMaster.Core/Clients/clients.json`. To add or change one without rebuilding, create a file
with the same shape at:

| System | Path |
|---|---|
| Windows | `%APPDATA%\RatioMaster.NET\clients.json` |
| Linux | `~/.config/RatioMaster.NET/clients.json` |
| macOS | `~/Library/Application Support/RatioMaster.NET/clients.json` |

An entry with a name that already exists replaces it; a new name is added. Settings shows the exact
path. One entry looks like this:

```json
{
  "clients": [
    {
      "name": "uTorrent 3.3.2", "family": "uTorrent", "version": "3.3.2",
      "httpProtocol": "HTTP/1.1", "hashUpperCase": false,
      "key":    { "type": "hex", "length": 8, "upperCase": true },
      "peerId": { "prefix": "-UT3320-%18w", "type": "random", "length": 10, "urlEncode": true },
      "headers": ["Host: {host}", "User-Agent: uTorrent/3320", "Accept-Encoding: gzip"],
      "query": "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1",
      "defaultNumWant": 200,
      "memoryScan": { "processName": "uTorrent", "searchString": "&peer_id=-UT3320-", "startOffset": 0, "maxOffset": 200000000 }
    }
  ]
}
```

`type` is `alphanumeric`, `numeric`, `hex` or `random`. The query placeholders are `{infohash}`,
`{peerid}`, `{port}`, `{uploaded}`, `{downloaded}`, `{left}`, `{event}`, `{numwant}`, `{key}` and
`{localip}`. Leave out `memoryScan` when the client's values cannot be read from its process.

## Where your data lives

Settings, the autosaved session and the optional client file all sit in the configuration folder shown
in Settings. Nothing is sent anywhere except the tracker requests you configure and, if you leave it
on, a version check against ratiomaster.net that reports only the version, operating system and
processor count.

## The website

`web/` holds ratiomaster.net, which runs on Cloudflare Workers. See `web/README.md`.

## Licence

MIT, see [LICENSE](LICENSE). The rewrite removed the GPL-licensed BytesRoad socket library that older
versions bundled; all networking is now plain .NET.
