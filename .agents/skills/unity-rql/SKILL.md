---
name: unity-rql
description: |-
  How to read and change anything in the Unity Editor with RQL (rql-query / rql-mutate / rql-schema): the query
  language, operators, sources (scene objects, assets, scenes, prefabs, console logs, Editor state, Project Settings), Editor operations
  (create, add components, scenes, prefabs, assets, selection, Play Mode) and what installed extensions add.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---
# RQL — query and change the Unity Editor

RQL is one pipeline language for the whole Editor: find objects, read members, filter, aggregate, and change them in one
call. Prefer one precise query over many exploratory calls.

- `rql-query` — read-only queries, compact table results.
- `rql-mutate` — queries ending with `set` / `call`; all-or-nothing, undoable (Ctrl+Z), `dry_run: true` previews.
- `rql-schema` — sources and component types in the scene; `rql-schema(type)` lists one type's members and methods.

```bash
unity-mcp-cli run-tool rql-query --input '{"query": "objects | where name contains \"Enemy\" | select $ref name"}'
unity-mcp-cli run-tool rql-mutate --input '{"query": "objects | where name == \"Player\" | set active = false"}'
```

## Language

```
RQL: source | stage | stage ...
Sources: objects (every GameObject in the loaded scenes, inactive included), roots, scenes, selection, prefab (the prefab open in prefab editing mode), console (Editor log entries), shaders, componentTypes, profiler (memory / rendering / frame snapshots; call Start / Stop / Save), tests (test methods; run a selection with tests-run query), testResults (last run), editor (Editor state and operations), settings (Project Settings: physics, time, quality, player, build). Assets: unity.assets("t:Material") or unity.assets("t:Prefab", "Assets/Enemies"), unity.asset("Assets/Mats/Red.mat"). ref("Light#1234") resolves a $ref; unity.find("Player") or unity.find("Level/Pillar_1") a GameObject by unique name or path.
Stages:
  where <condition>                  filter (a boolean: where sharedMaterial != null, not where sharedMaterial)
  select a b.c alias=(expr)          project columns (wrap computed expressions in parentheses)
  sort <expr> [desc], ...            stable sort (computed keys in parentheses: sort (a * b) desc)
  first N | skip N | count
  get <path>                         replace each item by a member (lists are flattened)
  recurse children [max-depth=N] [include-self=false]   walk the hierarchy (input items included by default)
  distinct [expr] | sum expr | min expr | max expr | avg expr   (distinct keeps whole items; values: get tag | distinct)
  group-by expr                      rows key|count; group-by a, b (or kind=$type) gives one column per key, then count;
                                     items = the group's items: … | where $ is Light | group-by type | select key count avg=(items.avg(intensity))
Expressions: == != < <= > >= and or not + - * / %; contains, starts-with, ends-with (case-insensitive); matches "regex"; x in [a, b]; x is null; x is not null; $ is Light (includes subclasses); has(member); c ? a : b or if(c, a, b) (only the taken branch runs; where a stage takes one expression, wrap it: select kind=($ is Light ? "light" : "other")). Literals: numbers, "strings", true/false/null, lists [a, b], objects {x: 1, y: 2}. Text functions: lower upper len, replace(s, from, to) (case-insensitive), replace-regex(s, pattern, repl) ($1 = group 1), substring(s, start[, length]), split(s, sep), trim(s), pad(v, width[, fill]) — also as methods: name.replace("Old", "New"), name.substring(0, 4).
Paths: a.b.c, list[0], map["key"]; .member on a list maps over its elements. List methods: .any(pred) .all(pred) .count([pred]) .sum(e) .avg(e) .min(e) .max(e) .first([pred]); inside them bare names refer to the element, falling back to the outer item.
Meta: $ (current item), $ref (stable id such as Light#1234), $type, $path (the item's place in this query, e.g. roots[0].children[2]), $index, $depth. Enums compare to strings (type == "Spot", or the Inspector's name: shadowCastingMode == "Two Sided"); == on strings is case-sensitive.
Unity: GameObject has components (list), children (list), active, name, path (hierarchy path "Level/Props/Crate"), tag, layer, layerName, transform (position, localPosition, eulerAngles, localScale, parent; call LookAt(target) turns it toward a GameObject or a position). Component members are the C# members (public + [SerializeField]); find them via objects | get components | where $ is Light. Vectors/colors print as (x, y, z); write "(1, 2, 3)", [1, 2, 3] or {x: 1, y: 2, z: 3}; colors also "#ff8800". An asset's file: assetPath; its import settings: get importer. Material shader properties: properties["_Color"] (the main color / texture: color, mainTexture); a Material Variant has a parent. A Mesh value also takes a primitive's name ("Cylinder"), a Material value a built-in material's ("Default-Material"). Prefab instances: prefabSource, prefabOverrides (all of the instance's on its root, an object's own on a part). Destroyed objects read as null; a GameObject's missingReferences lists references to deleted assets or objects (where missingReferences.count() > 0). Navigate with paths or get; select only projects output columns.
Results come back as compact rows (header line, then col|col). Long results are truncated — refine with where/first or use count. Unknown members produce warnings with suggestions; rql-schema(type) lists a type's members.
Language rules:
- Conditions are booleans. where, and / or / not, ?: and the predicates of .any() .all() .count() .first() take a comparison or has(), never a bare value: where material != null (or has(material)), where count > 0, where name != "", where len(tags) > 0. A number, string, object or list there fails with type-mismatch; null counts as false.
- cond ? a : b (the same as if(cond, a, b)) picks a value and evaluates only the taken branch: set y = (y < 0 ? 0 : y). Inside select, wrap computed values in parentheses: select kind = (intensity > 2 ? "bright" : "dim").
- Lists compare by value, in order: tags == ["a", "b"], x in [[1, 2], [3, 4]]; distinct and group-by merge equal lists.
- group-by gives rows with key (or one column per key), count and the group's items: group-by kind | select key, count, avg = items.avg(weight); also items.sum(e), items.min(e), items.max(e), items.count(pred), items.first().name. Group by the item's type with $type (group-by $type); a bare type is a member that most items do not have.
- Receipts list only real changes: a set row whose value stayed the same is left out and counted as unchanged, so no rows with N unchanged means the values already matched.
- call Method(args) runs the method of each item (the items must be objects). call source.Method(args) runs the method of that one source object once per stream item, with $, $index, $path and $depth in the arguments being the item's; every call counts against the target limit, and every argument is checked before the first call (a dry run checks without calling).
- Bulk create: range(start, end[, step]) streams numbers (end excluded), and a list literal streams its elements, so one call creates many objects: range(0, 150) | call editor.CreateGameObject("Cube_" + $, {primitive: "Cube", position: [($ % 15) * 2, 0, floor($ / 15) * 2]}) builds a 15-wide grid, [["Crate", [0, 0, 0]], ["Barrel", [2, 0, 0]]] | call editor.CreateGameObject($[0], {primitive: "Cube", position: $[1]}) one object per entry. The receipt has one row per call with the $ref of the created object.
- Vectors and colors the host marks as component-wise compute component by component: set Scale = Scale * 1.5, Position + [0, 1, 0], Position - Offset, -Velocity, v / 2. Adding a number to a vector is a type-mismatch (add [x, y, z] instead); quaternions and other types without that mark stay type-mismatch. Lists of numbers are vectors too: [x, 0, z] * k, [1, 2] + [3, 4]. Vector functions work on both: length(v) (or v.length(), magnitude), normalize(v), distance(a, b), dot(a, b), lerp(a, b, t); len(x) stays the size of a string or list.
- Trigonometry is in radians: sin, cos, tan, asin, acos, atan, atan2(y, x), pi(), deg(x), rad(x); trigonometric results are rounded (sin(pi()) is 0). In a set value or call argument $count is the number of items entering the stage ($index runs 0 to $count - 1), so set position = [5 * cos(2 * pi() * $index / $count), 0, 5 * sin(2 * pi() * $index / $count)] lays the items out on a circle of radius 5.
- A missing map entry reads as null: props["k"] == null and props["k"] is null are true without the entry, and has(props["k"]) tests whether it exists. A misspelt member is not null: it stays unknown and is reported.
- Arguments by name: call Method({name: value, ...}) with one object literal that no overload takes as a whole binds each key to the parameter of that name; parameters left out keep their defaults, and an unknown name fails listing the parameters: range(0, 10) | call editor.CreateGameObject({name: "Tile" + $, primitive: "Cube", position: [$ * 2, 0, 0]}). Engine objects (scene objects, nodes, actors) are never built from an object literal: pass an existing one, e.g. ref("Type#id"). Value-like assets and owned subobjects that the host marks are built from a typed literal: set mesh = {$type: "TypeName", size: 2}.

```

