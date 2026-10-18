# Publish dependency locks

`Directory.Build.props` selects these locks when `RuntimeIdentifier` is set. A publish restore has one runtime target, whereas the pinned upstream Core lock contains both supported runtimes. Separate locks keep publishing reproducible without editing Core or its original lockfile.

The build tasks always restore in locked mode. To intentionally update publish dependencies after reviewing a package/toolchain change, regenerate both `linux-x64` and `win-x64` graphs:

```sh
mise exec -- dotnet restore src/Game.MonoGame/Game.MonoGame.csproj -p:RuntimeIdentifier=linux-x64 -p:SelfContained=true -p:RestoreLockedMode=false --force-evaluate
mise exec -- dotnet restore src/Game.Godot/Game.Godot.csproj -p:RuntimeIdentifier=linux-x64 -p:Configuration=ExportRelease -p:SelfContained=true -p:RestoreLockedMode=false --force-evaluate
# Repeat with RuntimeIdentifier=win-x64, then review every changed lock.
```

Use the explicit `RuntimeIdentifier` property here: `dotnet restore -r` sets a different restore property and does not select these lock paths. Godot's export configuration excludes the editor-only GodotSharpEditor binding. Its required legacy solution is generated into ignored `src/Game.Godot/.godot/`; `CommonWatch.slnx` remains the maintained repository solution.
