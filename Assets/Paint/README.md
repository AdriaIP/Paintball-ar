# Paint Trace System - Setup Guide

## Overview
This system allows wet paintballs to leave realistic paint traces on surfaces, including AR scene meshes. After 5 hits, the ball becomes white (dry) until painted again.

## Quick Setup

### 1. Create PaintSplatManager GameObject
1. Create an empty GameObject in your scene named "PaintSplatManager"
2. Add the `PaintSplatManager` component
3. Add the `RuntimeSplatTextureGenerator` component (auto-generates splat textures)

### 2. Configure the Manager
On `PaintSplatManager`:
- **Base Splat Size**: 0.1 (10cm splats)
- **Size Variation**: 0.03
- **Surface Offset**: 0.001 (prevents z-fighting)
- **Max Splats**: 200 (increase for more splats, costs memory)
- **Conform To Edges**: true (adapts splats to surface edges)

### 3. Ball Setup
Your paintballs need:
1. `SphereCollision` component (already exists, renamed from `Collision`)
2. `WetBall` component (auto-added by SphereCollision if missing)
3. A Collider component
4. A Rigidbody component

On `SphereCollision`:
- Set `Blue` and `Red` colors
- **Ignored Tags**: Tags that won't receive splats (BluePaint, RedPaint, etc.)
- **Min Impact Velocity**: 0.5 (minimum speed to leave a trace)
- **Multi Splat On Hard Impact**: true (more splats for harder hits)

### 4. Paint Sources
Make sure your paint containers/buckets have:
- Tag: "BluePaint" or "RedPaint"
- A Collider component

## Why Quad-Based Splats Instead of URP Decals?

URP Decal Projectors have limitations with AR scene meshes because:
1. AR meshes are generated at runtime without proper UV mapping
2. Decals require specific layer/rendering setup that AR meshes may not have
3. Scene mesh shaders may not support decal receiving

**Our quad-based approach:**
- Projects splats as billboards aligned to surface normals
- Works on ANY surface with a collider
- Adapts to edges and corners automatically
- Uses depth offset to prevent z-fighting
- Lower GPU overhead than decal projection

## Custom Splat Textures

### Using Pre-made Textures
1. Create white splat shapes with alpha transparency (PNG)
2. Assign to `PaintSplatManager.splatTextures` array

### Runtime Generation
The `RuntimeSplatTextureGenerator` creates 4 types:
- Circle splats (soft circular stains)
- Irregular splats (organic blob shapes)
- Drip splats (gravity-affected drips)
- Splatter splats (impact splatter patterns)

## Custom Shader

The `PaintSplatUnlit` shader in `Assets/Paint/Shaders/` provides:
- Transparent rendering with soft edges
- Depth offset to prevent z-fighting
- Double-sided rendering
- GPU instancing support
- AR depth compatibility

To create a material:
1. Create Material → Shader: `Custom/PaintSplatUnlit`
2. Assign a splat texture to `_BaseMap`
3. Set base color to white (color comes from code)

## Splat Prefab (Optional)

For custom splat visuals:
1. Create Quad: 3D Object → Quad
2. Remove the MeshCollider
3. Add `PaintSplat` component
4. Apply the custom material
5. Save as prefab
6. Assign to `PaintSplatManager.splatPrefab`

## Troubleshooting

### Splats not appearing on AR mesh
- Check that the AR mesh has a MeshCollider (OVRSceneManager usually adds this)
- Increase `surfaceOffset` if splats clip through
- Make sure `maxSplats` hasn't been reached

### Z-fighting (flickering splats)
- Increase `surfaceOffset` on PaintSplatManager
- Increase `_DepthOffset` on the shader material

### Splats appearing mid-air
- Check that collisions are being detected properly
- Verify the ball has both Rigidbody and Collider
- Make sure surfaces have Colliders

### Performance issues
- Reduce `maxSplats`
- Reduce `textureSize` on RuntimeSplatTextureGenerator
- Disable `conformToEdges` for simpler splats
- Set `splatLifetime` > 0 so splats fade and recycle

## Advanced: Adding More Paint Colors

In `SphereCollision.OnCollisionEnter()`, add more color checks:
```csharp
else if (tag == "GreenPaint")
{
    PaintBall(green); // Add green Color field
    return;
}
```

## Code Architecture

```
SphereCollision (on ball)
    ├── Detects paint source collision → calls WetBall.Paint()
    └── Detects surface collision → calls WetBall.UsePaint() + PaintSplatManager.SpawnSplat()

WetBall (on ball)
    ├── Tracks wet/dry state
    ├── Tracks remaining hits (5 by default)
    └── Changes ball color

PaintSplatManager (singleton in scene)
    ├── Object pooling for splats
    ├── Surface conforming logic
    └── Spawns PaintSplat instances

PaintSplat (on each splat quad)
    ├── Sets color/texture
    └── Handles lifetime/fading
```
