# Dune-Inspired Worm Riding Game — Design & Technical Reference

Status: early design/prototyping phase. This doc consolidates all decisions made so far for handoff/tracking on the Unity client side.

---

## 1. Tooling Setup

- **Blender MCP** — via `blender-mcp`, run through `cmd /c uvx blender-mcp` on Windows. Gives direct scene manipulation in Blender, plus asset pulls from Poly Haven and Sketchfab.
- **Unity MCP** — CoplayDev's `MCPForUnity` package, installed via Package Manager → git URL. Runs an HTTP server on `localhost:8080` for live editor control.
- Intended pipeline: Blender MCP handles worm mesh creation, rigging, and hero prop/set-dressing assets. Unity MCP handles scene scaffolding, shader/script setup, and live editor control — avoiding manual file round-tripping between the two apps.

---

## 2. Core Concept

A Dune-inspired sandworm riding game. Player explores a dune field, lures a sandworm using a thumper, then mounts and steers it using hand-hooks on its segment rings — echoing the "maker hooks" mechanic from Dune lore. Designed for both standard third-person and VR (Pico 4) from the start, in parallel rather than retrofitted.

---

## 3. Sandworm — Model & Rig

- **Body structure:** segmented body (ring segments), suited to a Chain IK rig.
- **Rig approach:** Unity's **Animation Rigging** package using **Chain IK constraints** — chosen over the legacy Animator-based IK, which only suits humanoid rigs.
- **Movement:** spline-based, using Unity's Splines package. The worm's body follows a spline curve; segments trail along it.
- Alternative/fallback for heavier rig needs: the third-party **Final IK** asset.
- **Mesh sourcing options considered:**
  - AI text-to-3D generation (Meshy.ai, Tripo3D, Luma Genie, CSM) then clean up/retopo in Blender.
  - Hand-sculpt in Blender from scratch (organic shape, beginner-friendly).
  - Free Sketchfab base model (Creative Commons) modified in Blender.
  - Placeholder capsule chain for early prototyping while other systems are built.
  - Typical pipeline regardless of source: generate → retopology → UV unwrap → texture → rig → animate → import to Unity.
- Not yet finalized: which mesh-sourcing route to take (decision deferred; prototyping proceeded with placeholder-first approach in mind).

---

## 4. Terrain

**Decision: build terrain in Unity, not Blender.**

Reasoning:
- A Blender-authored terrain would be a static mesh — fighting against the runtime deformation needed for the worm-carves-sand mechanic.
- Unity's **Terrain** component is heightmap-based natively, which is exactly the data format the deformation techniques below need.
- Blender's terrain role is limited to (optionally) baking a grayscale heightmap PNG for import, or building hero rock formations / hand-placed set-dressing meshes — not the live terrain itself.

**Explicit scope decision:** Don't over-invest in terrain cosmetics (hill silhouette tuning, dune ridge shaping, multi-layer texture splatting). Since the worm's dynamic deformation dominates what the player actually sees during gameplay, get a "good enough" dune scaffold fast and put the real effort into the deformation/anticipation systems instead. A single decent sand material (base color + normal + grain noise) is enough; no need for elaborate splat maps.

Options discussed for getting the dune silhouette right (lower priority per above, but documented for later):
- Ridge noise (abs value of Perlin, inverted) for parallel ridge lines (barchan/transverse dune pattern).
- Domain warping to break up overly regular ridge lines.
- Asymmetric height falloff per ridge to fake windward (gentle) vs. leeward (steep slip-face) slopes.
- Could be built as a `TerrainData.SetHeights()`-driven C# editor tool with sliders (ridge frequency, warp strength, height, wind direction).
- Faster alternative: Unity's Terrain Tools package erosion/stamp brushes for hand-placed control over key dunes.
- Note for the worm corridor: keep slopes gentle enough along the worm's actual travel paths, since very steep slip-faces can make emergence/deformation read oddly if the worm has to "climb" a near-vertical face.

---

## 5. Dynamic Sand / Deformation System

This is the priority system — where the actual build effort should go.

**Displacement/deformation ("worm carving through sand"):**
- Render-texture-based deformation: a top-down camera renders the worm's position/depth into a RenderTexture, fed into a terrain shader as a heightmap offset. Cheap — just a texture update, not per-vertex physics.
- For persistent trenches ("the worm reshapes the dune"): actual runtime `TerrainData.SetHeights()` calls around the worm's position each frame (throttled/interpolated) — costlier, used for lasting deformation.
- Only track displacement within a surface-proximity radius of the worm — no need to deform sand while the worm is fully underground, which keeps the render-texture approach performance-friendly.

