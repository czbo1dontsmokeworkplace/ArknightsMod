# SHPC effect migration

Requested local references:

- `CalamityLegendsComeBack/Weapons/SHPC/Effects/CPreMoodLord/CoreOfCalamity/CoreOfCalamitySplitOrb.cs`: drifting orb, delayed target lock, high-speed jet, layered glow and streak.
- `CalamityLegendsComeBack/Weapons/SHPC/EXSkill/NL_SHPC_EXWeapon.cs`, `SpawnBurstEffect`: progressively denser absorption storm, light lanes, sparks, squashed marks and pulsing core.
- `CalamityLegendsComeBack/Weapons/SHPC/Effects/BPrePlantera/NewSHPS.cs`, preset 4: corkscrew approach, orbit and collapse; colored outer trail and shorter bright inner trail. The disabled `SHPC_ChargeSpiralInv` spawn is not used.
- `CalamityMod/Effects/ImpFlameTrail.fx`: adapted into `Assets/Effects/ShpcEnergy/SoulTrail.fx` with ordinary two-component ribbon UVs and a local streak sampler.

The five PNG materials are copied from the local CalamityMod source: `ExtraTextures/Trails/ScarletDevilStreak.png`, `ExtraTextures/Trails/SylvestaffStreak.png`, `Particles/BloomCircle.png`, `Particles/BloomRing.png`, and `Particles/ProvidenceMarkParticle.png`. Calamity assets/effect credit: Calamity Mod team / their original contributors. SHPC composition credit: the user's CalamityLegendsComeBack implementation.

All materials are loaded locally by ArknightsMod. No runtime dependency on either reference mod is added. Existing Phalanx sand, red-sand and glass signature effects are retained.
