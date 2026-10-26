# PurifiedGel material sources

The original PurifiedGelBall.png is copied byte-for-byte from the user-specified local CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PurifiedGel_Ball.png (34x288, 8 frames). The runtime palette cache remaps original color regions, preserving source animation and alpha. It does not replace the source sprite with a generic sphere.

Added local reference particle textures: GelBloomCircle.png and GelBloomRing.png from CalamityMod/Particles/BloomCircle.png and BloomRing.png; GelPoint.png from CalamityMod/Particles/PointParticle.png; GelCircle.png from CalamityMod/ExtraTextures/BasicCircle.png. Reference code: PurifiedGel_Ball.PreDraw / SpawnDeathVisual, CalamityMod.Particles.PointParticle.CustomDraw, CalamityMod.Dusts.SquashDust.PreDraw. User-authorized local prototype resource reuse. No source mod runtime dependency.

Existing gray materials remain under their original filenames. RGB grayscale, alpha masks and opaque black backgrounds are explicitly handled by CoreCasterPalette.Material; the source textures are not modified to make the preview look different from production.
