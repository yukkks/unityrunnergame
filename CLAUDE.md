# Dog Runner — Farewell ("One More Walk" / SciFiRunner)

An endless **lane-runner**: a dog runs forward, dodging obstacles and eating
treats/coins. Eating makes the dog visibly chubbier ("get too chubby to fly").
Built for **WebGL** and deployed to Vercel; a separate landing page
(`dogrunner-website`) plays an intro video then embeds this game in an iframe.

## Environment (verify before assuming)

- **Unity 6.3** — `6000.3.10f1` (see `ProjectSettings/ProjectVersion.txt`)
- **Render pipeline: URP 17.3.0** (`com.unity.render-pipelines.universal`). All
  shaders/materials must be URP — Built-in shaders show as magenta. Two editor
  scripts exist to repair this: `Assets/Editor/AutoUpgradePolygonMaterialsToURP.cs`
  and `Assets/Editor/FixMissingSceneMaterials.cs`.
- **Input: new Input System** (`com.unity.input.settings.actions` is set in
  EditorBuildSettings). Do not use the legacy `Input.GetKey` API for new code.
- **Build target: WebGL** (`Assets/WebGLTemplates/`, `vercel.json`).

## The scene that matters

- **`Assets/RunnerScene.unity`** is the one enabled build scene — this is the
  actual game. `SampleScene`, the various `Showcase`/`DEMO` scenes, and
  `Assets/_Recovery/` are asset-pack demos / backups, not the game.

## Gameplay scripts — `Assets/Scripts/`

Hand-written game code lives only here (everything else under `Assets/` is a
third-party asset pack: PolygonSciFiCity, Japanese Alley, Red_Deer dogs,
VegetablePack, ZeroGrid food, explosions, skyboxes, TextMesh Pro, FlexUnit).

- **`GameManager.cs`** — central singleton (`GameManager.Instance`). Owns the
  `GameState` (`Waiting` / `Running` / `GameOver`), speed ramp
  (`startSpeed`→`maxSpeed`), score, all TMP UI, the **Supabase global top-score**
  leaderboard (via `UnityWebRequest`), and **URP post-processing / lighting /
  atmosphere** driven from code. Key entry points: `AddScore()`, `GameOver()`.
  This file is large and central — read it before changing game flow.
- **`PlayerController.cs`** — lane-based dog movement + VFX.
- **`Spawner.cs`** — spawns obstacle/coin patterns based on spawn rate.
- **`ObstacleMover.cs`** — moves spawned objects toward the player.
- **`CoinPickup.cs`** — collectible treat/coin pickup + animation.
- **`DogWeightVisual.cs`** — `GainWeight()` / `LoseWeight()` scale the dog model
  (the "chubby" mechanic).
- **`CameraFollow.cs`** — follow cam with tilt and lane-switch feedback.
- **`AudioController.cs`** — BGM + SFX (`PlayLaneSwitch`, `PlayHit`, `PlayCoin`,
  `SetRunning`, `SetBgmEnabled`).
- **`SideBuildings.cs`** — procedural roadside environment.
- **`SplashTipsCycler.cs`** — cycles loading-tip text on the splash screen.

## Conventions

- Gameplay code is exposed to designers via `[Header(...)]`-grouped public
  inspector fields. Prefer adding tunables as serialized fields over hardcoding.
- Cross-script calls go through `GameManager.Instance` and the other
  MonoBehaviour public methods above — match that pattern, don't invent new
  global singletons.
- Secrets note: `GameManager` has public `supabaseUrl` / `supabaseAnonKey`
  fields set in the scene/inspector. The anon key is client-side by design, but
  do not paste service-role keys here.

## Working agreements for Claude

- **Compile feedback:** I cannot see Unity's console unless a Unity MCP bridge is
  connected (see README/your setup). Without it, paste compile errors back to me.
- Don't edit third-party asset-pack folders unless explicitly asked.
- Keep changes small and compile often — Unity C# round-trips are slow.
- This is a git repo with LFS for binary assets; `.gitignore` already excludes
  `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds. Don't commit those.

## Known oddities

- Several Unity SRP Core files (`ComputeCommandBuffer.cs`, `RasterCommandBuffer.cs`,
  `I*CommandBuffer.cs`) sit at the **project root**, outside `Assets/`. They look
  accidentally copied and are not part of the game. Confirm before touching/removing.
