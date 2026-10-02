# gyarte

Top-down 2D dungeon crawler in Unity 6 (6000.5.2f1, URP 2D, new Input System, Cinemachine). The project is in a testing stage; systems are being built and proven in test scenes.

## At the start of every session

Read the Obsidian vault in `Obsidian/` before doing anything else. It is the blueprint of the project.

1. `Obsidian/Home.md` — the map of the vault and the current state.
2. `Obsidian/Weekly.md` — what happened so far and the goal for the current week.
3. `Obsidian/Roadmap.md` — the plan, open decisions and known issues.
4. The system note under `Obsidian/Systems/` for whatever the task touches, plus `Obsidian/Architecture.md` when the task crosses systems.

The vault may be out of date if work happened outside a session. When it disagrees with the code, the code is right; fix the note.

## Keep the vault current

- When a system changes, update its note in the same piece of work, and `Architecture.md` if a link between systems changed.
- When something is finished or a new problem is found, update `Roadmap.md`.
- At the end of a week, or when asked, add the week's entry to `Weekly.md`: what happened, how things work now, and the next goal.
- Notes are written for a reader who has not seen the code: explain how and why, use `[[wikilinks]]` between notes, and leave exact values to the code and the Inspector.

## Working in this project

- **Scripts** live in `Assets/Prefabs/Scripts/` and `Assets/Prefabs/Enemies/Slime/Scripts/`. They use no namespaces. Match the existing style: `[Header]` and `[Tooltip]` on public fields, short comments that explain why.
- **Three conventions hold the systems together:** the `Player` tag, the `wall` tag on anything enemies should treat as solid, and room objects (trigger `BoxCollider2D` + `SpriteRenderer`) as children of the object with `RoomManager`. See `Obsidian/Architecture.md`.
- **Unity 6 API:** use `Rigidbody2D.linearVelocity`, `FindAnyObjectByType`, and `GetEntityId()` rather than `GetInstanceID()`.
- **Every new thing goes in the test menu.** `TestMenu.cs` (opened with U in `testing the new thing`) is where features are tried out. When a system, enemy, attack or setting is added, add its controls to the menu in the same piece of work. See `Obsidian/Systems/Test Menu.md`.
- **Verify in the editor.** When the `unity-mcp` tools are available (Unity must be open on this project), check work there before reporting: console errors, missing scripts, a scene capture, and a Play mode test for behaviour. Say so plainly when something could not be tested.
- **Editor state belongs to the user.** Check whether Play mode is running before changing a scene, and say when you start or stop it.
- **Renames and moves** of assets go through Unity (or keep the `.meta` file with the asset) so references survive.
- **Git:** commit only when asked. Do not touch scenes or changes you did not make.
