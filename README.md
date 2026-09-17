# Force Outfit Change

A RimWorld 1.6 mod that adds a button to the Assign tab so you can make colonists
change into their assigned apparel policy immediately, instead of waiting for them
to get round to it.

Unreleased. Not published to the Steam Workshop.

## What it does

- Adds a force-outfit-change button beside each colonist's apparel policy in the Assign tab.
- Wakes sleeping colonists and interrupts whatever they are doing.
- Supports click and drag across several colonists at once.
- Intended for switching the colony into battle gear when a raid lands.

## Layout

| Path | Contents |
| --- | --- |
| `1.6/Core/` | `PawnColumnWorker_ForceOutfitChange`, the marker component and the DefOf |
| `1.6/Jobs/` | `JobDriver_ForceWear`, `JobDriver_ForceRemoveApparel` |
| `1.6/Patches/` | Assign tab patch and the `JobGiver_OptimizeApparel` patch |
| `1.6/Defs/` | Job and PawnColumn defs |
| `1.6/Languages/` | Keyed strings in nine languages |

## Building

Requires the `RimWorldDir` environment variable pointing at the RimWorld install,
the .NET SDK, and Mono for the 4.7.2 reference assemblies.

```sh
./build.sh
```

That builds Release and installs the mod into `$RimWorldDir/Mods/ForceOutfitChange`.
Build output under `1.6/Assemblies/net472/` is not tracked in git.

## Licence

GPL-3.0. See [LICENSE](LICENSE).