## Changing things (rql-mutate)

```
  set path = expr, ...   assign members (paths may go through structs: set transform.position.y = transform.position.y + 1)
                         values see each target: $ and $index (its position in the stream entering set / call)
  call Method(args)      invoke a method the tools expose
Examples:
  objects | where name == "Player" | set transform.position = "(0, 1, 0)"
  objects | get components | where $ is Light | set intensity = intensity * 2, color = "#ffcc88"
  objects | where tag == "Enemy" | set active = false
  objects | where name starts-with "Pillar_" | set name = name.replace("Pillar_", "Column_")   bulk rename
  objects | where name starts-with "Crate" | sort name | set transform.position.x = $index * 2   spread along x
  objects | where name starts-with "Coin" | set transform.localScale = transform.localScale * 1.5   vectors / colors compute per component (a color's alpha too)
  objects | where name starts-with "Lamp" | set transform.position = transform.position + [0, 2.5, 0]
  objects | where name starts-with "Rock" | set transform.position = [transform.position.x, 0, transform.position.z] * 2   lists of numbers are vectors
  objects | where name starts-with "Enemy" and distance(transform.position, unity.find("Player").transform.position) < 10 | set active = false   (units: meters)
  objects | where name == "Drone" | set transform.position = transform.position + normalize(transform.forward) * 3   also length(v), dot(a, b), lerp(a, b, t)
  unity.asset("Assets/Mats/Red.mat") | get properties | where Key ends-with "Color" | set Value = "#ffffff"   map entries write through
  unity.asset("Assets/Mats/Red.mat") | set properties["_Color"] = "(1, 0, 0, 1)"
  unity.asset("Assets/Mats/Red.mat") | set parent = unity.asset("Assets/Mats/Base.mat")   make it a Material Variant of Base
  unity.asset("Assets/Data/Items.asset") | get rows | where Category == "Tool" | set Price = round(Price * 1.15)   data asset (ScriptableObject list rows)
  objects | where name starts-with "Pillar" | get components | where $ is MeshFilter | set sharedMesh = "Cylinder"   built-in mesh
  objects | get components | where $ is MeshRenderer and sharedMaterial == null | set sharedMaterial = "Default-Material"   built-in material
  objects | where name == "Crate" | get components | where $ is MeshRenderer | set sharedMaterial = {$type: "Material", color: "#ff8800"}   a new material (saved with the scene)
  objects | where name == "Shop" | get components | where $ is ShopStock | set item = {$type: "ItemData", price: 3}   a new data ScriptableObject
  unity.asset("Assets/Art/hero.png") | get importer | set textureType = "Sprite", spriteImportMode = "Single"   import settings (reimported)
  objects | where name == "Spawner" | set targets = append(targets, ref("GameObject#1234"))
Create / add / remove (call; the receipt's result column holds the new object's $ref for follow-up queries):
  editor | call CreateGameObject("Props")                                   new root GameObject
  editor | call CreateGameObject("Crate", ref("GameObject#1234"), "Cube")    child of a parent, optional primitive
  range(0, 300) | call editor.CreateGameObject("Cube_" + $, {primitive: "Cube", position: [($ % 15) * 2, 0, floor($ / 15) * 2]})   with max_targets=300:
                         bulk create, one call per item: a 15x20 grid in ONE request and ONE Undo step; the receipt lists the first 50 new objects' $refs (query the rest by name)
  [["Crate", [0, 0, 0]], ["Barrel", [2, 0, 0]]] | call editor.CreateGameObject($[0], {primitive: "Cube", position: $[1]})   one per entry
                         {placement}: primitive, parent: ref(…), position / rotation (Euler degrees) / scale as [x, y, z], local to the parent
  editor | call CreateGameObject({name: "Crate", primitive: "Cube", position: [1, 0, 2], scale: [2, 2, 2]})   arguments by name: one literal, any order, the rest default
  objects | where name == "Crate" | call AddComponent("Rigidbody")           add a component by type name
  objects | where name == "Old" | call Destroy()                             remove a GameObject (components: get components | where $ is X | call Destroy())
  objects | where name == "Lamp" | set transform.parent = ref("GameObject#1234")   reparent (keeps the world position)
  objects | where name == "Crate" | call Duplicate()                          copy next to it (returns the copy)
  objects | where name == "Main Camera" | call LookAt(unity.find("Player"))   aim at a GameObject (or a position "(0, 1, 0)")
Scenes, prefabs, assets, Editor state:
  editor | call OpenScene("Assets/Scenes/Level.unity")          editor | call NewScene("Assets/Scenes/New.unity")
  scenes | where path == "" | call SaveAs("Assets/Scenes/New.unity")   scenes | where name == "Level" | call SetActive()
  editor | call InstantiatePrefab("Assets/Prefabs/Enemy.prefab", {parent: ref("GameObject#1234"), position: [0, 0, 5]})
  range(0, 10) | call editor.InstantiatePrefab("Assets/Prefabs/Coin.prefab", {position: [$ * 2, 1, 0]})   ten instances, one Undo step
  objects | where name == "Crate" | call SaveAsPrefab("Assets/Prefabs/Crate.prefab")   (on a prefab instance: a Prefab Variant)
  objects | where name == "Coin_3" | call RevertPrefabOverrides()   or ApplyPrefabOverrides() (read: select prefabSource prefabOverrides)
  editor | call OpenPrefab("Assets/Prefabs/Enemy.prefab")  then  prefab | recurse children | …  then  editor | call ClosePrefab()
  editor | call CreateMaterial("Assets/Mats/Glow.mat")        editor | call MoveAsset("Assets/A.mat", "Assets/Mats/A.mat")
  editor | call RefreshAssets()   after files changed on disk: imports them, reloads a changed open scene (keeps one with unsaved edits and says so)
  editor | set selection = [ref("GameObject#1234")]         editor | set isPlaying = true   (scripts reload: retry a call that fails right after)
  editor | call AddLayer("Pickups")  then  objects | where name starts-with "Pickup" | set layerName = "Pickups"   (read: editor | select layers tags)
  editor | call AddTag("Interactive")  then  objects | where name starts-with "Barrel" | set tag = "Interactive"
  profiler | call Start()      profiler | call Save("Logs/profile.json")      (read: profiler | select memory frame)
  settings | set physics.gravity = "(0, -12, 0)", time.fixedDeltaTime = 0.01   Project Settings (saved, undoable)
  settings | set build.scenes = append(build.scenes, "Assets/Scenes/Level2.unity")   scenes in the build
```

