# FrostNova cold air — 2026-11-15

Emission follows ReforgedReformed/Weapons/Legendary/SHPC/Effects/BPrePlantera/Essence/EssenceofSnow_N2.cs: 50 ticks, 280→560 diameter, 10 MediumMist per update, 18 radial mists every six updates, eight HeavySmoke per update. Density is bounded when overlapping emitters would exhaust the shared particle budget.

MediumMist.png and HeavySmoke.png are unchanged local Calamity textures from Particles/MediumMist.png and Particles/HeavySmoke.png. FrostNovaMediumMist and FrostNovaHeavySmoke port their original particle update/draw rules into this mod's existing particle manager, with update-order compensation. No Calamity runtime dependency is added.
