using System.Collections.Generic;
using ArknightsMod.Content.Buffs.Supporter.Pramanix;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Pramanix
{
	public static class PramanixColdAttachment
	{
		private static readonly Dictionary<int, int> Stacks = new();
		private static readonly Dictionary<int, int> Skill2HitCounts = new();

		public const int ChilledAttachTicks = 6;
		public const int DomainFreezeThreshold = 8;
		public const int Skill2FreezeInterval = 3;

		public static void ApplyDomain(NPC npc, Player player) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!CanAffect(npc))
				return;

			AddStack(npc, 1);
			ApplyChilled(npc);
			if (GetStacks(npc) >= DomainFreezeThreshold)
				TryFreeze(npc);
		}

		public static void ApplySkill2Hit(NPC npc, Player player) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!CanAffect(npc))
				return;

			AddStack(npc, 4);
			ApplyChilled(npc);

			int id = npc.whoAmI;
			Skill2HitCounts.TryGetValue(id, out int hits);
			hits++;
			Skill2HitCounts[id] = hits;
			if (hits % Skill2FreezeInterval == 0 || GetStacks(npc) >= DomainFreezeThreshold)
				TryFreeze(npc);
		}

		public static void ApplyBurst(NPC npc, int chilledSeconds = 3) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!CanAffect(npc))
				return;

			AddStack(npc, 3);
			ApplyChilled(npc, chilledSeconds * 60);
			TryFreeze(npc);
		}

		public static void Clear(NPC npc) {
			int id = npc.whoAmI;
			Stacks.Remove(id);
			Skill2HitCounts.Remove(id);
		}

		private static bool CanAffect(NPC npc) =>
			npc.active && !npc.friendly && !npc.dontTakeDamage && npc.life > 0;

		private static int GetStacks(NPC npc) {
			Stacks.TryGetValue(npc.whoAmI, out int value);
			return value;
		}

		private static void AddStack(NPC npc, int amount) {
			int id = npc.whoAmI;
			Stacks.TryGetValue(id, out int value);
			Stacks[id] = value + amount;
		}

		private static void ApplyChilled(NPC npc, int ticks = ChilledAttachTicks) {
			npc.buffImmune[BuffID.Chilled] = false;
			npc.AddBuff(BuffID.Chilled, ticks);
			npc.netUpdate = true;
		}

		public static void TryFreeze(NPC npc, int ticks = 120) {
			if (!CanAffect(npc) || npc.boss)
				return;

			int freezeType = ModContent.BuffType<PramanixFreezeDebuff>();
			if (!npc.HasBuff(freezeType)) {
				npc.AddBuff(freezeType, ticks);
				if (Main.netMode != NetmodeID.Server)
					SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/Frozen") with { Volume = 0.75f, Pitch = Main.rand.NextFloat(-0.06f, 0.06f) }, npc.Center);
			}
			else {
				npc.AddBuff(freezeType, ticks);
			}

			Stacks[npc.whoAmI] = 0;
			npc.netUpdate = true;
		}

		public static void CleanupInactive() {
			var remove = new List<int>();
			foreach (var (id, _) in Stacks) {
				if (id < 0 || id >= Main.maxNPCs || !Main.npc[id].active)
					remove.Add(id);
			}
			foreach (int id in remove) {
				Stacks.Remove(id);
				Skill2HitCounts.Remove(id);
			}
		}
	}
}