Agent rules: Deleting (DeleteAsset, Destroy, script-delete, or AssetDatabase / File deletes in script-execute) takes two turns unless the user named the exact items or you created them yourself in this task: first list the exact candidates (rql-query, a dry_run=true mutation or a read-only snippet), show them and end your turn; delete only that list, in a separate call, after the user confirms. Otherwise never find and delete candidates in the same turn. After project files change outside the Editor (written to disk by you, a script or the user), run editor | call RefreshAssets() before reading or changing them: it imports them and reloads an open scene whose file changed (changed .cs files recompile and the scripts reload: retry a call that fails right after). When you map a name the user misspelt to a real one (an object, component or member), say so. Unity units are meters, degrees (rotation) and seconds.


## Sources

| Source | What |
|---|---|
| `animation` | animation: animators (every Animator in the loaded scenes), CreateClip(path, frameRate, loop), CreateController(path, motion) |
| `componentTypes` | component types that can be added (name fullName namespace) |
| `console` | captured Editor log entries, oldest first (last 1000): logType message timestamp stackTrace sequence |
| `editor` | Editor state (isPlaying, isPaused, selection …) and operations: CreateGameObject, OpenScene, NewScene, SaveScenes, InstantiatePrefab, OpenPrefab / SavePrefab / ClosePrefab, CopyAsset / MoveAsset / DeleteAsset / CreateFolder, CreateMaterial, SaveAssets, RefreshAssets, AddTag / AddLayer, ClearConsole, ExecuteMenuItem (rql-schema "editor" for signatures) |
| `lighting` | the active scene's lighting: environment (ambient, fog, skybox, sun), Lighting Settings asset, bake (rql-schema "lighting" for members and calls) |
| `objects` | every GameObject in the loaded scenes, depth-first, inactive ones included |
| `prefab` | root GameObject of the prefab open in prefab editing mode (none otherwise) |
| `probuilder` | ProBuilder: meshes (every ProBuilderMesh in the loaded scenes), CreateShape(type, name, size, position, rotation, parent), CreatePolyShape(points, height, name, position, flipNormals, parent), Merge(meshes, deleteSources) |
| `profiler` | Unity profiler: enabled supported memory rendering script frame modules (live snapshots, MB / ms); calls Start, Stop, ClearFrames, EnableModule, Save, Load (rql-schema "profiler" for signatures) |
| `roots` | root GameObjects of the loaded scenes |
| `scenes` | loaded scenes |
| `selection` | objects selected in the Editor |
| `settings` | Project Settings pages, writable and undoable, saved to ProjectSettings/: physics (gravity, defaultContactOffset, solver iterations …), time (fixedDeltaTime, timeScale …), quality (levels, currentLevel, vSyncCount, shadows …), player (productName, companyName, bundleVersion, colorSpace …), build (scenes in the build), e.g. settings \| select physics.gravity time.fixedDeltaTime |
| `shaders` | every shader the Editor knows (built-in and project): name supported hasErrors |
| `testResults` | results of the last tests-run: name status durationMs message stackTrace |
| `tests` | test methods (fullName name fixture namespace assembly mode categories parameterized explicit ignored); run a selection with tests-run query, e.g. tests \| where fixture == "PlayerTests" |
| `probuilder.edges(…)` | probuilder.edges(mesh, faces?): distinct edges [[a, b], …] of the faces (index list; default every face), for Bevel / SubdivideEdges / ConnectEdges |
| `probuilder.facing(…)` | probuilder.facing(mesh, direction): indices of the faces pointing Up, Down, Left, Right, Forward, Back or Other (local space, within 45°) |
| `probuilder.openEdges(…)` | probuilder.openEdges(mesh): border edges [[a, b], …] (a face on one side only: around holes, plane borders), for Bridge |
| `probuilder.vertices(…)` | probuilder.vertices(mesh, start?, count?): [{index, position}] local vertex positions (at most 20 per call; default 0, 20) |
| `unity.asset(…)` | unity.asset(path): the main asset at a path, e.g. "Assets/Mats/Red.mat" |
| `unity.assets(…)` | unity.assets(filter, folder?): AssetDatabase search, e.g. "t:Material", "t:Prefab Enemy" (at most 200, loaded as needed) |
| `unity.find(…)` | unity.find(name): the GameObject with that unique name or hierarchy path ("Level/Pillar_1"), e.g. set Follow = unity.find("Player") |

