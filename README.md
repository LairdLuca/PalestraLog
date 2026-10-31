# PalestraLog

An Android app for logging gym workouts: training plans, days, exercises, sets with weight and reps, rest timer, super sets and session history. It works fully offline and all data stays on the phone.

> ⚠️ **Vibe-coded prototype.** I built this app for personal use, and most of it was written by an AI coding assistant (Claude Code) while I steered it and tested the result on my phone. It works and I use it, but it is not a polished product: treat it as inspiration or a starting point, not as a reference example.

## Before you use it

- **No automated tests.** It has only been tested by hand, on one Android phone.
- **Data lives only inside the app** (local SQLite). Uninstalling the app deletes everything, so export a backup from the Home screen before doing anything risky.
- **Built for Android.** It is a .NET MAUI project and also compiles for iOS, Mac Catalyst and Windows, but it has only been used and tested on Android.
- **The UI is in Italian only**, and so are the code comments.
- Some code comments refer to design documents (IDs like `AD-14`, the file `formato-backup.md`) that are not part of this repository.

## Stack

- .NET 10 / .NET MAUI
- MVVM with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) (source generators)
- SQLite via [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)
- SkiaSharp for a few custom-drawn controls

## Building

You need the .NET 10 SDK with the MAUI workload (`dotnet workload install maui-android`).

```bash
# Windows / macOS
dotnet build src/PalestraLog/PalestraLog/PalestraLog.csproj -f net10.0-android

# Linux: restore also evaluates the iOS/MacCatalyst targets and fails (NETSDK1178),
# so restrict it to Android too
dotnet build src/PalestraLog/PalestraLog/PalestraLog.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android
```

To install it on a connected device, run `dotnet build -t:Run -f net10.0-android` on the same project, or press F5 in Visual Studio.

## License

[MIT](LICENSE)

<p align="right">Happy Coding! 🐱</p>
