# Vector VFX materials

`MuzzleFlash`, `PrecisionRound`, `ImpactFlare`, `SparkStreak`, `FlightWisp` and `PressureFlash` are unchanged PNG copies from the user's local `HelldiversNeverDie/Assets/Vfx`. The latest round uses the complete R-2 Amendment precision material, its 16-world-pixel remnant spacing and kinetic contact composition.

`Feather.png` is an unchanged copy of `CalamityMod/Particles/FeatherParticle.png`, credited to the Calamity Mod team and original contributors. Frame 0 is used as a filled, tinted feather for overlapping wing primaries/coverts and drifting feather particles. No wing outlines are drawn with lines.

Existing local `ShpcEnergy/BloomCircle`, `ShpcEnergy/BloomRing` and `HelldiverExplosions/SmokePuffs` are reused. References are local assets; no runtime dependency on either donor mod is added. Particle appearance and movement are separate from projectile damage, cadence, magazines and skill calculations.

The 2026-10-26 revision removes the Xyk motion equations and lifecycle. Current wings are two shoulder-anchored feather fans: a shared stroke, smoothed body lean and spread, whole-wing unfold/fold, and local flexible feather geometry. There is no independent wing-cell chase/orbit, staggered arrival, scale overshoot, ground-time disappearance, flight-reserve breakup or special Xyk input-state response. Existing feather/halo/light textures are retained, and no actual flight ability is granted. Earlier rejected Xyk implementations remain only in historical validation snapshots.

Divergent skill volleys still reference `CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCRight_HoulOut.cs`: zero-angle primary, 2.5/-4-pixel secondary origins and 0.45..2.2-degree secondary offsets. Three-shot volleys place the secondaries on opposite sides. The new wing motion does not alter gun damage, cadence or ammunition.
