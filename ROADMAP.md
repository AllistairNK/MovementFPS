# MovementFPS2026 — Development Roadmap

A movement-focused first-person shooter prototype built in Unity 6 (2026). The core
pillar is fast, physics-based traversal (sprint/crouch/slope movement, wall-running,
grappling) layered under a first-person shooter — combat and content come after the
movement feels great.

Status legend: `[x]` done · `[~]` partial / placeholder in place · `[ ]` not started

---

## Phase 0 — Foundation (done)

- [x] First-person player controller: walk/sprint/crouch, jump, air control, slope
      handling (`PlayerMovement.cs`)
- [x] Mouse-look camera with pitch clamp (`PlayerCam.cs`)
- [x] FPS counter for perf monitoring during movement tuning (`FPSCounter.cs`)
- [x] Gray-box test level: floor, slopes, wall-run cubes, hook-point pillars
      (ProBuilder)
- [x] Fixed physics correctness bugs causing random forward "jumps" and camera
      snaps: frictionless physics material, Update/FixedUpdate desync, slope-check
      hysteresis, SphereCast ground check, Rigidbody interpolation + CCD, clamped
      per-frame mouse rotation

## Phase 1 — Traversal mechanics (in progress)

- [x] Grappling hook (`GrapplingGun.cs`): SpringJoint-based swing, right-click to
      fire, reel in/out (Q/E) with color-coded rope, in-range crosshair indicator
- [x] Grapple test course (`GrappleTestArea`: ascending towers + swing anchors)
- [ ] Wall-running — a `Wallrun` block cluster already exists in the level but has
      no script behind it yet; needs wall-detection raycasts, a wall-run state in
      `PlayerMovement`'s state machine, camera tilt, and a wall-jump
- [ ] Replace placeholder grapple gun box with a real (or better-proportioned)
      viewmodel; fix its low contrast against dark level geometry
- [ ] Grapple polish: swing momentum preservation on release, a max-swing-speed
      cap, a short cooldown/no-spam guard, whiff/miss feedback (sound or crosshair
      flash) when firing at nothing
- [ ] Slide mechanic (crouch-while-sprinting momentum slide), a natural pairing
      with the existing slope-handling code
- [ ] Gliding — an airborne state that slows fall speed and adds forward
      control (parachute/wingsuit-style), likely triggered by holding a key
      while airborne; pairs naturally with grapple-release momentum and the
      ascending tower test course already in `GrappleTestArea`
- [ ] Double jump — a second in-air jump, reusing/extending the existing
      `readyToJump`/`Jump()` flow in `PlayerMovement.cs`, gated behind a pickup
      or unlock if progression is added later
- [ ] Forward boost — a short burst of forward momentum (dash-style impulse
      along look/move direction), usable on ground or in air; needs its own
      cooldown and should be checked against the slope-force logic so it
      doesn't fight the slope-stick force on activation

## Phase 2 — Combat

- [ ] Weapon base class / interface (fire, reload, ammo, recoil) that a future gun
      slots into — decide if the grapple gun and a real weapon coexist (separate
      slots) or share a slot with a swap key
- [ ] Hitscan or projectile weapon prototype
- [ ] Enemy base: simple stationary or patrolling target with health and death
      feedback, to validate weapon feel before investing in AI
- [ ] Basic enemy AI (detection, chase, attack) once a target dummy proves the
      weapon loop is fun
- [ ] Player health/damage and a death/respawn flow
- [ ] Combat feedback: hit markers, damage numbers or flashes, simple SFX

## Phase 3 — Level design & content

- [ ] Replace the gray-box floor/slopes with a purpose-built movement-tech test
      chamber (ramps, gaps, wall-run corridors, grapple points) as the first real
      "level"
- [ ] A second level introducing combat encounters that require traversal skills
      (e.g., an arena only reachable via grapple/wall-run)
- [ ] Environment art pass: replace default ProBuilder gray/orange materials with
      real textures or a stylized shader
- [ ] Lighting pass beyond the single Directional Light — bake or realtime GI,
      skybox

## Phase 4 — UI / UX

- [~] Crosshair (done) and FPS counter (done, display currently disabled in
      `FPSCounter.cs` — text assignment is commented out)
- [ ] HUD: health, ammo, grapple cooldown/rope-length indicator
- [ ] Pause menu, settings (sensitivity, FOV, key rebinding)
- [ ] Main menu / level select if more than one level exists
- [ ] Audio: footsteps, jump/land, grapple fire/attach/release, ambient level audio

## Phase 5 — Polish & release prep

- [ ] Full settings menu (graphics quality, audio mixer volumes)
- [ ] Controller/gamepad support if targeted
- [ ] Performance pass: profiling under `manage_profiler`-style tooling, draw call
      / physics tick budget check once real levels exist
- [ ] Build pipeline: confirm `manage_build`-style pipeline target platform(s),
      icon/branding, versioning
- [ ] Playtesting pass focused specifically on movement feel (grapple + wall-run +
      slide interplay) before locking level geometry

---

## Open design questions (revisit before Phase 2)

- Does the grapple gun replace a weapon slot, or run alongside one (e.g., bound to
  a separate key permanently, like Titanfall's grapple)?
- Is progression/unlocks in scope at all, or is this a single-loadout tech demo?
- Single-player only, or is multiplayer ever a goal? (Affects whether the physics
  work — SpringJoint, Rigidbody-based movement — needs to be networking-friendly
  later.)

## Related notes

Deeper technical context on what's been built and why lives in this session's
persistent memory (grapple gun implementation details, the movement physics bug
root causes/fixes) — ask in a future session to recall it if picking this back up.
