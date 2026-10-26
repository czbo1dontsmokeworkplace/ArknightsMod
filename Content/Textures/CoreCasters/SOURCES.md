# PurifiedGel material sources

The original PurifiedGelBall.png is copied byte-for-byte from the user-specified local CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PurifiedGel_Ball.png (34x288, 8 frames). The runtime palette cache remaps original color regions, preserving source animation and alpha. It does not replace the source sprite with a generic sphere.

Added local reference particle textures: GelBloomCircle.png and GelBloomRing.png from CalamityMod/Particles/BloomCircle.png and BloomRing.png; GelPoint.png from CalamityMod/Particles/PointParticle.png; GelCircle.png from CalamityMod/ExtraTextures/BasicCircle.png. Reference code: PurifiedGel_Ball.PreDraw / SpawnDeathVisual, CalamityMod.Particles.PointParticle.CustomDraw, CalamityMod.Dusts.SquashDust.PreDraw. User-authorized local prototype resource reuse. No source mod runtime dependency.

Existing gray materials remain under their original filenames. RGB grayscale, alpha masks and opaque black backgrounds are explicitly handled by CoreCasterPalette.Material; the source textures are not modified to make the preview look different from production.

## Fast energy revision · 2026-10-26

The slow PurifiedGel body is retained as a source asset but is no longer loaded by CoreCasterArt or used as the spell/particle body. The live seven-staff family now uses the following byte-for-byte local Calamity masks without a donor-mod dependency:

- EnergyLance.png: CalamityMod/Particles/GlowSpark.png (WingmanShot / IonBlast pointed colored shell and pale core).
- EnergyStreak.png: CalamityMod/Particles/BloomLineSoftEdge.png (detached directional luminous particles).
- EnergyShock.png: CalamityMod/Particles/HighResHollowCircleHardEdgeAlt.png (short, flattened contact/muzzle shock; source luminous bounding rectangle 36,952,2374,781).
- EnergyFlow.png: CalamityMod/ExtraTextures/Trails/SylvestaffStreak.png (sampled inside the continuously tapered real flight path).

These grayscale light masks are tinted throughout, using alpha-zero light blending for the shell/core/particles; dark source backgrounds add no black rectangles. The real trail shader uses a controlled colored body with a smaller bright spine. No new raster art was generated. Existing supplied OperatorBolts and PurifiedGel assets were preserved on disk.