**Surface shading:**
- Layered shader: fine noise-based normal map + subtle parallax/height map for a granular (not solid) look.
- Sparkle/glint shader — small high-frequency specular noise that shifts with camera angle — sells "sand" strongly.

**Particles:**
- GPU particles (VFX Graph) for sand cascading off the worm's sides as it surfaces/dives, emission shape driven off the worm's spline curve.
- A "sand fog"/dust veil particle layer for the emergence burst moment.

**Traveling bow-wave ripple:**
- A ripple/wave shader ahead of the worm (like a bow wave in water) to sell "something massive moving under here" before it's visible — ties into the tremor-sensing/anticipation gameplay.

---

## 6. Core Gameplay Loop

1. **Thumper placement phase** — player roams the dune field and places a thumper. Raycast from camera/cursor to terrain, snapped to surface normal so it doesn't clip on slopes. Potential strategic depth: placement could matter for line-of-sight to the worm's entry point, distance from hazards, or proximity to an objective (left as a hook for later, not yet built out).

2. **Thumper activates → worm is called** — worm spawns off-screen or at a fixed minimum distance (not directly under the thumper) to create a travel-time anticipation window. Worm's spline target becomes the thumper position; approach speed tuned to give enough time for the anticipation beats below.

3. **Anticipation beats** — escalate as the worm approaches:
   - **Sand vibration:** shader-based jitter on the terrain surface, amplitude scaled by `1/distance` (clamped) to the worm, driven by a single float uniform updated from C# each frame.
   - **Camera shake:** distance-driven scaling — low-frequency/low-amplitude early, ramping to sharper high-frequency shake near the end. Cinemachine Impulse system: continuous ambient rumble + a strong one-shot impulse for the crash.
   - **Audio:** rising low-frequency drone/heartbeat and distant reverberating pounding as the worm nears.
   - **Pre-crash tell:** sand bulges upward along the worm's approach path (a soft heightmap bump) just before the actual burst.

4. **The crash** — hard cut in intensity from the buildup: large-radius `SetHeights()` crater/burst, strong one-shot Cinemachine impulse, sand particle burst, worm mesh punching through with an emergence animation. A brief hitstop/time-dilation (a few frames at or near-0 timescale) right at breach adds weight cheaply.

**Suggested build order:** get the distance-driven vibration + camera shake loop working first with a placeholder worm (even a capsule following the spline) before bringing in the actual worm mesh/rig.

---

## 7. Mounting & Hooking Mechanic

Based on Dune's "maker hooks" — the player doesn't just stand on the worm, they actively hook its segment rings to keep it surfaced and steer it.

**Mounting:**
- Once the worm breaches and settles into a "surfaced" state, a mount trigger (collider along exposed dorsal segments + player proximity + input prompt) switches control from free-roam third-person to "on-worm" mode.
- Player is **parented to a moving anchor GameObject** (not the worm mesh directly — spline-driven meshes aren't great physics parents). The anchor tracks the worm's spine position/rotation along the spline each frame; the player's CharacterController position is corrected to follow it.

**Hooking:**
- Each worm ring segment has hook point(s) spaced around the ring. When the player is close enough and triggers the hook input, a raycast/sphere-check against nearby hook points snap-attaches.
- A hook does two things:
  1. Anchors the player's position relative to that segment, so local offset stays consistent as the worm undulates.
  2. **Fights the worm's dive behavior** — the worm has a "want to dive" urge/timer that ticks up over time; each active hook reduces it. If hooks fall below a threshold (player unhooks, or takes too long moving to a new ring), the worm starts diving again, forcing the player to re-hook further along.

**Steering:**
- Hooking near the **front** rings tilts the worm to keep surfacing/rising; hooking further **back** lets that segment sink.
- Abstraction: whichever ring segment currently has the most/frontmost active hook determines a target pitch/lean applied to the spline's head-end, cascading down the body via the Chain IK setup.
- This makes hook position/movement the actual steering input (not a generic joystick-controls-velocity scheme) — closer to the lore and a more interesting mechanic.

**Build order:**
1. Basic mounting first — player parented to a following anchor, walking on a static/slow worm back.
2. Then the "hook reduces dive urge" loop in isolation with a single front hook point (no steering yet) — validates the core risk/reward loop.
3. Steering (hook position → lean → spline head target) last, since it depends on mount + hook systems being solid.

**Input-agnostic design (important architectural decision):**
- Hooking is defined as an abstract event — `OnHookAttempt(Transform targetPoint)` — rather than bound directly to a keypress. Third-person triggers it via button + nearest-hook-point raycast; VR triggers it via a controller/hand entering a hook point's grab volume + grip gesture. Both funnel into the same dive-urge/steering logic, avoiding two parallel mechanic implementations. This was flagged as the most important thing to get right early, since retrofitting VR onto a keyboard-first interaction layer later usually means a rewrite.
- **Recommended build order:** implement the input-agnostic event/interface layer first, get the core loop working in third-person (faster to iterate), then wire XRI grab-interactables to the same events once the loop feels good. Don't build and tune both input paths simultaneously.

---

## 8. VR Support (Pico 4)

- **Foundation:** Unity's **XR Interaction Toolkit (XRI)** — XR Origin rig, hand/controller tracking, grab-interactable system. Hook points map naturally onto `XRGrabInteractable`-style objects.
- Mounting reuses the same "player parented to a following anchor" approach — the anchor drives the XR Origin's transform instead of a CharacterController. Locomotion while mounted is effectively solved (moving with the worm), sidestepping typical VR locomotion-comfort issues.

**Comfort considerations:**
- A large undulating creature + camera shake + unpredictable pitch/roll is a strong VR-sickness risk if the camera inherits all that motion 1:1.
- Mitigations discussed: dampen/partially decouple roll/pitch transmitted to the camera relative to the worm's segment (vs. the whole spline curve's roll); optional vignette/FOV restriction during high-motion moments; treat underground "dive" sequences as a scripted cutaway or heavily comfort-gate them rather than simulating a disorienting first-person underground ride.

