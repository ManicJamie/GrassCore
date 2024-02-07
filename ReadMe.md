# GrassCore

A core mod allowing downstream mods to register grass cut events & prevent cut grass from reappearing.

Most of the logic, including the grass list & cut detection, have been lifted directly from GrassyKnight.

## Interface
In your `Initialize`, set one or more of the flags;
```cs
WeedKillerEnabled // Prevents cut grass from respawning.
DisconnectWeedKiller // Disconnects WeedKiller from the internal cut grass dict, allowing downstream mods to modify.
UniqueCutsEnabled // Invoke UniqueGrassWasCut when a new grass is cut.
CutsEnabled // Invoke GrassWasCut when grass is cut.
RawCutsEnabled // Invoke Raw_GrassWasCut when any grass-like object is cut.
```
Note that setting `UniqueCutsEnabled -> CutsEnabled -> RawCutsEnabled` will cause all upstream flags to be set.

Events are invoked under static `GrassEventDispatcher`, except for `GrassRegister_Global.OnStatsChanged`. All events under `GrassEventDispatcher` have the following delegate;
```cs
public delegate void GrassWasCut_EventHandler(GrassKey key);
```
GrassKey is a struct of SceneName, ObjectName and Position (as a Vector2).

## Todo

- [ ] Add unique detection for other grass-likes (think this might just be drapes, though implementing a version of GrassyKnight's LawnMower to verify wouldn't hurt)
- [ ] Add support for Breakable? maybe dividing dynamically by NonBouncer