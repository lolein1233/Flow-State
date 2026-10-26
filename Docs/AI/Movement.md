# Movement and camera tuning

Updated 2026-10-25 for `Assets/02_Escenas/Gameplay.unity`.

`FPSController` owns movement, player rotation and the camera. Root motion stays disabled.

- Walking is 1.8 m/s; sprinting is 5.5 m/s. Ground acceleration is 14 m/s² and braking is 20 m/s². `walkSpeed` is authoritative; the legacy `speed` value no longer overrides it. Traversal is faster than the initial natural-cadence pass (1.25/4 m/s), while the accepted animation cadence remains capped at 1.15x.
- Ground travel follows the body's smoothed heading. Sharp reversals brake before accelerating; airborne input keeps the existing momentum and air-control rules. Graffiti and combat retain their separate movement paths.
- Movement and camera use the same filtered orbit yaw. Mouse deltas do not multiply by delta time; the existing sensitivity keeps its 60 Hz calibration.
- Horizontal camera follow carries player displacement immediately. Orbit angles, vertical steps, look-ahead, collision recovery and mode changes have their own smoothing. Normal follow does not add a second world-position or rotation filter, which previously amplified parent-transform movement.
- Obstructions shorten the camera boom immediately; moving away restores distance smoothly. Collision queries reuse the controller's non-allocating buffer.
- Animator `Speed` comes from actual capsule displacement, so pushing against a wall does not play full-speed running. The `Walk` state blends walking and running continuously. Speed and cadence changes are damped over 0.08/0.12 seconds. `LocomotionRate` uses approximate planted-foot speeds measured from the scaled character (1.1 m/s for Walking and 3.5 m/s for Running) with a strict 1.15x playback limit.

Regression coverage lives in `Assets/Editor/MovementFeelTests.cs`. Existing parkour and graffiti tests cover the neighboring modes. Gameplay remains the scene for subjective movement-feel checks; walk/run blend thresholds in the shared MAIKOL controller match its 1.8/5.5 tuning.
