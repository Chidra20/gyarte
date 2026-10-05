# Demo Game Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `Demo` scene that plays the full loop: random layout → waves → key → gate → next random layout, with ability choices every 2nd level, attack cooldowns shown in a HUD corner, player health, and death back to the Start Menu.

**Architecture:** One `Demo` scene rebuilds itself per level through the existing `LevelRandomizer`; a `GameLoop` component drives the state. A general `Health` component is shared by the player and the slimes (through `EnemyHealth`). Logic that can be pure (wave math, spawn rules, farthest room, ability draw, cooldown text) lives in static methods covered by EditMode tests; scene behaviour is checked in Play mode through the Unity MCP.

**Tech Stack:** Unity 6000.5.2f1, URP 2D, new Input System, UGUI, Unity Test Framework 1.7 (EditMode, NUnit), unity-mcp for editor checks.

**Spec:** `docs/superpowers/specs/2026-10-05-demo-game-loop-design.md`. Read it first; this plan argues from it.

## Global Constraints

- Scripts go in `Assets/Prefabs/Scripts/` (slime ones in `Assets/Prefabs/Enemies/Slime/Scripts/`, abilities in `Assets/Prefabs/Abilities/`). No namespaces. `[Header]` and `[Tooltip]` on public fields, short comments that explain why.
- Unity 6 API: `Rigidbody2D.linearVelocity`, `FindAnyObjectByType`, `GetEntityId()` (never `GetInstanceID()`/`FindFirstObjectByType`).
- **Do not commit.** The project rule is to commit only when the user asks. Each task ends with a checkpoint instead.
- **Editor state belongs to the user:** check whether Play mode is running before touching a scene; say when Play mode starts or stops. Don't modify `testing the new thing` or `Test AI enemy` except where a task says so (adding the attack corner/HP label to `testing the new thing` is allowed by the spec).
- Asset moves and renames go through Unity (`AssetDatabase`) so `.meta` files survive. `EnemyHeatlh.cs` keeps its file name.
- Every new feature gets its controls in `TestMenu.cs` in the same task.
- Tests live in `Assembly-CSharp-Editor` and cannot see `internal` members of the gameplay scripts, so test hooks (clock, death override, registry reset, `TryFindSpawn`, `Awake`) are `public`, with a `[Tooltip]`/comment saying they are for tests.
- Per-feature pieces (health, contact damage, cooldowns, attack corner) must work without a `GameLoop` (in `testing the new thing`).
- The key is a **hollow red square**.
- Defaults from the spec: abilities every **2** levels; 3 cards; Vitality **+2 max, heal 2**; Quick Scythe **cooldown ×0.8**; Deep Reserves **+1 charge**; Fleet Foot **speed multiplier +0.15**.
- The vault is part of the work: each task updates the system note it touches (see the spec's "Vault updates"). Notes explain how and why and leave exact values to the code.

## How to run tests (used by every task)

Task 1 creates `Assets/Tests/Editor/TestRunLogger.cs`. To run a test class, call it through `mcp__unity-mcp__Unity_RunCommand`:

```csharp
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result) { TestRunLogger.Run("HealthTests"); }
}
```

then read `mcp__unity-mcp__Unity_GetConsoleLogs`. The logger prints one line per failure and a final `TESTS HealthTests: P passed, F failed`. "PASS" below means `F` is 0 and `P` equals the number of tests in the class; "FAIL" means the class doesn't compile or `F > 0`. After any script change, wait for the recompile to finish (the console shows no compile errors) before running.

Play-mode checks also go through `RunCommand` (enter Play mode, call methods, log, read the console). Ask before entering Play mode.

## Review Focus

1. **Dying at a busy moment.** If the player dies while the choice screen is open, or on the frame the gate is entered, the death screen wins and the next level is not built. Covered: Task 9, `GameLoop_DeathDuringChoosing`.
2. **Splits and merges never count as a clear.** A big slime splitting, or two babies merging, must never make the alive count reach 0 for a frame. Covered: Task 2, `Registry_SplitKeepsCountAboveZero`.
3. **Waves that can't spawn.** In a tiny level, or with tight spacing, a wave may spawn short or empty. An empty wave counts as cleared and the loop moves on instead of stalling. Covered: Task 7, `SpawnRules_GivesUpAfterAttempts` and Play check 7b.
4. **Frozen Start Menu.** After death the Start Menu must load with time scale 1, and Escape must not open the pause menu over the death or choice screen. Covered: Task 9, Play checks 9c and 9d.
5. **Abilities stacking past the limit or running out.** The choice screen never offers an ability at `maxStacks`, and with fewer than 3 left it shows fewer cards, or skips the screen when none are left. Covered: Task 6, `Draw_SkipsMaxedAndShortensHand`.

---

### Task 1: Test harness and `Health`

**Files:**
- Create: `Assets/Tests/Editor/TestRunLogger.cs`, `Assets/Tests/Editor/HealthTests.cs`
- Create: `Assets/Prefabs/Scripts/Health.cs`

**Interfaces:**
- Produces: `TestRunLogger.Run(string testClassName)`, which runs EditMode tests whose full name matches the class name and logs failures plus `TESTS <name>: P passed, F failed`.
- Produces: `class Health : MonoBehaviour` with
  - `public int maxHealth = 3; public float invulnerableTime = 0f;`
  - `int CurrentHealth {get;}`, `bool IsDead {get;}`, `bool IsInvulnerable {get;}`, `bool GodMode {get;set;}`
  - `bool TakeDamage(int amount)`: returns true if the damage was applied
  - `void Heal(int amount)` (capped at max), `void SetMaxHealth(int amount, bool refill)` (min 1)
  - `event Action<int,int> Changed` (current, max); `event Action Damaged`; `event Action Died`
  - Health is set in `Awake`. Invulnerability is measured with `Time.time`, so the tests use an injectable clock: `public Func<float> clock = () => Time.time;`

- [ ] **Step 1: Write `TestRunLogger`** using `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` with `Filter { testMode = TestMode.EditMode, groupNames = new[] { name } }` and an `ICallbacks` whose `RunFinished` logs the totals and whose `TestFinished` logs each failed leaf test's name and message.
- [ ] **Step 2: Write the failing tests** in `HealthTests`. Each test creates a `GameObject`, adds `Health`, sets its fields, then calls `Awake` itself, because EditMode doesn't call it. Make `Awake` `public` so the tests can call it directly. Destroy the object in `TearDown`:
  - `StartsFull`: max 3 → `CurrentHealth == 3`.
  - `DamageLowersAndRaisesChanged`: `TakeDamage(1)` → 2, `Changed` fired with (2,3).
  - `DiesOnceAtZero`: two `TakeDamage(5)` calls → `Died` fired exactly once, `IsDead`, and the second call returns false.
  - `InvulnerableWindowBlocksHits`: `invulnerableTime = 1`, clock at 0, hit (true); clock 0.5, hit (false, health unchanged); clock 1.01, hit (true).
  - `GodModeBlocksDamage`: `GodMode = true` → `TakeDamage` false, health unchanged.
  - `HealCapsAtMax` and `SetMaxHealthRefills`: `SetMaxHealth(5, true)` → 5/5; `SetMaxHealth(0, false)` → max 1.
- [ ] **Step 3: Run `TestRunLogger.Run("HealthTests")`.** Expected: FAIL (`Health` does not exist). **If the failure is instead that `NUnit`/`TestRunnerApi` cannot be found in `Assets/Tests/Editor`**, the predefined editor assembly isn't getting the test framework. Add `Assets/Tests/Editor/Tests.Editor.asmdef` (Editor only, references `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `optionalUnityReferences: TestAssemblies`). Because asmdefs can't reference `Assembly-CSharp`, also add `Assets/Prefabs/Gameplay.asmdef` covering the gameplay scripts **only if** every script under `Assets/Prefabs/` compiles inside it (it depends on `Unity.InputSystem`, `Unity.Cinemachine`, `Unity.TextMeshPro`/UGUI, `Unity.RenderPipelines.Universal.2D.Runtime`). Stop and report to the user before going this way; it changes the project structure.
- [ ] **Step 4: Implement `Health`.**
- [ ] **Step 5: Run `TestRunLogger.Run("HealthTests")`.** Expected: PASS, 7 tests.
- [ ] **Step 6: Checkpoint.** Console clean, no commit.

---

### Task 2: `EnemyHealth` on top of `Health`, with an alive registry

**Files:**
- Modify: `Assets/Prefabs/Enemies/Slime/Scripts/EnemyHeatlh.cs` (whole file)
- Modify: `BigSlime.prefab`, `BabySlime.prefab` (add `Health`, move each prefab's `maxHealth` value onto it: big 2, baby 1)
- Modify: `TestMenu.cs` `DrawEnemies` (show "Alive enemies: N")
- Test: `Assets/Tests/Editor/EnemyRegistryTests.cs`

**Interfaces:**
- Consumes: `Health` (Task 1).
- Produces: `[RequireComponent(typeof(Health))] class EnemyHealth`
  - keeps `public void TakeDamage(int damage)` and `public void SetMaxHealth(int amount)` (both delegate to `Health`), the hit flash, `Alert()` on a non-lethal hit, `DieAndSplit()`/`Destroy` on `Health.Died`
  - `static int AliveCount {get;}`, `static IReadOnlyCollection<EnemyHealth> Alive {get;}`, `static event Action AliveCountChanged`
  - Registers in `Awake` (not `OnEnable`, so a merged slime counts before its babies are destroyed). Unregisters on death **after** death handling has spawned replacements, and in `OnDestroy`. Unregistering twice is harmless.
- Removes the per-hit `Debug.Log` (a cleanup item in the Roadmap).

- [ ] **Step 1: Write the failing tests:**
  - `Registry_CountsAddAndRemove`: create 2 objects with `EnemyHealth` (call `Awake`) → `AliveCount == 2`; `DestroyImmediate` one → 1.
  - `Registry_SplitKeepsCountAboveZero`: subscribe to `AliveCountChanged` and record the minimum count seen. Kill an `EnemyHealth` whose `Died` handler is replaced by a test hook that creates two new `EnemyHealth` objects (use `public Action onDeathOverride`). The minimum seen must never be 0.
  - `TakeDamage_DelegatesToHealth`: `TakeDamage(1)` on max 2 → `Health.CurrentHealth == 1`.
  - `TearDown` destroys everything and calls `public static void ResetRegistry()`.
- [ ] **Step 2: Run `TestRunLogger.Run("EnemyRegistryTests")`.** Expected: FAIL.
- [ ] **Step 3: Implement the rework** and update both prefabs through `PrefabUtility` (RunCommand) or the Inspector. Check that the Test Menu's spawn-with-health still uses `SetMaxHealth`.
- [ ] **Step 4: Run `EnemyRegistryTests` and `HealthTests`.** Expected: PASS, 3 and 7.
- [ ] **Step 5: Play check 2a** (ask first; `Test AI enemy`): two swings on a big slime → split into 2 babies. Log `AliveCount` before (1), after the split (2) and after the merge (1).
- [ ] **Step 6: Checkpoint.** Update `Combat.md` (EnemyHealth now wraps Health, registry) and `Slime Enemy.md`.

---

### Task 3: Player health, contact damage, knockback, no more shoving

**Files:**
- Create: `Assets/Prefabs/Enemies/Slime/Scripts/ContactDamage.cs`
- Modify: `PlayerMovement.cs` (knockback, `speedMultiplier`), `Player.prefab` (add `Health`: max 10, `invulnerableTime` 0.8; set layer `Player`)
- Modify: `ProjectSettings/TagManager.asset` (layer 6 = `Player`), `ProjectSettings/Physics2DSettings.asset` (Player×Enemy collision off). Make both changes through the API (`Physics2D.IgnoreLayerCollision` is runtime-only; use `SerializedObject` on the settings asset or the Project Settings window).
- Modify: `BigSlime.prefab`, `BabySlime.prefab` (add `ContactDamage`)
- Modify: `TestMenu.cs` (Player: health slider, max health, god mode toggle, heal; Enemies: contact damage, interval and knockback, applied to every living enemy and to newly spawned ones). Remove the "Enemies cannot deal damage yet" label.
- Test: `Assets/Tests/Editor/ContactDamageTests.cs`

**Interfaces:**
- Consumes: `Health`.
- Produces: `class ContactDamage : MonoBehaviour`. Fields: `damage = 1`, `damageInterval = 1f`, `contactRadius = 0.55f`, `knockbackSpeed = 8f`, `knockbackTime = 0.15f`. Each frame it checks `Vector2.Distance(slime, player) <= contactRadius + playerRadius` (or an `OverlapCircle` on the player layer). `static bool ReadyToHit(float lastHitTime, float now, float interval)` is a pure gate.
- Produces: `PlayerMovement.ApplyKnockback(Vector2 velocity, float duration)`: for the duration, `FixedUpdate` sets `linearVelocity = velocity` and ignores input. `public float speedMultiplier = 1f` multiplies `moveSpeed`.
- Produces: the player flashes while `Health.IsInvulnerable` (alpha blink on the `visual` sprite), done in a small `PlayerHurtFlash` behaviour inside `PlayerMovement.cs`'s folder: `Assets/Prefabs/Scripts/PlayerHurtFlash.cs`.

- [ ] **Step 1: Write the failing tests:** `ReadyToHit(-999, 0, 1)` true; `ReadyToHit(0, 0.5f, 1)` false; `ReadyToHit(0, 1f, 1)` true.
- [ ] **Step 2: Run `ContactDamageTests`.** Expected: FAIL.
- [ ] **Step 3: Implement `ContactDamage`, knockback, `speedMultiplier`, `PlayerHurtFlash`, the layer and matrix changes, the prefab edits and the Test Menu controls.**
- [ ] **Step 4: Run `ContactDamageTests`.** Expected: PASS, 3.
- [ ] **Step 5: Play check 3a** (`testing the new thing`, ask first): spawn a slime with the Test Menu's `SpawnEnemy` at the player's position + (0.3, 0). Over 3.5 s, log the player's health each time it changes. Expected: it drops by 1 about once per second, never twice within 0.8 s, and the player is pushed away. Check that the player has no Rigidbody contact with the slime (the player's position isn't carried along while standing still and the slime is in `stopDistance`). With god mode on, health stays at 10.
- [ ] **Step 6: Checkpoint.** Update `Player.md` (health, knockback, layer), `Slime Enemy.md` (contact damage, shoving fixed), `Combat.md`, `Test Menu.md`, and the Roadmap known issues (remove "Nothing can hurt the player" and "Chasing slime shoves the player").

---

### Task 4: Attack cooldowns and the attack corner HUD

**Files:**
- Modify: `PlayerAttack.cs`
- Create: `Assets/Prefabs/Scripts/AttackCorner.cs`, `Assets/Prefabs/UI/Attack Corner.prefab`, `Assets/Prefabs/Scripts/HealthLabel.cs`, `Assets/Prefabs/UI/Health Label.prefab`
- Modify: `testing the new thing.unity` (add both prefabs to its Canvas)
- Modify: `TestMenu.cs` (Player: melee cooldown slider)
- Test: `Assets/Tests/Editor/AttackStatusTests.cs`

**Interfaces:**
- Produces on `PlayerAttack`:
  - `public float meleeCooldown = 0.6f` (the tooltip says it should be ≥ `swingDuration`). The timer starts when the swing starts.
  - `float MeleeCooldownRemaining`, `float MeleeCooldownFraction` (1 = just used, 0 = ready), `float SecondsToNextCharge` (0 when full)
  - `string MeleeBlockedReason`, `string SpellBlockedReason`: `""` when ready, otherwise `"Busy"` (the other attack is playing), `"Cooldown"`, or `"No charges"`. Busy takes priority.
  - `public void SetMaxSpellCharges(int max, bool fill)`, used by abilities.
- Produces: `static string AttackCorner.StatusText(string reason, float seconds)`: `""` → `"Ready"`; `"Busy"` → `"Busy"`; otherwise `reason + " " + seconds.ToString("0.0") + "s"`, e.g. `"No charges 2.3s"`.
- `AttackCorner` finds the player's `PlayerAttack` by the `Player` tag. It has two slots (label, fill `Image` with `fillAmount`, status `Text`) plus charge dots for Fire, and updates every frame with unscaled reads.
- `HealthLabel` shows `"HP {current}/{max}"` from `Health.Changed`.

- [ ] **Step 1: Write the failing tests:** `StatusText("", 0)` == `"Ready"`; `StatusText("Busy", 0.4f)` == `"Busy"`; `StatusText("Cooldown", 0.35f)` == `"Cooldown 0.4s"`; `StatusText("No charges", 2.3f)` == `"No charges 2.3s"`.
- [ ] **Step 2: Run `AttackStatusTests`.** Expected: FAIL.
- [ ] **Step 3: Implement the `PlayerAttack` changes, `AttackCorner`, `HealthLabel` and the prefabs** (bottom-left and top-left anchors, default font as in the existing menus). Add them to `testing the new thing`'s Canvas. The Fire slot's fill uses `1 - RechargeProgress` when it has no charges, otherwise 0.
- [ ] **Step 4: Run `AttackStatusTests`.** Expected: PASS, 4.
- [ ] **Step 5: Play check 4a** (`testing the new thing`): call the melee path twice within 0.3 s (simulate `meleeAction` through the Input System test device as in the 2 Oct check, or expose `public void TryMelee()`) → only one swing. Log `MeleeBlockedReason` right after the first (`"Busy"`), after the animation (`"Cooldown"`), and after the cooldown (`""`). Cast until the charges are 0 → `SpellBlockedReason == "No charges"`. Take a Game view capture showing the corner.
- [ ] **Step 6: Checkpoint.** Update `Combat.md` (cooldowns), and the new `Systems/HUD.md` (attack corner and health label).

---

### Task 5: Level data from the randomizer

**Files:**
- Modify: `LevelRandomizer.cs`
- Create: `Assets/Prefabs/Scripts/LevelGraph.cs` (static helpers)
- Test: `Assets/Tests/Editor/LevelGraphTests.cs`

**Interfaces:**
- Produces on `LevelRandomizer`:
  - `[Tooltip] public bool buildOnStart = true;`. `Start()` only builds when this is true (and the floor is empty, as today).
  - `IReadOnlyList<RectInt> Rooms`, `int StartRoomIndex` (0), `IReadOnlyList<Vector2Int> RoomLinks` (pairs of room indices from the door list), all filled by `Randomize()`.
  - `bool TryGetRandomFloorPoint(int roomIndex, out Vector2 point)`: a random cell inside the room's floor, at least 1 cell from the room edge. It is rejected if `Physics2D.OverlapCircle(point, 0.4f)` hits a collider tagged `wall` (pillars). Up to 20 tries.
  - `int RoomIndexAt(Vector2 worldPoint)`: −1 if the point is in no room.
- Produces: `static class LevelGraph`
  - `int[] DoorDistances(int roomCount, IReadOnlyList<Vector2Int> links, int from)`, a BFS (unreachable = int.MaxValue)
  - `int FarthestRoom(int roomCount, IReadOnlyList<Vector2Int> links, IReadOnlyList<RectInt> rooms, int from)`: largest door distance; ties go to the larger centre distance from `from`.
  - `List<int> RoomsByDistance(...)`: the same ordering, descending, excluding `from`

- [ ] **Step 1: Write the failing tests:** chain 0–1–2–3 → `FarthestRoom(...,0) == 3`. Star 0–1, 0–2, 0–3 with room 3's centre farthest → 3. One room → `FarthestRoom == 0` and `RoomsByDistance` is empty. Distances in the chain → [0,1,2,3].
- [ ] **Step 2: Run `LevelGraphTests`.** Expected: FAIL.
- [ ] **Step 3: Implement `LevelGraph` and the randomizer additions.** The door list already records which rooms each door joins, so keep the parent index when attaching a room.
- [ ] **Step 4: Run `LevelGraphTests`.** Expected: PASS, 4.
- [ ] **Step 5: Edit-mode check 5a.** Don't save the scene: open nothing new, and use the user's open scene only if it is `testing the new thing` and not in Play mode. Otherwise do this in Play mode. Call `Randomize()` and log `Rooms.Count`, `RoomLinks.Count` (== rooms − 1), and 5 `TryGetRandomFloorPoint` results, each with `RoomIndexAt` matching the requested room.
- [ ] **Step 6: Checkpoint.** Update `Level Randomizer.md`.

---

### Task 6: Inventory and abilities

**Files:**
- Create: `Assets/Prefabs/Scripts/Inventory.cs`, `Assets/Prefabs/Abilities/Ability.cs`, `AbilityPool.cs`, `VitalityAbility.cs`, `QuickScytheAbility.cs`, `DeepReservesAbility.cs`, `FleetFootAbility.cs`, plus one `.asset` for each of the four, and `Default Ability Pool.asset`
- Modify: `Player.prefab` (add `Inventory`), `TestMenu.cs` (Inventory section: give key, add any ability from the pool, list the inventory)
- Test: `Assets/Tests/Editor/InventoryTests.cs`

**Interfaces:**
- Produces: `abstract class Ability : ScriptableObject` with `displayName`, `[TextArea] description`, `maxStacks = 3`, and `abstract void Apply(GameObject player)`.
  - Vitality: `Health.SetMaxHealth(max+2, false)` then `Heal(2)`. Quick Scythe: `meleeCooldown *= 0.8f`. Deep Reserves: `SetMaxSpellCharges(max+1, false)` and +1 current charge. Fleet Foot: `speedMultiplier += 0.15f`.
- Produces: `[CreateAssetMenu] class AbilityPool : ScriptableObject { public List<Ability> abilities; }` and `static List<Ability> AbilityPool.Draw(IList<Ability> pool, Func<Ability,int> stackCount, int count, System.Random rng)`: distinct, skips `stackCount(a) >= a.maxStacks`, returns up to `count`.
- Produces: `class Inventory : MonoBehaviour` with `int Keys`, `void AddKey()`, `bool TryUseKey()`, `void AddAbility(Ability a)` (adds the stack and calls `a.Apply(gameObject)`), `int StackCount(Ability a)`, `IReadOnlyDictionary<Ability,int> Abilities`, and `event Action Changed`.

- [ ] **Step 1: Write the failing tests:**
  - `Keys_AddAndUse`: `AddKey` → 1; `TryUseKey` true → 0; `TryUseKey` false.
  - `AddAbility_StacksAndApplies`: a test `Ability` subclass counting `Apply` calls; added twice → `StackCount == 2`, applied twice, `Changed` fired twice.
  - `Draw_ReturnsDistinct`: a pool of 4, count 3 → 3 distinct.
  - `Draw_SkipsMaxedAndShortensHand`: a pool of 4 where 2 are maxed → returns 2. All maxed → empty.
- [ ] **Step 2: Run `InventoryTests`.** Expected: FAIL.
- [ ] **Step 3: Implement everything, create the assets** (through `ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset`, or the Create menu), and add the Test Menu section.
- [ ] **Step 4: Run `InventoryTests`.** Expected: PASS, 4.
- [ ] **Step 5: Play check 6a** (`testing the new thing`): add each ability once through the Test Menu method and log the changed values (max health 12 and current health +2, cooldown 0.48, charges 3, multiplier 1.15).
- [ ] **Step 6: Checkpoint.** Write `Systems/Inventory and Abilities.md`.

---

### Task 7: `WaveSpawner`

**Files:**
- Create: `Assets/Prefabs/Scripts/WaveSpawner.cs`
- Modify: `TestMenu.cs` (a Waves section, shown when a `WaveSpawner` exists: start waves, skip wave, kill all, the spacing and size fields)
- Test: `Assets/Tests/Editor/WaveSpawnerTests.cs`

**Interfaces:**
- Consumes: `LevelRandomizer.Rooms` / `TryGetRandomFloorPoint`, `EnemyHealth.AliveCount` / `Alive`, `BigSlime.Alert()`.
- Produces: `class WaveSpawner : MonoBehaviour`
  - Fields (defaults): `enemyPrefab`, `baseWaves = 2`, `wavesPerLevel = 0.5f`, `maxWaves = 6`, `baseEnemiesPerWave = 2`, `enemiesPerLevel = 1`, `maxEnemiesPerWave = 10`, `timeBetweenWaves = 3f`, `minPlayerDistance = 6f`, `minEnemySpacing = 2f`, `crowdRadius = 5f`, `maxPerRadius = 3`, `spawnAttemptsPerEnemy = 15`
  - `static int WaveCount(int level, ...)` = `min(maxWaves, baseWaves + floor(level*wavesPerLevel))`; `static int WaveSize(int level, ...)` = `min(maxEnemiesPerWave, baseEnemiesPerWave + level*enemiesPerLevel)`. Both take the values as parameters so they're testable.
  - `static bool IsValidSpawn(Vector2 p, Vector2 player, IList<Vector2> enemies, float minPlayerDistance, float minEnemySpacing, float crowdRadius, int maxPerRadius)`
  - `void StartLevel(int level)`, `void SkipWave()`, `void KillAll()`, `void StopAll()` (destroys every alive enemy, with no events)
  - `int CurrentWave`, `int TotalWaves`, `float NextWaveIn` (seconds, 0 while fighting), `bool Running`
  - `event Action<int> WaveStarted`, `event Action AllWavesCleared`
  - A wave is cleared when `AliveCount == 0` is checked in `Update`, not on the event, so same-frame splits never count. If a wave spawned 0, it's cleared at once.

- [ ] **Step 1: Write the failing tests:**
  - `WaveCount_Grows`: level 1 → 2, level 2 → 3, level 20 → 6.
  - `WaveSize_Grows`: level 1 → 3, level 20 → 10.
  - `IsValidSpawn_RespectsPlayerDistance`: 5 from the player with min 6 → false; 7 → true.
  - `IsValidSpawn_RespectsSpacingAndCrowd`: an enemy 1.5 away with spacing 2 → false. Three enemies within radius 5 (each ≥ 2 apart), `maxPerRadius` 3 → false. Two → true.
  - `SpawnRules_GivesUpAfterAttempts`: the public `static bool TryFindSpawn(Func<Vector2?> sample, Func<Vector2,bool> valid, int attempts, out Vector2 p)` with an always-invalid predicate → false after exactly `attempts` samples.
- [ ] **Step 2: Run `WaveSpawnerTests`.** Expected: FAIL.
- [ ] **Step 3: Implement `WaveSpawner`.** A sample is a random room, then `TryGetRandomFloorPoint`. Enemy positions come from `EnemyHealth.Alive`. Spawned slimes are parented under a `Enemies` object the spawner creates.
- [ ] **Step 4: Run `WaveSpawnerTests`.** Expected: PASS, 5.
- [ ] **Step 5: Play check 7a** (`testing the new thing`, with a temporary `WaveSpawner` added in Play mode only): `StartLevel(1)` → log every spawn position; check the spacing and player distance rules hold. `KillAll` → after `timeBetweenWaves`, wave 2 starts; after the last wave, `AllWavesCleared` fires. **7b:** set `minPlayerDistance = 999`, `StartLevel(1)` → every wave spawns 0, `AllWavesCleared` fires, and there are no errors.
- [ ] **Step 6: Checkpoint.** Start `Systems/Game Loop.md` with the waves section.

---

### Task 8: Key and gate

**Files:**
- Create: `Assets/Prefabs/Scripts/KeyPickup.cs`, `Assets/Prefabs/Scripts/Gate.cs`, `Assets/Prefabs/Key.prefab`, `Assets/Prefabs/Gate.prefab`, `Assets/Art/Placeholders/KeyOutline.png` (a 16×16 hollow red square, 2-px border, transparent inside, imported as a Sprite with point filter and PPU 16)
- Modify: `TestMenu.cs` (Inventory section: spawn the key next to the player, spawn the gate next to the player)

**Interfaces:**
- Consumes: `Inventory` (Task 6).
- Produces: `KeyPickup`, a trigger `BoxCollider2D` with a kinematic `Rigidbody2D` (so the player's dynamic body gets trigger callbacks). `OnTriggerEnter2D` with the `Player` tag → `Inventory.AddKey()`, `PickedUp` event, destroy itself. Sorting order 0.
- Produces: `Gate`, a trigger with two looks (`closedColor` dark grey and `openColor` light, on a white square sprite; size 2×2). `bool IsOpen`, `event Action Opened`, `event Action Entered`, `event Action NeedsKey`. Player enter: if closed and `TryUseKey()`, open. If closed with no key, raise `NeedsKey`. If open, raise `Entered`. Because the player is already inside the trigger when it opens, `Entered` also fires from `OnTriggerStay2D` after the gate has been open for 0.3 s.

- [ ] **Step 1: Create the sprite, scripts and prefabs.** These are trigger behaviours, so there's no EditMode test; Step 2 checks them.
- [ ] **Step 2: Play check 8a** (`testing the new thing`): spawn the gate beside the player and walk into it (set the position) → `NeedsKey`. Spawn the key on the player → `Keys == 1`. Re-enter the gate → `Opened`, `Keys == 0`, then `Entered`. Take a capture showing the hollow red key.
- [ ] **Step 3: Checkpoint.** Add the key and gate to `Systems/Game Loop.md` and `Scenes and Assets.md`.

---

### Task 9: `GameLoop`, objective HUD, ability choice screen, death screen

**Files:**
- Create: `Assets/Prefabs/Scripts/GameLoop.cs`, `AbilityChoiceScreen.cs`, `LoopHud.cs` (top-centre level/wave/objective, top-right inventory strip, the level banner and the death overlay), and `Assets/Prefabs/UI/Loop HUD.prefab`, `Ability Choice.prefab`
- Modify: `PauseMenu.cs` (ignore Escape while `GameLoop.BlocksPause` is true)
- Test: `Assets/Tests/Editor/GameLoopTests.cs`

**Interfaces:**
- Consumes: everything above.
- Produces: `enum LoopState { Starting, Fighting, KeyHunt, GateOpen, Choosing, Dead }`
- Produces: `class GameLoop : MonoBehaviour`
  - Fields: `randomizer`, `spawner`, `keyPrefab`, `gatePrefab`, `abilityPool`, `choiceScreen`, `hud`, `abilityEveryNLevels = 2`, `levelBannerTime = 2f`, `deathScreenTime = 1.5f`, `menuSceneName = "Start Menu"`
  - `int Level`, `LoopState State`, `static bool BlocksPause` (true while `Choosing` or `Dead`), `event Action<LoopState> StateChanged`
  - `void BeginLevel()`, `void OnPlayerDied()`, `void OnGateEntered()`. For the Test Menu: `void ForceKey()` (spawns the key at the player), `void ForceOpenGate()`, `void NextLevel()`, `void OfferChoiceNow()`
  - `static LoopState Transition(LoopState from, string evt)`, a pure table for the tests. Events: `"wavesCleared"` Fighting→KeyHunt; `"gateOpened"` KeyHunt→GateOpen; `"gateEntered"` GateOpen→Choosing or Starting (the caller decides by level); `"choiceMade"` Choosing→Starting; `"died"` any→Dead; any event from Dead → Dead.
  - `static bool OffersChoice(int level, int everyN)` = `everyN > 0 && level % everyN == 0`
- The death flow: `Time.timeScale = 0`, the HUD shows "You died", wait `deathScreenTime` with `WaitForSecondsRealtime`, `Time.timeScale = 1`, `SceneManager.LoadScene(menuSceneName)`. `OnDestroy` also resets the time scale.
- Produces: `AbilityChoiceScreen.Open(IList<Ability> cards, Action<Ability> onPicked)`. Time scale 0; it builds one button per card and selects the first with `EventSystem.current.SetSelectedGameObject`. On pick: close, time scale 1, then the callback. `GameLoop` calls `AbilityPool.Draw` and skips the screen if the hand is empty.
- `LoopHud` objective text by state: Fighting → `"Survive"` (or `"Next wave in Ns"` between waves); KeyHunt → `"Find the key"`; GateOpen → `"Go to the gate"`; and `"Need a key"` for 2 s after `Gate.NeedsKey`.

- [ ] **Step 1: Write the failing tests:**
  - `Transition_HappyPath`: Fighting —wavesCleared→ KeyHunt —gateOpened→ GateOpen.
  - `GameLoop_DeathDuringChoosing`: `Transition(Choosing, "died") == Dead`, and `Transition(Dead, "choiceMade") == Dead`.
  - `OffersChoice_EverySecondLevel`: levels 1,2,3,4 with N=2 → false,true,false,true. N=0 → always false.
- [ ] **Step 2: Run `GameLoopTests`.** Expected: FAIL.
- [ ] **Step 3: Implement `GameLoop`** (using `Transition` for every state change), `LoopHud`, `AbilityChoiceScreen`, their prefabs and the `PauseMenu` guard. `BeginLevel` follows the spec's four steps. The key goes to `LevelGraph.RoomsByDistance` from the player's current room, choosing randomly among the farther half, and falls back to the current room ≥ 4 units from the player. `OnPlayerDied` is wired to the player's `Health.Died` in `Start`.
- [ ] **Step 4: Run `GameLoopTests`.** Expected: PASS, 3. Also rerun all earlier classes; all PASS.
- [ ] **Step 5: Checkpoint.** Finish `Systems/Game Loop.md` and `Systems/HUD.md`.

(The play checks for this task run in Task 10, because they need the Demo scene.)

---

### Task 10: The Demo scene, Start → Demo, Test Menu Demo section, full verification

**Files:**
- Create: `Assets/Scenes/Demo.unity` (`AssetDatabase.CopyAsset` from `testing the new thing.unity`)
- Modify: `Demo.unity` (remove the Randomize button, set `buildOnStart = false`, clear the saved level tiles, add `GameLoop`, `WaveSpawner` and the HUD prefabs: Loop HUD, Attack Corner, Health Label, Ability Choice; wire references)
- Modify: `ProjectSettings/EditorBuildSettings.asset` (add Demo after Start Menu), `Start Menu.unity` (`MainMenu.gameSceneName = "Demo"`). Change the scene value rather than the script default, but also change the script default to `"Demo"`.
- Modify: `TestMenu.cs` (a Demo section shown when `FindAnyObjectByType<GameLoop>()` is not null: kill all, skip wave, spawn key, open gate, next level, offer choice now, show state/level/wave)

- [ ] **Step 1: Build the scene and wiring.** Ask before opening a different scene; save the user's open scene first only with their OK. Check for missing scripts on every object in Demo.
- [ ] **Step 2: Scene capture** of Demo (edit mode), plus a console check.
- [ ] **Step 3: Play checks in Demo** (ask first; report start and stop):
  - **10a:** On Play, level 1 builds, the gate is in `LevelGraph.FarthestRoom`, wave 1 spawns after the banner, and the HUD reads `Level 1 · Wave 1/2`.
  - **10b:** Kill all twice → the key spawns in a room ≠ the player's room; the objective is "Find the key".
  - **10c:** Spawn the key at the player → `Keys 1`; teleport to the gate → it opens; enter → level 2 with a different layout (log the room rects of both levels and check they differ). Level 1 → no choice screen.
  - **10d:** Finish level 2 the same way → the choice screen shows 3 cards and time scale is 0. Escape does nothing. Pick card 0 → the ability is in the inventory with its effect applied, level 3 builds, time scale is 1.
  - **9c / 10e:** Set the player's health to 1 and spawn a slime on the player → "You died" → after about 1.5 s the active scene is `Start Menu` with `Time.timeScale == 1`.
  - **9d:** Open the choice screen with `OfferChoiceNow`, then kill the player → Dead wins, and the menu loads with no level built after it.
  - Stop Play mode and report.
- [ ] **Step 4: Vault pass.** Update `Home.md` (current state, map rows for Game Loop, Inventory and Abilities, HUD), `Architecture.md` (diagram nodes GameLoop, WaveSpawner, Health, Inventory, HUD; the fourth convention reworded around `Health`; gaps updated), `Menus and Game Flow.md` (Start → Demo, the death flow, Escape guard), `Scenes and Assets.md` (the Demo scene and the roles of the three scenes), `Test Menu.md`, and `Roadmap.md` (move the finished items into Done with today's date; add new known issues found during testing).
- [ ] **Step 5: Final report to the user,** including what still needs a manual check: gamepad selection on the choice screen, the feel of the cooldowns and contact damage, and the real Escape key.