## Animation

Clips and controllers are assets: `unity.assets("t:AnimationClip")`, `unity.asset("Assets/…/Walk.anim")`,
`unity.asset("Assets/…/Player.controller")`. An AnimationClip has `curves` (path type property keyCount timeRange keys, keys with
writable time value inTangent outTangent …), `objectCurves` (sprite swaps), `events` and `settings` (loopTime loopBlend mirror …), all
writable views, plus ordinary members (frameRate wrapMode legacy length isLooping). An AnimatorController has `layers` (name
defaultWeight blendingMode defaultState `states` anyStateTransitions) and `parameters` (name type defaultFloat defaultInt defaultBool);
states and transitions are assets whose members (speed motion tag … / hasExitTime exitTime duration conditions …) are set directly.
Writes and calls are undoable; creating an asset is not. `rql-schema "AnimationClip"` / `"AnimatorController"` list the methods.

Clips (curve `path` is the child path under the Animator, "" = its own GameObject; `type` the animated component):

```
animation | call CreateClip("Assets/Animations/Walk.anim")   empty clip, 60 fps
animation | call CreateClip("Assets/Animations/Idle.anim", 30, true)   path, frameRate, loop
unity.assets("t:AnimationClip") | select name length frameRate isLooping n=(curves.count()) e=(events.count())
unity.asset("Assets/Animations/Walk.anim") | call SetCurve("", "Transform", "localPosition.y", [[0, 0], [0.5, 1], [1, 0]])   path, type, property, keys [time, value(, inTangent, outTangent)]
unity.asset("Assets/Animations/Walk.anim") | call SetCurve("Body", "SpriteRenderer", "m_Color.a", [{time: 0, value: 1}, {time: 1, value: 0, inTangent: -1}])
unity.asset("Assets/Animations/Walk.anim") | get curves | select path type property keyCount timeRange
unity.asset("Assets/Animations/Walk.anim") | get curves | where property == "localPosition.y" | get keys | select index time value inTangent outTangent
unity.asset("Assets/Animations/Walk.anim") | get curves | where property == "localPosition.y" | set keys[1].value = 2, keys[1].outTangent = 0
unity.asset("Assets/Animations/Walk.anim") | get curves | where property == "localPosition.y" | call AddKey(1.5, 0.5)   returns the key index; RemoveKey(index), SetKeys([[t, v], …]), Evaluate(t)
unity.asset("Assets/Animations/Walk.anim") | get curves | where path == "Body" | set postWrapMode = "Loop"
unity.asset("Assets/Animations/Walk.anim") | call RemoveCurve("Body", "SpriteRenderer", "m_Color.a")   or … | get curves | where … | call Remove()
unity.asset("Assets/Animations/Walk.anim") | set frameRate = 30, settings.loopTime = true
unity.asset("Assets/Animations/Walk.anim") | call AddEvent(0.5, "Footstep", "left")   time, functionName, stringParameter?, floatParameter?, intParameter?
unity.asset("Assets/Animations/Walk.anim") | get events | select index time functionName stringParameter
unity.asset("Assets/Animations/Walk.anim") | set events[0].time = 0.25, events[0].intParameter = 1
unity.asset("Assets/Animations/Walk.anim") | call RemoveEvent(0)   ClearEvents() / ClearCurves() remove all
```

