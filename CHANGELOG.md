# Changelog

All notable changes to this package are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- **Repeat**, to keep re-running the Actions at regular intervals while a zone stays occupied (e.g. lava
  that hurts every second you stand in it). A new **Repeat** option under After Trigger (offered with Do
  Nothing and Execute Exit Actions), with an **Interval** and a **Max Repeats** limit (0 = unlimited). Each
  repeat re-runs the whole Action list, Waits included, and re-checks the Conditions first - if they're
  false when a repeat is due, it runs as soon as they're true again. Hold Time only applies to the first
  run. It stops when the object leaves the zone, or is destroyed or deactivated while inside (with Execute
  Exit Actions, the Exit Actions then run once). On a Condition Watcher it repeats while the Conditions stay
  true. Works on `Collider Event`, `Collider Event 2D` and `Condition Watcher`.
- **Exit Actions When Repeats Finish**: with After Trigger set to Execute Exit Actions and a Max Repeats
  above 0, runs the Exit Actions as soon as the last repeat has finished (after its Actions are done),
  instead of waiting for the object to leave. The cycle then ends as if it had left, so the Exit Actions
  don't run a second time on leaving; a Condition Watcher waits for its Conditions to go false before it
  can start a new series. Off by default.

- **`Wait` Action**, to run Actions one after another instead of all at once. Everything above a Wait runs
  first, the list then pauses, and everything below it runs once the wait is over. Two modes: **Seconds**
  (a fixed time) or **Until Previous Actions Finish** (the longest-running Action above it). It also works
  in the Exit Actions list. A list without a Wait behaves exactly as before.

### Changed

- Leaving a zone (or, on a Condition Watcher, the Conditions going false) while the Action list is still
  running - which can now last longer than a frame, because of Wait - lets the sequence run to the end,
  except with **After Trigger > Execute Exit Actions**, where it cuts the sequence short and runs the Exit
  Actions straight away.

### Fixed

- A Collider Event / Condition Watcher disabled while its Actions were still running (for example by one of
  those Actions) could stay stuck and never fire again; it now recovers when re-enabled.

## [0.6.0]

### Added

- **2D support.** `Collider Event 2D` (**Add Component > Whenever > Collider Event 2D**) is the 2D
  counterpart of `Collider Event`: add it alongside any `Collider2D` set to "Is Trigger". It shares
  everything else with the 3D version (Conditions, Actions, Hold Time, After Trigger, Hint Material, Tags
  filter, Debug Color gizmo - drawn as a flat shape, or an outline for a Polygon Collider 2D). It uses the
  same Inspector as `Collider Event`.
- `Rigidbody 2D` Action: the 2D counterpart of `Rigidbody`. Changes a `Rigidbody2D`'s Body Type, Mass,
  Linear Damping, Angular Damping and/or Gravity Scale - each with its own on/off toggle, so only the
  ones you check are touched.
- `Transform Action`'s "Cancel Physics While Moving" now also covers a `Rigidbody2D` (switched to
  Kinematic for the duration of the move, then restored), not just a 3D `Rigidbody`.
