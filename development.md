# Prioritize — development reference

Source of truth for architecture, naming, and where to change things. Read this before planning or editing the mod.

## Glossary

| Term | Meaning |
|------|---------|
| **Thing priority** | Per-thing numeric priority stored in `PSaveData.ThingPriority`. **Runtime source of truth** for job scoring. |
| **Cell priority** | Per-tile painted priority in `PriorityMapData`. **Authoring input only** — synced onto things, not read during job search. |
| **Manual priority** | Set via `Designator_Priority_Thing`. Always wins over cell sync. |
| **Cell-sourced priority** | Thing priority applied by `CellToThingSync` from a painted cell. Tracked in `CellSourcedThingIds`. |
| **Scanner group** | Category used to gate Harmony patches per work giver: Construction, Hauling, Mining, Growing, Cleaning, Repair, SmoothFloor, Deconstruct (`PriorityScannerGroup`). |
| **Per-scanner gate** | `PriorityWorkIndex.HasPrioritiesFor(workGiver, map)` — mod prioritized search runs only when relevant things exist for that scanner. |
| **Unsafe patches** | Optional `GenClosest` `priorityGetter` injection (`UseUnsafePatches` setting). Broader effect, higher mod-conflict risk. |
| **Auto Priority Above Cells** | Player-facing name for the cell designator (internal: `Designator_Priority_Cell`, def `Priortize_Cell`). |

### RimWorld terms (relevant to this mod)

| Term | Meaning |
|------|---------|
| **WorkGiver** | RimWorld class that finds a target and creates a job for one work type (e.g. `WorkGiver_ConstructFinishFrames`). |
| **Work type** | Colonist work-tab category (Construction, Hauling, Mining, …). Multiple WorkGivers can share one work type. |
| **Prioritized search** | When `WorkGiver_Scanner.Prioritized == true`, RimWorld uses `GenClosest` with distance + priority scoring instead of a simpler closest/first scan. |

---

## Architecture

Two storage layers, one runtime path:

```
Player input
  ├─ Thing designator ──► PSaveData.ThingPriority  (runtime source of truth)
  └─ Cell designator  ──► PriorityMapData grid     (authoring / overlay only)
                              │
                              ▼ CellToThingSync (periodic + optional immediate)
                         PSaveData.ThingPriority  (cell-sourced entries)
                              │
                              ▼ PriorityWorkIndex (per-map, per-scanner-group counts)
                         Harmony patches (GetPriority, Prioritized, GenClosest)
```

### Data stores

All job-scoring priority data flows through **two stores**:

1. **`PSaveData`** (`GameComponent`) — thing priority dict, cell-sourced tracking, save/load
2. **`PriorityMapData`** (`MapComponent`) — per-cell priority grid for painting and sync input

Do not introduce a third store for priority metrics without explicit discussion.

Supporting types (not separate stores):

- **`PriorityWorkIndex`** — derived index rebuilt on priority changes; gates patches per scanner group
- **`PriorityState`** — fast check: `ThingPriority.Count > 0`
- **`CellToThingSync`** — propagates cell grid → thing dict

### Job selection path

The mod hooks **below** `JobGiver_Work` / think tree — at `WorkGiver_Scanner` and `GenClosest`:

1. **`WorkGiver_Scanner_Prioritized`** — sets `Prioritized = true` only when `PriorityWorkIndex` has relevant prioritized things for that scanner (not globally).
2. **`WorkGiver_Scanner_GetPriority`** — postfix adds thing priority to RimWorld's combined distance + priority score.
3. **`GenClosest_RegionalPrioritizedSearch`** — redirects reachable global search to regionwise BFS (~24–30 regions) when vanilla already uses a `priorityGetter`.
4. **`GenClosest_*` unsafe patches** — optional wrapper around `priorityGetter` when `UseUnsafePatches` is enabled.

Selection model: **keep vanilla combined distance + priority scoring**. No tier-first "pick highest priority, then closest" mode.

### Cell → thing sync

| Policy | Rule |
|--------|------|
| Manual wins | If thing has manual priority, sync skips it |
| Sticky on move | Auto-synced priority stays on thing after it leaves painted cells |
| Cell cleared | Removes cell-sourced priority from things still on that cell |
| Timing | `CellSyncMode`: `PeriodicOnly` or `ImmediateAndPeriodic` (default); interval default 600 ticks |

---

## Source layout

