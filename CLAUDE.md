# ForceOutfitChange

Adds a button column to the Assign tab that makes a colonist change into their assigned apparel policy
now, waking them and interrupting what they are doing. Dropped apparel is left on the floor rather than
hauled. Unreleased, 771 lines, recovered from `$RimWorldDir/Mods/ForceOutfitChange` and committed for
the first time on 2026-09-17. The code has never been reviewed or play-tested since recovery, so treat
everything below as a description of what the source says, not a statement that it works.

Harmony id `com.zei33.forceoutfitchange`, namespace `ForceOutfitChange`.

## Types and patches

| File | Type | Role |
| --- | --- | --- |
| `1.6/Core/PawnColumnWorker_ForceOutfitChange.cs` | `PawnColumnWorker` | Draws the button, drag-paint, kicks off the change |
| `1.6/Core/ForceOutfitChangeMarker.cs` | static | `HashSet<int>` of `thingIDNumber`, marks a pawn for one interception |
| `1.6/Core/ForceOutfitChangeDefOf.cs` | `[DefOf]` | `ForceRemoveApparel`, `ForceWear`, `ForceOutfitChangeButton` |
| `1.6/Patches/AssignTabPatch.cs` | `[StaticConstructorOnStartup]` | Inserts the column def into `PawnTableDefOf.Assign.columns` after `Outfit` |
| `1.6/Patches/JobGiver_OptimizeApparel_Patch.cs` | Harmony postfix on `JobGiver_OptimizeApparel.TryGiveJob` | Swaps `RemoveApparel`/`Wear` for the mod's job defs when the pawn is marked |
| `1.6/Jobs/JobDriver_ForceRemoveApparel.cs` | `JobDriver` | Wait, then `TryDrop` and unforbid |
| `1.6/Jobs/JobDriver_ForceWear.cs` | `JobDriver` | Copy of vanilla Wear, drops conflicting apparel instead of queueing hauls |

Defs: `1.6/Defs/JobDefs/Jobs_ForceOutfitChange.xml`, `1.6/Defs/PawnColumnDefs/PawnColumns_ForceOutfitChange.xml`.

## Surprising in the code

- `PawnColumnWorker_ForceOutfitChange.cs:143` calls `Delay.AfterNTicks(1, ...)`. `Delay` is a real
  vanilla type in the global namespace (no `using`), and it is a Unity coroutine that yields on
  `WaitUntil(GenTicks.TicksGame >= target)`. Game ticks do not advance while paused, so a click made
  while paused generates nothing until the player unpauses.
- The same method reflects into `JobGiver_OptimizeApparel.TryGiveJob` (`protected override`) and loops up
  to ten times, calling `TryTakeOrderedJob` on each result. Reading the marker consumes it
  (`ForceOutfitChangeMarker.cs:45`), and the postfix runs on every one of those reflected calls, so only
  the first job of the batch is swapped for the mod's dropping version.
- The marker set is static and only cleared in the mod constructor, that is once per game launch, and it
  is keyed on `thingIDNumber`, which is reused across saves.
- The button icon is `TexButton.Reload`, which is `UI/Buttons/Dev/Reload`, a dev toolbar texture.
- `IsEssentialJob` (`:126`) has its real list commented out and now only protects `Rescue` and
  `FleeAndCower`. Firefighting, tending and self-extinguish are interruptible.
- Drag-paint state is a static `List<Pawn>` and the `MouseUp` branch runs inside `DoCell`, so it fires
  from whichever row happens to be under the cursor.

## Pending decision: About.xml breaks house convention

`About/About.xml` says packageId `ForceOutfitChange.ForceOutfitChange` and author "ForceOutfitChange Mod
Team", and has no `<modVersion>`. Convention is `Zei33.ForceOutfitChange`, author Zei33, modVersion
present. It was left exactly as recovered rather than silently normalised. Changing a packageId breaks
save compatibility for anyone already running the mod, so decide before publishing, not after.

## Build

```sh
export FrameworkPathOverride=/opt/homebrew/opt/mono/lib/mono/4.7.2-api
dotnet build rimworld-force-outfit-change.sln -c Release
```

`./build.sh` builds Release then deletes and replaces `$RimWorldDir/Mods/ForceOutfitChange`, which is
where the recovered copy still lives. Destructive to that folder.

## Unfinished

- Nine `ForceOutfitChange_Keys.xml` files exist and are genuinely translated, but English uses literal
  `\n` in the tooltips while German and the others use real line breaks. Pick one.
- Nine empty `ModTemplate_Keys.xml` files are still there from the scaffold, as is `Documentation/`,
  which still reads "Mod Template".
- `ForceOutfitChange_Forced` is only shown under `Prefs.DevMode`.
- No Workshop id, no preview work, no in-game test recorded.
