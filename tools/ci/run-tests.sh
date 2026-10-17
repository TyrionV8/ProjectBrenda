#!/usr/bin/env bash
# Linux entry point shared by local runs and GitHub Actions.
set -euo pipefail

project_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
godot_command=${GODOT_BIN:-godot}
if ! godot_bin=$(command -v -- "$godot_command"); then
    printf 'Godot executable not found: %s\nSet GODOT_BIN to the Godot 4.7.2 .NET executable.\n' "$godot_command" >&2
    exit 127
fi
# Resolve a relative executable before changing into the project directory.
godot_bin=$(realpath -- "$godot_bin")
if [[ ! -x "$godot_bin" ]]; then
    printf 'Godot is not executable: %s\n' "$godot_bin" >&2
    exit 126
fi
if ! command -v dotnet >/dev/null; then
    printf '.NET SDK not found. Install .NET 8 SDK or newer.\n' >&2
    exit 127
fi

# Tests intentionally write saves. Never inherit the player's Godot directories.
test_user_root=$(mktemp -d /tmp/hardware-defence-tests.XXXXXXXX)
cleanup() {
    local status=$?
    trap - EXIT
    rm -rf -- "$test_user_root" || printf 'Could not remove test directory: %s\n' "$test_user_root" >&2
    exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
export XDG_DATA_HOME="$test_user_root/data"
export XDG_CONFIG_HOME="$test_user_root/config"
export XDG_CACHE_HOME="$test_user_root/cache"
mkdir -p -- "$XDG_DATA_HOME" "$XDG_CONFIG_HOME" "$XDG_CACHE_HOME"

cd -- "$project_dir"
printf '\n== Compile C# ==\n'
dotnet build
printf '\n== Import Godot resources ==\n'
"$godot_bin" --headless --path . --import
printf '\n== Run the complete test suite ==\n'
"$godot_bin" --headless --path . -- --test