Animator controllers (states by name; a trailing `layer` argument is a layer name or index, default 0):

```
animation | call CreateController("Assets/Animations/Player.controller")   Base Layer, empty state machine (CreateController(path, motion) adds a state)
unity.asset("Assets/Animations/Player.controller") | call AddParameter("Speed", "Float")   Float, Int, Bool or Trigger
unity.asset("Assets/Animations/Player.controller") | call AddParameter("Grounded", "Bool", true)   optional default value
unity.asset("Assets/Animations/Player.controller") | call AddParameter("Jump", "Trigger")
unity.asset("Assets/Animations/Player.controller") | call AddState("Idle", unity.asset("Assets/Animations/Idle.anim"))   name, motion?, layer?; the first state becomes the default
unity.asset("Assets/Animations/Player.controller") | call AddState("Walk", unity.asset("Assets/Animations/Walk.anim"))
unity.asset("Assets/Animations/Player.controller") | call AddTransition("Idle", "Walk", "Speed", "Greater", 0.1)   from, to ("Exit" = exit), parameter?, mode?, threshold?, layer?
unity.asset("Assets/Animations/Player.controller") | call AddTransition("Walk", "Idle", "Speed", "Less", 0.1)
unity.asset("Assets/Animations/Player.controller") | call AddAnyStateTransition("Idle", "Jump")   mode defaults by parameter type: Bool / Trigger If, Float Greater, Int Equals
unity.asset("Assets/Animations/Player.controller") | call SetDefaultState("Idle")
unity.asset("Assets/Animations/Player.controller") | get layers | select index name defaultWeight defaultState.name states.name
unity.asset("Assets/Animations/Player.controller") | get layers | get states | select name speed motion.name transitions.destinationState.name
unity.asset("Assets/Animations/Player.controller") | get layers | get states | where name == "Walk" | set speed = 1.5
unity.asset("Assets/Animations/Player.controller") | get layers | get states | where name == "Idle" | get transitions | set hasExitTime = true, exitTime = 0.9, duration = 0.25
unity.asset("Assets/Animations/Player.controller") | get layers | get states | where name == "Idle" | get transitions | call AddCondition("Grounded", "If")   RemoveCondition(index), ClearConditions()
unity.asset("Assets/Animations/Player.controller") | get layers | get states | get transitions | select destinationState.name hasExitTime conditions
unity.asset("Assets/Animations/Player.controller") | get parameters | select name type defaultFloat defaultBool
unity.asset("Assets/Animations/Player.controller") | get parameters | where name == "Speed" | set defaultFloat = 0.5
unity.asset("Assets/Animations/Player.controller") | call AddLayer("Upper Body")   returns the layer index
unity.asset("Assets/Animations/Player.controller") | set layers[1].defaultWeight = 0.5, layers[1].blendingMode = "Additive"
unity.asset("Assets/Animations/Player.controller") | call AddState("Wave", null, "Upper Body")
unity.asset("Assets/Animations/Player.controller") | call RemoveTransition("Walk", "Idle")   from "AnyState" / to "Exit" for those transitions
unity.asset("Assets/Animations/Player.controller") | call RemoveState("Wave", "Upper Body")
unity.asset("Assets/Animations/Player.controller") | call RemoveLayer("Upper Body")
unity.asset("Assets/Animations/Player.controller") | call RemoveTransition("AnyState", "Idle")
unity.asset("Assets/Animations/Player.controller") | call RemoveParameter("Jump")   conditions using it are removed too
```

