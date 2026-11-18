# Whenever

Attach ordered **Conditions** and **Actions** to a GameObject to build zone triggers and always-on
condition watchers, without writing code.

## Components

- **Collider Event** - add alongside any native Collider (mark it "Is Trigger"). Waits for something to
  enter the zone, then checks the Conditions. Optionally restrict which Tags can trigger it - leave every
  box unchecked to let anything with a Collider trigger it.
- **Collider Event 2D** - the same thing for 2D games: add alongside any Collider2D (mark it "Is Trigger").
  See "2D games" below.
- **Condition Watcher** - no Collider needed. Checks the Conditions continuously from the moment the
  scene starts, for things that don't happen at a place (e.g. "wait until the player has 3 keys").

## Quick start

1. Drag a ready-made prefab (in this package's `Prefabs` folder) into your scene: `Trigger Zone 3D` for a
   3D project, `Trigger Zone 2D` for a 2D one.
2. Under Conditions, click "Add Condition" and pick one (e.g. Distance).
3. Under Actions, click "Add Action" and pick one (e.g. Invoke Events).
4. Enter Play Mode and walk into the zone.

### Using your own shape

A **Collider Event** can go on any GameObject that has a Collider - a cube, sphere, capsule, a Mesh
Collider, anything. The shape is yours to choose:

1. Add a Collider to the GameObject (or use one it already has).
2. Tick **Is Trigger** on that Collider, so things pass through it instead of bumping into it.
3. Add **Collider Event** to the same GameObject.

You will usually also want to **remove the Mesh Renderer** (and its Mesh Filter). A Collider Event is most
often a logic zone - "the player reached this spot" - that isn't meant to be seen in the game, only to
detect something. Without a Mesh Renderer the zone is invisible at runtime but still works exactly the
same, and the Debug Color gizmo can still show its outline in the Scene view. (`Trigger Zone 3D` already
ships with its Mesh Renderer disabled.) Keep the Mesh Renderer only if the zone is also meant to be a
visible object, or if you point a Hint Material or Material Action at the zone's own Renderer.

### 2D games

For a 2D project, use **Collider Event 2D** instead of Collider Event - or just drag in the ready-made
`Trigger Zone 2D` prefab. To build one yourself, the steps are the same as above, with a 2D Collider:

1. Add a Collider2D to the GameObject (Box, Circle, Polygon, Capsule... any of them).
2. Tick **Is Trigger** on it.
3. Add **Collider Event 2D** (**Add Component > Whenever > Collider Event 2D**).

Everything else - Conditions, Actions, Hold Time, After Trigger, Hint Material, the Tags filter - works
exactly like the 3D version, and so does the advice about removing the renderer (a zone is usually an
invisible logic area, so remove its Sprite Renderer if it has one). The Debug Color gizmo draws the zone's
outline in the Scene view.

Two things to know:

- As with 3D triggers, Unity only reports a 2D trigger overlap when at least one of the two objects has a
  **Rigidbody2D** (usually the player or whatever is moving).
- Use the **Rigidbody 2D** Action (not Rigidbody) to change a Rigidbody2D, and note that **Looking At** is
  designed for 3D - it measures where the camera faces and uses a 3D raycast to check for obstructions,
  so it doesn't suit 2D scenes. Every other Condition and Action works in 2D.

## Conditions

Looking At, Input, Variable, Distance, Rotation.

Each condition after the first has an **And/Or** selector relating it to the one above it, read strictly
top to bottom (e.g. "A Or B And C" reads as "(A Or B) And C").

## Actions

Animation, Audio, Scene, GameObject, Material, Rigidbody, Rigidbody 2D, Transform, Variable, Invoke Events,
Instantiate Prefab, Wait.

### Running Actions one after another

By default, every Action in the list starts at the same moment, top to bottom. To space them out in time,
put a **Wait** Action between them: everything above it runs first, the list then pauses, and everything
below it runs once the wait is over.

Wait has two modes:

- **Seconds** - waits a fixed time.
- **Until Previous Actions Finish** - waits for the longest-running Action above it (e.g. a Transform
  animated over 2 seconds), however long that turns out to be.

For example: *Transform* (move the door, animated over 2 s) -> *Wait: Until Previous Actions Finish* ->
*Audio* (play a "done" sound) opens the door, then plays the sound once it has finished opening. A list
with no Wait behaves exactly as before.

A few details:

- Wait also works in the Exit Actions list.
- Once a sequence has started, it runs to the end even if the object leaves the zone (a door that is
  opening doesn't stop because you stepped away). The exception is **After Trigger > Execute Exit
  Actions**: there, leaving the zone cuts the sequence short and runs the Exit Actions straight away. On a
  Condition Watcher in that mode, the Conditions going false does the same - so they need to stay true
  for the whole sequence.
- **Set Inactive**, **Destroy** and **Destroy Parent** wait for the whole sequence to finish first.

## Variables

Instead of typing a string key, create a Variable asset (**Assets > Create > Whenever >
Variables**) and drag it into a Variable Condition or Variable Action - or click the "+" next to the
field to create one on the spot. Toggle **Persistent** on a Variable to have it saved to disk and
reloaded automatically between play sessions.

Note: a Variable asset is shared - every GameObject that references the same one reads/writes the same
value. There's currently no built-in way to give each instance of a prefab (e.g. each enemy) its own
independent value like HP without writing a script for it.

## Base options

- **Hold Time** - how many seconds the Conditions must stay true, without interruption, before Actions run.
- **After Trigger** - what happens once Actions have run (deactivate, destroy, do nothing and reset, or
  wait and run Exit Actions once the Conditions/zone go inactive again).
- **Repeat** - run the Actions again at regular intervals for as long as the zone stays occupied. See
  "Repeating while inside" below.
- **Hint Material** - optional. A material applied to a target while the Conditions above aren't all met
  yet, and automatically restored once they are (or once the zone is left before they are) - useful as a
  hint that an interaction is available, e.g. highlighting the object you can press E on until you
  actually press it. Reflects the combined Condition list result (respecting And/Or), not any single
  Condition on its own.

### Repeating while inside

Normally, once the Actions have run, a Collider Event doesn't fire again until the object leaves the zone
and comes back in. Turn on **Repeat** to have it keep going instead - the typical case is lava: the zone
hurts you the moment you step in, then again every second for as long as you stay.

Under **After Trigger** (set to **Do Nothing** or **Execute Exit Actions**), tick **Repeat** and set:

- **Interval** - seconds between the end of one run of the Actions and the start of the next.
- **Max Repeats** - how many extra times to run them after the first. **0** repeats for as long as the
  zone stays occupied.

How it behaves:

- Each repeat runs the **whole Action list again**, Waits included.
- The **Conditions are re-checked** at each repeat. If they're false when a repeat is due, it waits and
  runs as soon as they're true again - so lava can stop burning while a "fire resistant" Variable is true,
  and resume when it isn't.
- **Hold Time** only applies to the first run; repeats start right on their interval.
- It stops when the object leaves the zone, or if it's destroyed or deactivated while inside. With
  **Execute Exit Actions**, the Exit Actions run once at that point - e.g. to remove a "burning" effect.
- Once **Max Repeats** is reached, it behaves like a normal Collider Event: the object has to leave and
  re-enter before it fires again.
- With **Execute Exit Actions** and a **Max Repeats** above 0, an extra option appears: **Exit Actions When
  Repeats Finish**. Tick it to run the Exit Actions as soon as the last repeat is done (once its Actions
  have finished), instead of waiting for the object to leave - e.g. the lava burns 5 times, then a "burning
  ends" effect plays. The cycle then ends as if the object had left, so the Exit Actions don't run a second
  time when it does. On a Condition Watcher, it then waits for the Conditions to go false before it can
  start a new series.
- On a **Condition Watcher** there is no zone, so it repeats for as long as the Conditions stay true. When
  they go false the cycle ends, and when they're true again it starts over from a first run.
- Repeat isn't offered with **Set Inactive**, **Destroy** or **Destroy Parent**, since those remove or
  deactivate the GameObject after the first run.

## Prefabs

`Prefabs/` has two ready-made prefabs, ready to drag into a scene as shown in Quick start:

- `Trigger Zone 3D` - a Box Collider set to "Is Trigger" with a Collider Event already attached.
- `Trigger Zone 2D` - a Box Collider 2D set to "Is Trigger" with a Collider Event 2D already attached.

For any other shape, see "Using your own shape" above.

## Sample

Import **Demo** from this package's page in the Package Manager window for a walkable scene that puts
several Conditions, Actions, and Variables to use together.

Its materials are authored for URP. On a project still using the Built-in Render Pipeline, they'll show up
pink until you run **Window > Rendering > Render Pipeline Converter** (Unity's own built-in fixer) - a
one-time, couple-clicks step, not something wrong with the package itself. The package's actual Conditions
and Actions have no material/shader dependency at all; this only affects the optional Demo sample.

## Gotchas

A few things that aren't bugs, but are easy to trip over:

- **Rotation Condition measures world rotation by default.** If the object you're checking has a rotated
  parent, its world rotation includes the parent's rotation too - not just the number shown on the
  object's own Transform. Switch **Space** to **Self** to compare its local rotation instead.
- **`Transform.eulerAngles` is always 0-360, never negative.** A slight rotation "the wrong way" (e.g. -5°)
  reads as 355°, which can satisfy a "Greater Than" comparison you didn't intend. Rotation Condition
  already normalizes this internally, but keep it in mind if you inspect rotation values elsewhere.
- **Scene Action loading a scene not in Build Settings works in the Editor, but not in a real build.**
  Unity's Editor Play Mode has a convenience fallback that resolves a scene by name across the whole
  project; a built player doesn't have it. Add every scene you load to Build Settings before shipping.
- **Cancel Physics (Transform Action) doesn't pause momentum, it erases it.** A kinematic Rigidbody always
  reports zero velocity. Once restored to non-kinematic after the move finishes, the object resumes at
  rest, not from whatever speed it had before - it won't carry on falling at its old speed, for example.
- **Drag from the Hierarchy, not the Project window, into a Target field.** Dragging a prefab *asset* gives
  you a reference to the asset itself (whose values never change at runtime), not the instance in your
  scene. This is a general Unity gotcha, not specific to this package, but it's an easy mix-up.
- **A Cylinder's Collider is actually a Capsule.** Unity has no native cylinder-shaped collider - the
  default Cylinder primitive gets a Capsule Collider, rounded ends included. The Debug Color gizmo draws
  the real collider shape, so a "cylinder" zone will show (and behave) like a capsule.

## Credits

Initially built starting from the concept behind Alexander Scott's
[Enhanced Trigger Box](https://github.com/alexander-scott/Enhanced-Trigger-Box) - a trigger volume driven by
composable conditions and actions. This package has since diverged into its own architecture (Conditions,
Actions, and Variables as separate composable components, a Condition Watcher for non-physical triggers,
custom Inspector tooling, etc.), but the original idea is worth crediting.
