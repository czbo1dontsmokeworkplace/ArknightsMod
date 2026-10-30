# Subsuming Vortex flight trail

Credit: **Azafure, LLC**, Calamity Mod.
Source: https://github.com/CalamityTeam/CalamityModPublic

- `Effects/SideStreakTrail.fx` -> `Assets/Effects/MedicSideStreak.fx`
- `Effects/SideStreakTrail.xnb` -> `Assets/Effects/MedicSideStreak.xnb`

Both shader files are included unchanged. Reference: the `SideStreakTrail` pass in `Projectiles/Magic/ExoVortex2.cs`, using Terraria's `Images/Misc/Perlin` noise. The wrapper draws a ribbon along the actual medical projectile position history with its operator body colour. No vortex body shader, BlobbyNoise swirl, targeting, damage or explosion is used.