Animator components (`animation | get animators` = every Animator in the loaded scenes):

```
editor | call CreateGameObject("Player")
objects | where name == "Player" | call AddComponent("Animator")
animation | get animators | set runtimeAnimatorController = unity.asset("Assets/Animations/Player.controller"), applyRootMotion = false
animation | get animators | select gameObject.name runtimeAnimatorController.name isHuman cullingMode
```

Parameter names and types are read-only (conditions refer to them): rename by RemoveParameter + AddParameter. New transitions
have no exit time and no blend duration; set them on the transition. Only the Animator's configuration members are readable (its
playback state is not meaningful in Edit Mode); in Play Mode `call SetParameter("Speed", 2)` / `call Play("Walk")` drive it.
Other components' AnimationCurve fields take keyframe lists too: `set curve = [[0, 0], [1, 1]]`.

## Particle systems

Particle systems are components: `objects | get components | where $ is ParticleSystem`. Module structs (`main`,
`emission`, `shape` …) are written through, even though they are get-only properties:

```
objects | get components | where $ is ParticleSystem | set main.startLifetime = 3, main.loop = false
objects | get components | where $ is ParticleSystem | set emission.rateOverTime = [5, 25]          random between two
objects | get components | where $ is ParticleSystem | set main.startColor = "#ff8800"
objects | get components | where $ is ParticleSystem | set main.startColor = [["#ff0000", 0], ["#0000ff", 1]]   gradient
```

