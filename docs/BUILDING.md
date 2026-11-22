# Building AeroMenu.dll

## Prebuilt DLL

`AeroMenu.dll` in the repository root was compiled against
**Among Us 2026.9.29 / BepInEx IL2CPP**. Copy it to `BepInEx/plugins/AeroMenu.dll`. If
your game version differs, rebuild using the instructions below.

## Building from source

AeroMenu is a BepInEx 6 / IL2CPP plugin that **references the game's own assemblies**
(`Assembly-CSharp.dll`, `UnityEngine.*.dll`, `BepInEx.Core.dll`, and ~100 more) from
`BepInEx/interop` and `BepInEx/core`. Those files are part of the Among Us installation
and cannot legally be redistributed in a public repository. Consequently a compiled
`AeroMenu.dll` is not committed here, and this environment (macOS, no .NET SDK, no game
files) cannot produce one.

## Build it yourself (2 minutes)

### Windows

1. Install [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Install BepInEx IL2CPP into your Among Us folder and launch the game once so
   `BepInEx/interop` is generated.
3. From a terminal in the repository root:

   ```powershell
   dotnet build .\AeroMenu.csproj -c Release -p:AmongUsDir="C:\Program Files (x86)\Steam\steamapps\common\Among Us"
   ```

4. Copy `bin\Release\net6.0\AeroMenu.dll` to `BepInEx\plugins\AeroMenu.dll`.

On macOS / Linux the repository also ships `build-dll.sh`, which does the same thing in
one command:

```bash
./build-dll.sh "/path/to/Among Us"
```

### Linux / Steam Deck

Use the same command with the Proton/Wine prefix path that contains `BepInEx`:

```bash
dotnet build AeroMenu.csproj -c Release -p:AmongUsDir="/path/to/Among Us"
```

### GitHub Actions

`docs/build-dll-workflow.yml (copy it to `.github/workflows/` to enable — GitHub tokens need `workflow` scope to add workflow files)` builds the DLL on a Windows runner and uploads it as
an artifact. You must first provide the BepInEx interop assemblies to the runner — for
example from a private artifact or repository secret — because they cannot be included
in this public repo.

## Troubleshooting

- **"Could not find Assembly-CSharp.dll"** — pass `-p:AmongUsDir=...` explicitly; the
  default paths in the `.csproj` only cover common Windows installs.
- **Method unstripping errors at runtime** — make sure you generated interop assemblies
  with the BepInEx version matching your game version.
