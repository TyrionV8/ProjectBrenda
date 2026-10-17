# Automated tests on Linux

Continuous integration (CI) runs the complete test suite on each `push` and
`pull_request` through `.github/workflows/tests.yml`. It uses Ubuntu, the .NET 8
Software Development Kit (SDK), and the official Godot 4.7.2 .NET archive. The
download is checked against the release's SHA-512 (Secure Hash Algorithm, 512-bit)
checksum list before extraction;
the extracted engine is cached. The job has a 20-minute limit for setup and tests.

Install .NET 8 SDK (or newer) and **Godot 4.7.2 .NET**, keeping the `GodotSharp`
directory alongside its executable. From the repository root, run:

```bash
# GODOT_BIN selects the executable for this command, including paths with spaces.
# bash executes the same script that GitHub Actions uses.
GODOT_BIN="/absolute/path/to/Godot_v4.7.2-stable_mono_linux.x86_64" \
  bash tools/ci/run-tests.sh
```

`GODOT_BIN` can also be an executable name available on `PATH` (the directories
searched for commands). It defaults to `godot` when unset. The script locates the
repository from its own location, so it can be called from another directory.

The script runs these steps in order:

1. `dotnet build` restores dependencies and compiles the C# project.
2. `godot --headless --path . --import` imports project resources. `--headless`
   runs without a graphical window, `--path .` selects the repository, and
   `--import` imports resources then exits.
3. `godot --headless --path . -- --test` runs the complete project test suite.
   The separator `--` passes `--test` to the game's test runner.

The first failed command stops the script and returns its exit code. A successful
run exits with `0` and the suite reports `N/N tests passed` (where `N` is the number
of tests). Imports and compiled outputs remain in the project's normal `.godot`
directory. NuGet package restoration uses the existing .NET configuration.

Every run creates exclusive temporary directories for `XDG_DATA_HOME`,
`XDG_CONFIG_HOME` and `XDG_CACHE_HOME`. These Linux environment variables follow
the X Desktop Group (XDG) directory specification and select
Godot's data, configuration and cache locations, including `user://` saves. Their
previous values are overridden only inside the script process; normal player
saves are preserved. On exit, the script removes only its own temporary directory
and preserves the build, import or test exit code. `HOME` is not changed.

Do not run multiple copies of the full suite at the same time on one machine:
network tests use local ports. Wait for another run to finish before starting.
The GitHub job compiles and tests only; it does not export or publish the game.

Official references: [Godot 4.7.2 release](https://github.com/godotengine/godot/releases/tag/4.7.2-stable),
[Godot data paths](https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html),
[.NET setup action](https://github.com/actions/setup-dotnet).
