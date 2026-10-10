using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.BossSummon;
using ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;
using ArknightsMod.Content.NPCs.Enemy.OF.Pmp;
using ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;
using ArknightsMod.Content.NPCs.Enemy.Seamonster;
using Terraria.ModLoader;

namespace ArknightsMod.Systems
{
	public class BossChecklistCompatibilitySystem : ModSystem
	{
		public override void PostSetupContent()
		{
			if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist))
				return;

			TryLogBoss(bossChecklist,
				internalName: "MaterialistAntagonizer",
				progression: 12.5f,
				downed: () => DownedBossSystem.DownedMaterialistAntagonizer,
				npcIDs: new List<int> { ModContent.NPCType<Content.NPCs.Enemy.MaterialistAntagonizer.MaterialistAntagonizer>() },
				spawnItems: new List<int> { ModContent.ItemType<Content.NPCs.Enemy.MaterialistAntagonizer.MaterialistUplink>() }
			);

			TryLogBoss(bossChecklist,
				internalName: "FaustAndMephisto",
				progression: 6.9f,
				downed: () => DownedBossSystem.DownedFaustAndMephisto,
				npcIDs: new List<int> { ModContent.NPCType<Content.NPCs.Enemy.FaustAndMephisto.Mephisto>(), ModContent.NPCType<Content.NPCs.Enemy.FaustAndMephisto.Faust>() },
				spawnItems: new List<int>()
			);

			TryLogBoss(bossChecklist,
				internalName: "Pompeii",
				progression: 3.0f,
				downed: () => DownedBossSystem.DownedPompeii,
				npcIDs: new List<int> { ModContent.NPCType<Pompeii>() },
				spawnItems: new List<int> { ModContent.ItemType<PompeiiSummon>() }
			);

			TryLogBoss(bossChecklist,
				internalName: "TheFirstToTalk",
				progression: 3.5f,
				downed: () => DownedBossSystem.DownedTheFirstToTalk,
				npcIDs: new List<int> { ModContent.NPCType<TheFirstToTalk>() },
				spawnItems: new List<int> { ModContent.ItemType<TheFirstToTalkSummon>() }
			);

			TryLogBoss(bossChecklist,
				internalName: "FrostNova",
				progression: 10.0f,
				downed: () => DownedBossSystem.DownedFrostNova,
				npcIDs: new List<int> { ModContent.NPCType<FrostNova>() },
				spawnItems: new List<int> { ModContent.ItemType<SpicyCandy>() }
			);

			TryLogBoss(bossChecklist,
				internalName: "AACT",
				progression: 10.5f,
				downed: () => DownedBossSystem.DownedAACT,
				npcIDs: new List<int> { ModContent.NPCType<AACT>() },
				spawnItems: new List<int> { ModContent.ItemType<AACTSummon>() }
			);

			TryLogBoss(bossChecklist,
				internalName: "Evolution",
				progression: 11.5f,
				downed: () => DownedBossSystem.DownedEvolution,
				npcIDs: new List<int> { ModContent.NPCType<Content.NPCs.Enemy.Evolution.Evolution>() },
				spawnItems: new List<int> { ModContent.ItemType<EvolutionSummon>() }
			);
		}

		private void TryLogBoss(Mod bossChecklist, string internalName, float progression, Func<bool> downed, List<int> npcIDs, List<int> spawnItems)
		{
			var extra = new Dictionary<string, object>
			{
				["spawnItems"] = spawnItems,
			};

			bossChecklist.Call("LogBoss", Mod, internalName, progression, downed, npcIDs, extra);
		}
	}
}
