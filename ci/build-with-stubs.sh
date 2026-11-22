#!/usr/bin/env bash
# Compiles the plugin without the game installed: BepInEx and the Unity reference assemblies are downloaded (public),
# and ci/stubs stands in for the game's Assembly-CSharp. Used by CI; handy locally too.
set -euo pipefail
cd "$(dirname "$0")/.."

BEPINEX_VERSION="${BEPINEX_VERSION:-5.4.23.3}"
BEPINEX_SHA256="${BEPINEX_SHA256:-41a089e5b1b1f0713b331346baf6677b1184c69eabebf51101097954e854c749}"
UNITY_VERSION=2021.3.33
UNITY_SHA256=d32d34526d89958c63220d3a5ce26c5c44c11bfd411a2e1fbc3200088ef160c4

CACHE=ci/.cache
GAME=ci/.stubgame
mkdir -p "$CACHE" "$GAME/BepInEx/core" "$GAME/COMPOUND_Data/Managed"

fetch() { # url file sha256
  if [ ! -f "$2" ]; then curl -fsSL -o "$2" "$1"; fi
  echo "$3  $2" | shasum -a 256 -c - >/dev/null || { echo "checksum mismatch for $2" >&2; rm -f "$2"; exit 1; }
}

fetch "https://github.com/BepInEx/BepInEx/releases/download/v${BEPINEX_VERSION}/BepInEx_win_x64_${BEPINEX_VERSION}.zip" "$CACHE/bepinex.zip" "$BEPINEX_SHA256"
fetch "https://api.nuget.org/v3-flatcontainer/unityengine.modules/${UNITY_VERSION}/unityengine.modules.${UNITY_VERSION}.nupkg" "$CACHE/unity.nupkg" "$UNITY_SHA256"

rm -rf "$CACHE/x" && mkdir -p "$CACHE/x"
unzip -qo "$CACHE/bepinex.zip" 'BepInEx/core/BepInEx.dll' 'BepInEx/core/0Harmony.dll' -d "$CACHE/x"
unzip -qo "$CACHE/unity.nupkg" 'lib/net45/*' -d "$CACHE/x"
chmod -R u+rw "$CACHE/x"
cp "$CACHE"/x/BepInEx/core/*.dll "$GAME/BepInEx/core/"
cp "$CACHE"/x/lib/net45/UnityEngine*.dll "$GAME/COMPOUND_Data/Managed/"

MANAGED="$(pwd)/$GAME/COMPOUND_Data/Managed"
dotnet build ci/stubs -c Release -p:UnityDir="$MANAGED" -o "$MANAGED"
dotnet build src -c Release -p:GameDir="$(pwd)/$GAME" -o ci/.out
echo "plugin compiled against stubs: ci/.out/CompoundMP.dll"