```
Source/Prioritize/
├── MainMod.cs                 # Harmony bootstrap, globals, overlay colors, Ctrl+scroll
├── PrioritizeMod.cs           # Mod entry, settings UI
├── PrioritizeSettings.cs      # ModSettings (unsafe patches, invert priority, sync mode)
├── PDefOf.cs                  # DefOf for designation defs
│
├── PSaveData.cs               # GameComponent: ThingPriority dict, CellSourcedThingIds
├── PriorityMapData.cs         # MapComponent: cell grid, prioritized cell index
├── PriorityState.cs           # HasActivePriorities()
├── PriorityWorkIndex.cs       # Per-scanner-group counts, thing classification
├── PriorityScannerGroup.cs    # Scanner group enum
├── CellToThingSync.cs         # Cell grid → thing dict sync
├── CellSyncMode.cs            # Sync mode enum
├── PriorityUtils.cs           # GetPriority(Thing) with optional sign inversion
├── PriorityGameComponent.cs   # Tick (sync, cleanup), OnGUI (overlay labels)
│
├── Designator_Priority_Thing.cs
├── Designator_Priority_Cell.cs
├── Designator_PrioritySettings.cs
├── Designator_PriorityPresets.cs  # Negative / 0 / positive preset architect buttons
├── PriorityPresetMenu.cs
├── PriorityShowConditions.cs  # Thing filter presets for thing designator
├── PriorityDrawMode.cs        # Overlay: None / Thing / Cell
│
├── Workgiver_UniversalConstruct.cs   # Merged construction WorkGiver
└── HarmonyPatches/
    ├── WorkGiver_Scanner_GetPriority.cs
    ├── WorkGiver_Scanner_Prioritized.cs
    ├── GenClosest_RegionalPrioritizedSearch.cs
    ├── GenClosest_ClosestThing_Global.cs          # unsafe only
    ├── GenClosest_ClosestThing_Global_Reachable.cs  # unsafe only
    ├── GenClosest_RegionwiseBFSWorker.cs          # unsafe only
    ├── Game_FinalizeInit.cs
    ├── Thing_Destroy.cs
    ├── Designation_Notify_Removing.cs
    ├── Frame_FailConstruction.cs
    ├── Blueprint_TryReplaceWithSolidThing.cs
    └── PlaySettings_DoPlaySettingsGlobalControls.cs

1.6/Defs/
├── Designations/Designations.xml       # Architect tab, Priortize_Cell / Priortize_Thing
└── WorkGiverDef/ConstructUniversal.xml # Universal construction WorkGiver

Languages/*/Keyed/          # UI strings (English, Korean, …)
```

---

## UI navigation

### Architect tab (designation order)

Settings → −preset → 0 → +preset → Thing Priority → Auto Priority Above Cells

| Designator | Action |
|------------|--------|
| **Priority Settings** | Thing filter menu (blueprints, haulables, designations, …); highlights matching things on the map while selected |
| **Negative preset** | Left-click applies negative preset (default −3); right-click picks −1…−5 (saved in mod settings) |
| **0** | Sets selected priority to 0 |
| **Positive preset** | Left-click applies positive preset (default +3); right-click picks +1…+5 (saved in mod settings) |
| **Thing Priority** | Click/drag things to assign priority |
| **Auto Priority Above Cells** | Paint cells; values sync to things on those tiles |

### Priority preset buttons

Three architect-tab buttons (not a popup): **negative preset** (default −3), **0**, **positive preset** (default +3). Numbers are drawn on the button icon. Right-click negative/positive to choose ±1–±5 for that slot (persisted in mod settings).

### Controls

- **Ctrl + mouse wheel** while a priority designator is active: increase/decrease selected priority (-32766 … 32768).
- **Play-settings bar** (bottom): overlay toggle — None / Cell / Thing priority labels on map.

### Mod settings (`PrioritizeMod`)

- `UseUnsafePatches` — enable optional `GenClosest` patches
- `UseLowerAsHighPriority` — invert priority sign for scoring and overlay colors
- `CellSyncMode` — `PeriodicOnly` vs `ImmediateAndPeriodic`
- `CellSyncIntervalTicks` — default 600
- `NegativePreset` / `PositivePreset` — Set Priority dialog preset slots (defaults −3 / +3)

---

## Data persistence

### Game save (`PSaveData`)

| Field | Scribe key | Content |
|-------|------------|---------|
| `ThingPriority` | `thingPriority` | `thingIDNumber → priority` |
| `CellSourcedThingIds` | `cellSourcedThingIds` | IDs whose priority came from cell sync |

Post-load: `ClearUnusedThingPriority()`, rebuild `PriorityWorkIndex`, run `CellToThingSync.SyncAllMaps()`.

### Per-map save (`PriorityMapData`)

