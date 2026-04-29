# Cyber-Security Network Defense Simulation

Drop into Play Mode from any scene. `CyberDefenseBootstrapper` creates the simulation manager and camera automatically if the scene does not already contain one.

## Controls

- `Q`: Toggle Quad-Tree gizmo visualization.

## What Is Implemented

- Self-implemented dynamic Quad-Tree in `Spatial/QuadTree.cs`
  - `Insert`
  - `Remove`
  - `Update`
  - Radius and rectangle `Query`
  - Dynamic root growth for moving entities
  - `DebugDraw` using `Gizmos.DrawWireCube`
- Data-Sentinels gather Data-Packets, return them to the Central Hub, flee or hide behind Firewalls, recharge, and cooperatively repair Corrupted Nodes.
- Malware-Bugs query the Quad-Tree for Sentinels or the Hub, rally into packs, hunt, attack, retreat from drones, and corrupt nodes.
- Repair-Drones use flocking-style separation, alignment, cohesion, and malware-pressure steering to patrol high-threat areas.
- System Overload adapts behavior when Malware or Corrupted Nodes become too numerous.
- `CyberEntityConfig` ScriptableObjects can be created from `Assets > Create > Cyber Defense > Entity Config` and assigned to the simulation for tuning.

## Visual Polish Layer

- Primitive square renderers are hidden at runtime and replaced with procedural sprites, glows, particles, and state bars.
- Nano-Scrubbers use circular dust trails and turn overloaded orange while carrying data.
- Repair-Bots use a glowing gear/drone body and bright sparks while repairing. Three nearby bots multiply the repair spark size and rate.
- Corrosion-Sparks use jagged pulsating sprites and intensify as they close on the hub.
- Corrupted Nodes use exposed socket art with arcing particles that calm down as integrity is restored.
- Interaction beams flash during data deposit and repair collaboration.
- Destroyed actors emit a micro-circuit particle burst.
- The Quad-Tree overlay uses fading semi-transparent diagnostic lines and highlights queried regions in blue.