`MinMaxCurve` takes a number or `[min, max]`; `MinMaxGradient` a color, `[color, color]` or gradient keys.
Playback in the Editor: `objects | get components | where $ is ParticleSystem | call Play()` (also Pause, Stop, Clear,
Simulate(seconds)).

## ProBuilder

ProBuilder meshes are components: `probuilder | get meshes` (= `objects | get components | where $ is ProBuilderMesh`). A mesh
reads as faceCount vertexCount edgeCount triangleCount, `bounds` (local) / `worldBounds`, `directions` (face indices per local
direction), `materials` (slots: index material faceCount) and `faces` (read-only views: index direction center normal
submeshIndex material vertices edges); the raw vertex arrays are not members. Edit with `call` (rql-mutate): faces are index lists
`[0, 1]`, edges vertex index pairs `[[0, 1], [1, 2]]`. `probuilder.facing($, "Up")`, `probuilder.edges($, faces?)`,
`probuilder.openEdges($)` and `probuilder.vertices($, start?, count?)` compute selections (also as call arguments). The receipt's
result column holds the created mesh's $ref: `ref("ProBuilderMesh#…") | call Extrude([0], 1)`.

Create and inspect:

```
probuilder | call CreateShape("Cube", "Crate", "(2, 1, 2)", "(0, 0.5, 0)")   type, name?, size?, position?, rotation?, parent?
probuilder | call CreateShape("Cube", "Box", null, "(3, 0.5, 0)")   types: Cube Stair CurvedStair Prism Cylinder Plane Door Pipe Cone Sprite Arch Sphere Torus
probuilder | call CreateShape("Cylinder", "Block", "(1, 2, 1)", "(6, 1, 0)")
probuilder | call CreatePolyShape(["(0, 0)", "(4, 0)", "(4, 3)", "(0, 3)"], 0.5, "Floor", "(-6, 0, 0)")   points (x, z), height, name?, position?, flipNormals?, parent?
probuilder | get meshes | select $ref gameObject.name faceCount vertexCount edgeCount triangleCount bounds.size
probuilder | get meshes | where gameObject.name == "Crate" | select directions materials worldBounds.center
probuilder | get meshes | where gameObject.name == "Crate" | get faces | select index direction center normal vertexCount submeshIndex material.name
probuilder | get meshes | where gameObject.name == "Crate" | get faces | where direction == "Up" | select index center edges
probuilder | get meshes | where gameObject.name == "Crate" | select up=(probuilder.facing($, "Up")) edges=(probuilder.edges($, [0])) v=(probuilder.vertices($, 0, 4))
```

