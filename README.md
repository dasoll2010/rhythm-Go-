# rhythm Go! — Windows Edition

C# Windows Forms desktop prototype.

## Build locally (Windows)
```powershell
dotnet publish rhythm-Go.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./output
```

The executable is `output/rhythm-Go.exe`.

## GitHub Actions
Push to `main` or run **Actions → rhythm Go! Windows Build → Run workflow**.
Download the `rhythm-Go-Windows-x64` artifact.

## Controls
- Main: click `STAGE 1`
- Game: click to advance through 10 tiles
- Debug: type `thisdebug1`
- Editor: select/drag blocks; use the buttons on the left
- Key Viewer: press keyboard keys; `Esc` returns to main