| Field | Scribe key | Content |
|-------|------------|---------|
| Cell grid | `priorityGrid` | `ushort[]` (stored value = priority + 32768) |
| `numCells` | `numCells` | Grid size |

Post-load: recount non-zero cells, rebuild `prioritizedCellIndices`.

### Mod config (`PrioritizeSettings`)

Separate from save games — unsafe patches, invert priority, sync mode/interval.

### Lifecycle hooks

| Event | Handler | Action |
|-------|---------|--------|
| `Game.FinalizeInit` | `Game_FinalizeInit` | Create `PSaveData` if missing; cleanup; full cell sync |
| `Thing.Destroy` | `Thing_Destroy` | Queue thing ID for deferred dict removal |
| `Designation.Notify_Removing` | `Designation_Notify_Removing` | Clear thing priority |
| `Frame.FailConstruction` | `Frame_FailConstruction` | Copy priority to respawned blueprint |
| `Blueprint.TryReplaceWithSolidThing` | `Blueprint_TryReplaceWithSolidThing` | Copy priority to finished building |
| Tick | `PriorityGameComponent` | Periodic sync; flush destroyed-thing cleanup batch |

---

## Harmony patches

| Patch | Target | When active | Effect |
|-------|--------|-------------|--------|
| `WorkGiver_Scanner_GetPriority` | `GetPriority` | Thing priorities exist + per-scanner gate | Adds thing priority to score |
| `WorkGiver_Scanner_Prioritized` | `Prioritized` getter | Per-scanner gate | Enables prioritized search for relevant scanners only |
| `GenClosest_RegionalPrioritizedSearch` | `ClosestThing_Global_Reachable` | Gate open + vanilla uses `priorityGetter` | Regionwise BFS instead of full-map scan |
| `GenClosest_ClosestThing_Global` | `ClosestThing_Global` | `UseUnsafePatches` | Wraps `priorityGetter` |
| `GenClosest_ClosestThing_Global_Reachable` | `ClosestThing_Global_Reachable` | `UseUnsafePatches` | Same |
| `GenClosest_RegionwiseBFSWorker` | `RegionwiseBFSWorker` | `UseUnsafePatches` | Same |
| `Game_FinalizeInit` | `Game.FinalizeInit` | Always | Init save data, sync |
| `Thing_Destroy` | `Thing.Destroy` | Always | Deferred cleanup |
| `Designation_Notify_Removing` | `Designation.Notify_Removing` | Always | Clear priority |
| `Frame_FailConstruction` | `Frame.FailConstruction` | Always | Preserve priority on fail |
| `Blueprint_TryReplaceWithSolidThing` | `Blueprint.TryReplaceWithSolidThing` | Always | Transfer to built thing |
| `PlaySettings_DoPlaySettingsGlobalControls` | Play-settings bar | Always | Overlay toggle button |

**Note:** `GenClosest_ClosestThing_Global_Reachable` and `GenClosest_RegionalPrioritizedSearch` both target the same method; Harmony order determines which runs first.

---

## Naming

### Player-facing / UI copy

- Use **Auto Priority Above Cells** for the cell designator (not "Cell Priority").
- Use **Thing Priority** for per-thing assignment.

### Code / save compatibility

Keep legacy internal names — do not rename unless explicitly asked:

- Def names: `Priortize_Cell`, `Priortize_Thing` (typo preserved for save compat)
- Classes: `Designator_Priority_Cell`, `PriorityMapData`, `PSaveData`

---

## Scanner groups and classification

`PriorityWorkIndex.ClassifyThing` maps things to scanner groups:

| Group | Typical things |
|-------|----------------|
| Construction | Blueprints, frames, construct designations |
| Hauling | Haulable items |
| Mining | Mine designations |
| Growing | Cut/harvest designations, plants |
| Cleaning | Filth |
| Repair | Damaged buildings |
| SmoothFloor | Smooth floor designations |
| Deconstruct | Deconstruct/remove designations |

Gate rule: **conservative** — open if any non-zero thing priority exists for that scanner's domain. Never close the gate when prioritized work exists for that scanner type.

---

## Known gaps

1. **Missing component XML defs** — `PSaveData`, `PriorityGameComponent`, `PriorityMapData` have no `GameComponentDef`/`MapComponentDef` in `1.6/Defs/`; `Game_FinalizeInit` creates `PSaveData` as fallback.
2. **Cell-only paint** — no runtime effect until sync populates `ThingPriority`.
3. **`MapsWithCellPriorities`** — tracked in `PriorityMapData` but not used in runtime gates.

---

## When unsure

Search or read the referenced source files above rather than guessing RimWorld or mod behavior. Internal planning notes live in `.cursor/plans/`.
