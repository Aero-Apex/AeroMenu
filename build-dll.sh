#!/usr/bin/env bash
# Build AeroMenu.dll against your Among Us / BepInEx installation.
# Usage: ./build-dll.sh "/path/to/Among Us"
set -euo pipefail

if [ "$#" -lt 1 ]; then
  echo "Usage: $0 <AmongUsDir>   # folder containing BepInEx/interop and BepInEx/core"
  exit 1
fi

AMONG_US_DIR="$1"
ROOT="$(cd "$(dirname "$0")" && pwd)"

if [ ! -f "$AMONG_US_DIR/BepInEx/interop/Assembly-CSharp.dll" ]; then
  echo "ERROR: missing $AMONG_US_DIR/BepInEx/interop/Assembly-CSharp.dll" >&2
  echo "Install BepInEx IL2CPP and launch the game once to generate the interop assemblies." >&2
  exit 1
fi

DOTNET="${DOTNET:-dotnet}"
if ! command -v "$DOTNET" >/dev/null 2>&1; then
  echo "ERROR: .NET 6 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/6.0" >&2
  exit 1
fi

"$DOTNET" build "$ROOT/AeroMenu.csproj" -c Release -p:AmongUsDir="$AMONG_US_DIR"

DLL="$ROOT/bin/Release/net6.0/AeroMenu.dll"
if [ -f "$DLL" ]; then
  echo ""
  echo "BUILD OK: $DLL"
  echo "Copy it to: $AMONG_US_DIR/BepInEx/plugins/AeroMenu.dll"
fi