- `Looking At` stays 3D-only (it relies on the camera's facing and a 3D raycast); the README says so.
- `Trigger Zone 2D` starter prefab: a Box Collider 2D ("Is Trigger") with a Collider Event 2D attached.

### Changed

- The `Trigger Zone` starter prefab is now `Trigger Zone 3D`, to sit next to `Trigger Zone 2D`. Its GUID
  is unchanged, so existing references keep working.

## [0.5.0]

### Added

- `Third-Party Notices.txt`, listing the third-party components bundled in the optional Demo sample (the
  Inter font under the SIL Open Font License 1.1, and a Pixabay sound effect), as the Asset Store
  submission guidelines require.

### Changed

- **The package is now called Whenever** (previously "Collider Event System"), since it covers more than
  collisions - a Condition Watcher fires without any Collider. `ColliderEvent` and `ConditionWatcher`
  keep their names. This is a breaking rename for anyone upgrading from Collider Event System:
  - Package name is now `com.jfcartier.whenever`.
  - Namespace is now `JFCartier.Whenever` (and `.Editor`, `.Editor.Drawers`, `.Samples`), replacing
    `ColliderEventSystem`; assembly definitions are renamed to match (`JFCartier.Whenever`,
    `JFCartier.Whenever.Editor`). Their GUIDs are unchanged.
  - Menu paths moved from "Collider Event System/..." to "Whenever/..." (Add Component menu and
    **Assets > Create > Whenever > Variables**), and the Console log prefix is now "(Whenever)".
  - The two packages share GUIDs, so don't install both in the same project.
- Minimum Unity version lowered to **6000.0.66f2** (was 6000.4.10f1), so the package can be used on
  Unity 6.0 LTS onward. 6000.0.66f2 is the first 6000.0 build without a Unity security alert/warning.
- The starter prefab `Trigger Cube` is now `Trigger Zone`: it describes what the prefab is (a logic zone)
  rather than a shape, which the README now says is free to choose. Its GUID is unchanged.
- README: new "Using your own shape" section - a Collider Event can go on any shape with a Collider
  ("Is Trigger" ticked), and the Mesh Renderer can usually be removed since these zones are most often
  invisible logic areas.

### Removed

- The `Trigger Sphere` and `Trigger Capsule` starter prefabs - any other shape is a Collider plus
  Collider Event away (see "Using your own shape" in the README). `Trigger Sphere` is still used by the Demo
  sample, so it now lives in `Samples~/Demo/Prefabs` (GUID unchanged).
- The offline PDF documentation (`Documentation/`). It duplicated the README word for word and had fallen
  out of date; the README (Markdown, an accepted documentation format) is now the documentation.
- The standalone `LICENSE` file (MIT) and the `license` field in `package.json`. The Asset Store rejects
  packages that carry an independent license: everything distributed through it is covered by the Asset
  Store End User License Agreement.

### Fixed

- Compile error on Unity 6000.0 in the Editor assembly (`EntityId` doesn't exist in Unity 6000.0): the
  Condition/Action list's foldout state is now keyed by the Object itself instead, which compiles on every
  Unity 6 version without relying on a deprecated API.

## [0.4.7]

### Fixed

- `Transform Action` only suspended a `Rigidbody` while moving a target, not a `CharacterController` -
  a `CharacterController` left enabled can silently reject or immediately undo a direct position/rotation
  change, making the Action appear to do nothing on a target like a Third Person Controller's capsule.
  It's now disabled for the duration of the move (instant or animated) and re-enabled once it finishes,
  matching how a Rigidbody is already handled.

## [0.4.6]

### Fixed

- A Condition/Action's `HideInInspector` flag (which keeps it from also drawing as its own raw
  component block, since it's always shown inline in the Conditions/Actions list instead) could get
  silently dropped by certain Prefab operations, making it reappear as a duplicate "(Script)" block
  underneath the list. The flag is now reapplied automatically whenever the Inspector is opened, so
  the duplicate block can no longer stick around.

## [0.4.5]

### Added

- Offline PDF documentation (`Documentation/`), covering installation, every Condition and Action,
  Variables, and known gotchas - required for Asset Store submission.

### Changed

- Quick Start now leads with dragging one of the ready-made trigger prefabs into the scene instead of
  building a trigger from scratch; the manual setup (Collider + Is Trigger + Collider Event) is now the
  secondary path, for adding a trigger to an existing GameObject.

### Fixed

- `ColliderEvent`'s capsule gizmo mesh cache is now a per-instance field instead of a shared static one,
  so it no longer needs an explicit Play Mode reset. No behaviour change - purely satisfies static-analysis
  tooling (e.g. the Asset Store Validator) that flags any mutable static field regardless of scope.

## [0.4.4]

### Fixed

- Demo sample's `Material` trigger was set to `ExecuteExitActions` instead of `Destroy` like every other
  trigger in the scene, so entering its zone didn't make it disappear the way the others do. Now uses
  `Destroy`, for consistency across the Demo.

## [0.4.3]

### Fixed

- `Transform Action`'s Additive rotation could produce no visible motion at all when the configured delta
  was a multiple of 360 on one axis (e.g. a "spin 360" effect) - `Quaternion.Euler(0, 0, 360)` is the same
  orientation as no rotation at all, so Slerping straight to it (as 0.1.8's fix for Additive drift did)
  never moved. Additive now reconstructs the rotation from the raw delta at each frame's progress instead
  of Slerping toward a fixed endpoint, so a full turn (or several) animates as an actual visible spin.

## [0.4.2]

### Changed

- Demo sample updated to demonstrate Hint Material (new `Demo Hint` material, reusing the existing
  cross-pipeline "Demo Transparent" Shader Graph).

## [0.4.1]

### Changed

- Every Target Mode field (Transform, Material, GameObject, Rigidbody, Animation, Instantiate Prefab
  Actions, and Hint Material) now defaults to **Specific Object** instead of Entering Objects, for
  consistency across the package. Only affects newly added components - existing ones keep whatever
  they're already set to.

## [0.4.0]

### Changed

- **Hint Material moved from Condition to Collider Event / Condition Watcher.** It reflects the combined
  Condition list result (respecting each Condition's And/Or), not any single Condition's own value - with
  more than one Condition, a per-Condition Hint Material would have shown/hidden based on that Condition
  alone, out of step with when Actions actually run. Behaviour is otherwise identical: applied while the
  Conditions aren't all met, restored once they are or once the zone is left first.

## [0.3.0]

### Added

- Every Condition now has an optional **Hint Material**: applied to a target while the Condition is
  false, and automatically restored once it becomes true (or once the zone is left before it does) -
  useful as a hint that an interaction is available (e.g. an Input Condition highlighting the object you
  can press E on). Shares its apply/restore logic with Material Action's Restore Original mode, so the
  two never disagree about what the "original" material was if they touch the same Renderer.

## [0.2.0]

### Added

- `Material Action` has a new **Mode**: **Apply** (previous behaviour) or **Restore Original**. Apply now
  remembers the Renderer's materials the first time it runs; Restore Original puts them back without
  needing a second, hand-picked "original" material - useful for a temporary highlight while a zone's
  Conditions are still pending (e.g. Action = Apply a highlight material on enter, Exit Action = Restore
  Original), all on a Collider Event with no Conditions of its own so it fires immediately.

## [0.1.10]

### Fixed

- Trigger prefabs (`Prefabs/Trigger Cube`, `Trigger Sphere`, `Trigger Capsule`) referenced Unity's built-in
  default material (the Standard shader), which shows up pink on a URP project - now used a custom Shader
  Graph material (`Materials/Trigger Zone`, targeting both Universal and Built-In) instead, so enabling the
  Mesh Renderer for a visible zone works out of the box regardless of render pipeline.

## [0.1.9]

### Fixed

- Condition/Action list rows now have left padding so their content (foldout, fields) doesn't sit
  underneath the ReorderableList's drag handle.

## [0.1.8]

### Fixed

- `Transform Action`'s **Additive** rotation mode no longer accumulates via Euler angle arithmetic
  (`start + delta`, then writing back through `eulerAngles`). Since `eulerAngles` isn't a stable
  round-trip representation once more than one axis is involved, this could silently drift or even
  cancel out from one run to the next (e.g. alternating +10/-10 instead of accumulating). Now composes
  `Quaternion`s directly and animates with `Slerp` instead of a per-axis `Lerp` on Euler angles.

## [0.1.7]

### Fixed

- `ColliderEvent`: avoided two small GC allocations found by Project Auditor - the 3-argument
  `Mathf.Max` call in the Sphere gizmo path (which allocates a `float[]` via its `params` overload) and
  `new string[0]` for the default Required Tags value (now `Array.Empty<string>()`).

## [0.1.6]

### Fixed

- `ColliderEvent`'s cached capsule gizmo mesh is now explicitly cleared on every Play Mode entry
  (`[InitializeOnEnterPlayMode]`), so it can never carry a stale reference under Fast Enter Play Mode /
  disabled domain reload.

## [0.1.5]

### Added

- Demo's notification sound now also ships as a lossless `.wav`, alongside the original `.mp3` (same file
  name), per Asset Store guidelines against lossy-only audio.

## [0.1.4]

### Fixed

- Demo sample: a misconfigured Shader Graph material, a mislabeled scene object, and a non-functional
  prefab reference, all in the demo scene itself.

## [0.1.3]

### Changed

- Demo sample's on-screen label switched from TextMeshPro to the legacy UI Text, to avoid the one-time
  "Import TMP Essentials" prompt Unity shows the first time TextMeshPro is used in a project - no import
  step of any kind is needed to try the Demo now. Uses the same bundled Inter font (a plain .ttf, no
  TMP-specific font asset needed).

## [0.1.2]

### Fixed

- Demo sample's materials (Demo Dark, Demo Grid, Demo Condition Met, Demo Transparent) now use custom
  Shader Graphs targeting both URP and Built-In, instead of a URP-only shader - no more pink materials or
  running the Render Pipeline Converter on a Built-In project.
- Demo sample now bundles its own font (Inter, SIL Open Font License) instead of referencing the Editor's
  TMP Essentials font, which doesn't exist in a project that has never imported TextMeshPro before.

## [0.1.1]

### Fixed

- `LICENSE` and `CHANGELOG.md` were missing `.meta` files, so Unity ignored them (with a console warning)
  when the package was installed via git URL.

## [0.1.0]

### Added

- **Collider Event** - zone-based trigger built on any native Collider.
- **Condition Watcher** - always-on equivalent with no physical zone, for state-based waits (e.g. "player
  has 3 keys").
- **Conditions**: Looking At, Input, Variable, Distance, Rotation - foldable left-to-right with an And/Or
  operator per condition.
- **Actions**: Animation, Audio, Scene, GameObject, Material, Rigidbody, Transform, Variable, Invoke
  Events, Instantiate Prefab.
- **Variables**: Float/Int/Bool/String assets, optionally persisted to disk between play sessions.
- **Target Mode** (Entering Objects / Specific Object) on every Action that acts on a GameObject, with
  Entering Objects automatically unavailable on a Condition Watcher (no entering object to give it).
- Custom Inspector tooling throughout: grouped "Target"/"Effect" sections, conditional field visibility,
  a "+" button to create a Variable inline, and inline validation warnings for non-obvious silent failures.
- Starter prefabs (`Prefabs/`) for Cube, Sphere, and Capsule trigger zones.
- **Demo** sample scene demonstrating Conditions, Actions, and Variables together.
