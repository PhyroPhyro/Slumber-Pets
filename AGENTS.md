# Slumber Pets — Agent Notes

## Project
- **Engine:** Godot 4.4 with C# (.NET 8.0, `Godot.NET.Sdk/4.4.1`)
- **Main scene:** `scenes/main_desktop.tscn` (desktop), `scenes/main_mobile.tscn` (mobile)
- **Viewport:** 320×180, `canvas_items` stretch, mobile renderer

## Developer Workflow
- Open `project.godot` in Godot 4.4 editor to run/build
- No lint, typecheck, or test commands — verify by running in editor
- `.godot/` is gitignored (editor cache); safe to delete and regenerate

## Architecture
- All scripts are **autoload singletons** registered in `[autoload]` of `project.godot`
- **Entry point:** `GameManager` — loads save, applies offline decay, sets pet location
- **Persistence:** `SaveManager` → `user://saves/save.json` (JSON via `System.Text.Json`)
- **Save version:** `CurrentVersion = 1` in `SaveManager.cs`; mismatch resets to default

### Directory layout
| Path | Domain |
|---|---|
| `scripts/core/` | GameManager, SaveManager, PetState, PetLocationManager |
| `scripts/pet/` | PetNeeds (hunger/energy/happiness decay) |
| `scripts/economy/` | ResourceManager, InventoryManager, ShopManager, IdleIncomeManager |
| `scripts/sleep/` | SleepTracker, AccelerometerReader |
| `scripts/walk/` | WalkTracker, StepCounter, MilestoneManager, ItemRoller |
| `scripts/network/` | LanDiscovery, SyncHost, SyncClient, PetTransferManager |
| `scripts/ui/` | Scene controllers (MainDesktop, MainMobile, ShopScene, etc.) |
| `scenes/` | `.tscn` files mirroring script domains |
| `data/` | Game balance/config JSON (items, balance, milestones) |

### Key autoload load order dependency
Scripts reference each other via `/root/<AutoloadName>`. If adding a new autoload that depends on an existing one, register it **after** its dependency in `project.godot` `[autoload]` section.

## Data Files
- `data/balance.json` — income rates, thresholds, tick interval (60s)
- `data/items.json` — item definitions
- `data/milestones.json` — walk milestone rewards
- These are loaded at runtime; changes require no rebuild

## Conventions
- Scripts use `using Godot;` and `using System;` at top
- No namespace declarations (global scope)
- Autoloads accessed via `GetNode<Type>("/root/<Name>")`
- Logging via `GD.Print()` / `GD.PrintErr()` with `[ClassName]` prefix
- UTF-8 charset (`.editorconfig`)

## Agent Workflow Rules
- **NEVER read the same file twice in a row** — context is retained across tool calls
- **Batch independent reads** — read multiple files in parallel when possible
- **Edit → verify once** — after editing, only re-read if the edit result is unclear or an error occurs
- **Avoid read loops** — if you find yourself reading the same files repeatedly, stop and act on what you already know
- **Scene scripts** — `.tscn` files without `[ext_resource type="Script"]` have no attached script; create one if behavior is needed
- **Button handlers** — buttons in `.tscn` without `Pressed` connections in code do nothing; always wire them in `_Ready()`
- **Signal flow** — `IdleIncomeManager` must use `ResourceManager.AddCoins()` (not `GameManager.AddStarCoins()` directly) so `OnStarCoinsChanged` fires and UI updates
- **Tracker initialization** — `SleepModeScene` and `WalkModeScene` must fetch their trackers from `/root/` and connect signals before calling `StartSleep()`/`StartWalk()`