Faces (indices shift after DeleteFaces; Extrude appends the new side faces and returns their indices):

```
editor | call CreateMaterial("Assets/Materials/Grass.mat")
probuilder | get meshes | where gameObject.name == "Crate" | call SetFaceMaterial(probuilder.facing($, "Up"), "Assets/Materials/Grass.mat")   faces, material (path, unity.asset(…) or ref); returns the submesh index
probuilder | get meshes | where gameObject.name == "Crate" | get materials | select index material.name faceCount
probuilder | get meshes | where gameObject.name == "Crate" | call Extrude(probuilder.facing($, "Up"), 0.5)   faces, distance (negative = inward), method? FaceNormal | IndividualFaces | VertexNormal
probuilder | get meshes | where gameObject.name == "Crate" | call Extrude(probuilder.facing($, "Forward"), 0.25, "IndividualFaces")
probuilder | get meshes | where gameObject.name == "Crate" | call FlipNormals([0])   faces? (every face when omitted)
probuilder | get meshes | where gameObject.name == "Box" | call DeleteFaces(probuilder.facing($, "Up"))   at least one face must remain
probuilder | get meshes | where gameObject.name == "Box" | select open=(probuilder.openEdges($))
probuilder | get meshes | where gameObject.name == "Box" | call Bridge(probuilder.openEdges($)[0], probuilder.openEdges($)[2])   two open edges [a, b], allowNonManifold?; returns the new face index
```

Edges, pivot, merge:

```
probuilder | get meshes | where gameObject.name == "Block" | call Bevel(probuilder.edges($, probuilder.facing($, "Down")), 0.1)   edges, amount 0 … 1 (exclusive); returns the new face indices
probuilder | get meshes | where gameObject.name == "Block" | call SubdivideEdges(probuilder.edges($, [0]), 1)   edges, subdivisions (1 = split in half); returns the vertices added
probuilder | get meshes | where gameObject.name == "Block" | call ConnectEdges(probuilder.edges($, [1]))   at least 2 edges sharing faces; returns the new face indices
probuilder | get meshes | where gameObject.name == "Floor" | call SetPivot("Center")   "Center" (bounds) or "FirstVertex"; the geometry stays in place
probuilder | get meshes | where gameObject.name == "Floor" | call SetPivotAt("(-6, 0, 0)")   a world position
probuilder | call Merge([meshes.first(gameObject.name == "Crate"), meshes.first(gameObject.name == "Box")])   refs to meshes or GameObjects (first = target), deleteSources? (default true)
```

Directions are in the mesh's local space (normal within 45° of the axis; the rest is Other). Invalid indices, edges that are not
edges of the mesh, amounts outside the range and deleting every face fail without changing anything. Every call is undoable
(the mesh is rebuilt after Undo / Redo); a created material asset stays.

## Installed RQL modules

- `com.IvanMurzak.Unity.MCP.Editor.Rql.Modules.UnityCoreModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.ToolTypesModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics.MathematicsModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Animation.AnimationModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting.LightingModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Particles.ParticleSystemModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D.PhysicsModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder.ProBuilderModule`


> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