**Hook interaction feel in VR:**
- Physical reach-and-grip is the payoff moment — strong feedback on grab matters (haptic pulse, visual/audio "thunk," brief resistance before "setting").
- Stretch goal (not v1): two-handed hooking, one hook per hand, for more granular lean/steering control than third-person offers.

### Pico 4 Haptics

- Pico 4 controllers use a **"HyperSense" motor** and support **broadband haptics** — amplitude, frequency, and duration control, not just fixed on/off pulses (confirmed via web search).
- **Standard cross-platform path (Unity XR Input System):**
  ```csharp
  if (controllerDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities))
  {
      controllerDevice.SendHapticImpulse(0, amplitude, duration);
      // amplitude: 0.0-1.0
      // duration: seconds
  }
  ```
  This path supports amplitude/duration only — no frequency control.

- **PICO native SDK path (for frequency control — needed for the tension/slip differentiation design below):**
  ```csharp
  PXR_Input.SendHapticImpulse(
      PXR_Input.VibrateType.RightController,
      amplitude: 1.0f,   // 0.0-1.0 strength
      duration: 300,      // ms
      frequency: 100       // Hz
  );
  ```

- **Known compatibility issue:** some third-party XR frameworks (reported for UltimateXR) have had haptics silently fail on Pico 4/Neo3 controllers due to a buffer-format mismatch with PICO's motor. Workaround is routing through PICO's native input class rather than the generic XRI/OpenXR haptic call. Worth checking early if haptics don't fire as expected.

**Haptic design mapping (steering intuition):**
- **Directional cue via asymmetry:** independent left/right controller amplitude — stronger vibration on the side the worm is pulling/resisting toward, giving a free spatial cue once both controllers are independently driven.
- **Frequency for proximity to hook-slip threshold:** low, steady frequency = stable hold; rising frequency/pulse rate = approaching the point where the hook slips or the dive-urge is winning — an early warning the player can feel building.
- **Amplitude tied to steering effort:** scale amplitude by how much effort the current steering action is costing (fighting the worm's natural dive vector should feel "harder" through vibration; coasting with momentum should feel calm) — turns the haptic layer into a subtle skill-expression signal.
- **Keep bands separate:** ambient "steering tension" rumble should sit in a distinct amplitude/frequency band from discrete one-shot event pulses (hook connect, hook slip, crash), so the player can distinguish continuous feedback from discrete events without consciously parsing it.

---

## 9. Open / Undecided Items

- Camera/gameplay perspective was raised as a decision point early on (third-person riding on top / first-person worm POV / side-view-2.5D) — third-person was the working assumption used throughout later design discussion, but confirm this is locked.
- Worm mesh sourcing method (AI-generate vs. hand-sculpt vs. Sketchfab base vs. placeholder-first) not yet finalized.
- Thumper placement's strategic depth (line-of-sight, hazard proximity, objective proximity) flagged as a hook for later — not yet designed in detail.
- Two-handed VR hooking is a stretch goal, not scoped for v1.
