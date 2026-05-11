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

## Session Documentation (Mandatory)

When the user says **"Documente esta sessão"** or after any significant architectural/feature change, create a session log document.

### When to Document
- New feature implementations
- Architectural changes (new scripts, refactors, system redesigns)
- Bug fixes that required non-trivial investigation
- Any change that modifies behavior described in the GDD
- When explicitly requested by the user

### Where to Save
`C:\Users\phyro\Documents\Vaults\Personal\4. Projetos\Slumber Pets\Logs\`

### File Naming
Format: `YYYY-MM-DD - Short Descriptive Title.md`
- Date format: `2026-05-11` (ISO 8601, no slashes in filename)
- Title: concise but descriptive (e.g., "Mobile Version Pet Location Awareness")
- Each log is a **new file** — never append to existing logs

### Document Structure (Markdown)
```markdown
# YYYY/MM/DD — Short Descriptive Title

## Motivação
- What problem was being solved
- Why this change was necessary
- Reference to GDD sections if applicable

## Decisões de Design
- Key decisions made and why
- Alternatives considered and rejected
- Justification for each decision

## Ações Tomadas
### Arquivos Criados
| Arquivo | Descrição |
|---|---|
| `path/to/file.cs` | What it does |

### Arquivos Modificados
| Arquivo | Alteração |
|---|---|
| `path/to/file.cs` | What changed |

### Detalhes das Alterações
- Code snippets for critical logic changes
- Flow descriptions for new systems
- Signal/connection changes

## Resultados Esperados
- Expected behavior after changes
- How to verify the change works
- Any known limitations or TODOs

## Referências ao GDD
- Section numbers and topics from the GDD that relate to this change
```

### Quality Checklist
- [ ] Motivation clearly states the problem
- [ ] All created/modified files are listed
- [ ] Critical code changes are documented with snippets
- [ ] Expected behavior is described
- [ ] GDD sections are referenced
- [ ] File saved in correct directory with correct naming format

### Vault Sync (Post-Documentation)
After creating the session log document, **immediately commit and push** to the Vault repository:

1. **Repository root:** `C:\Users\phyro\Documents\Vaults\Personal`
2. **Steps:**
   - `cd "C:\Users\phyro\Documents\Vaults\Personal"`
   - `git add -A`
   - `git commit -m "docs: <short description of the session log>"`
   - `git push`
3. **Commit message format:** `docs: YYYY-MM-DD — Short Title` (e.g., `docs: 2026-05-11 — Mobile Version Pet Location Awareness`)
4. **Verify:** `git status` should show clean working tree after push

### Game State Documentation (Mandatory — Post-Log)

After creating the session log and **before** the Vault Sync step, you must also document the current state of every game element, mechanic, system, or verb that was touched or is relevant to the session. This creates a living knowledge base inside the Obsidian vault.

#### Vault Root
`C:\Users\phyro\Documents\Vaults\Personal\4. Projetos\Slumber Pets\`

#### Contextual Folder Structure
Organize notes into contextual folders. Create folders as needed — the structure should grow organically:

| Pasta | Conteúdo | Exemplos |
|---|---|---|
| `Mecanicas/` | Mecânicas de gameplay — regras, fórmulas, loops | Sono, Caminhada, Economia, Renda Passiva |
| `Elementos/` | Entidades do jogo — pet, itens, atributos, recursos | Pet, Atributos, Moedas de Estrela, Comida |
| `Verbos/` | Ações do jogador — o que o player pode fazer | Alimentar, Brincar, Dormir, Passear, Transferir |
| `Sistemas/` | Sistemas técnicos — save, rede, persistência | SaveManager, LanDiscovery, PetTransferManager |
| `Cenas/` | Cenas e fluxos de UI — telas, navegação, estados | MainMobile, MainDesktop, SleepMode, PetSend |
| `Estado/` | Status de implementação — o que está feito, pendente, planejado | MVP Status, Roadmap v1.1, Pendências |

#### How to Document Each Element

For **every** mechanic, element, verb, system, or scene that was touched or is contextually relevant to the session:

1. **Check if a note already exists** — recursively search `C:\Users\phyro\Documents\Vaults\Personal\4. Projetos\Slumber Pets\` for a matching note
2. **If it exists** — update it with new information, status changes, or relationships
3. **If it doesn't exist** — create a new note in the appropriate contextual folder
4. **Always use Obsidian wikilinks** `[[]]` when referencing any other note that exists in the vault — never use plain text for something that has a dedicated note

#### Note Structure (Each Element)

```markdown
# Name of Element/Mechanic/System

## Intenção
- What this is and why it exists in the game
- Design goal or player experience it enables

## Como Funciona
- Rules, formulas, flow, or behavior
- Key implementation details (scripts, signals, data)

## Status
- **Implementado:** What's fully working
- **Parcial:** What's partially done (specify what's missing)
- **Pendente:** What's planned but not started
- **Bloqueado:** What's blocked and why (if applicable)

## Pendências
- [ ] Specific TODO item 1
- [ ] Specific TODO item 2
- [ ] Known issue or limitation

## Relacionados
- [[Note Name 1]]
- [[Note Name 2]]
- [[GDD Section Reference]]
```

#### Wikilink Rules

- **ALWAYS** search recursively in the vault before creating a new note — if a note exists, use `[[Note Name]]` instead of creating a duplicate
- Wikilinks should use the note filename without the `.md` extension
- If a note is inside a subfolder, use the full relative path: `[[Mecanicas/Sistema de Sono]]`
- When updating an existing note, add a `## Histórico de Alterações` section at the bottom with date and what changed

#### Example Workflow

1. Session touches the sleep system → check vault for existing sleep notes
2. Found `[[Mecanicas/Sistema de Sono]]` → update status, add new implementation details
3. Found `[[Elementos/Atributos]]` → add wikilink reference in the sleep note
4. No note exists for `[[Cenas/SleepModeScene]]` → create new note in `Cenas/` folder
5. All notes use `[[wikilinks]]` to cross-reference each other
6. After all element notes are created/updated, proceed to Vault Sync
