using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class LapplandSilenceDebuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
		}

		// Repeated hits cannot extend an already running silence.
		public override bool ReApply(NPC npc, int time, int buffIndex) => true;
	}

	public sealed class LapplandSilenceNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;
		private NPC spawnParent;
		private const int SilenceDuration = 5 * 60;
		private const int RecoveryDuration = 10 * 60;
		// One cycle belongs to this NPC: 5 seconds of silence, then 10 seconds of immunity.
		private int silenceCycleRemaining;
		internal bool CanReceiveSilence => silenceCycleRemaining == 0;
		private bool IsRecovering => silenceCycleRemaining > 0 && silenceCycleRemaining <= RecoveryDuration;

		public override void SetDefaults(NPC npc) {
			// Silence must also apply to bosses with broad debuff immunity.
			npc.buffImmune[ModContent.BuffType<LapplandSilenceDebuff>()] = false;
			spawnParent = null;
			silenceCycleRemaining = 0;
		}

		public override void ResetEffects(NPC npc) {
			// ResetEffects also runs when another effect stops the NPC's AI.
			if (silenceCycleRemaining > 0) {
				silenceCycleRemaining--;
				if (silenceCycleRemaining == 0 && Main.netMode != NetmodeID.MultiplayerClient)
					npc.netUpdate = true;
			}
			npc.buffImmune[ModContent.BuffType<LapplandSilenceDebuff>()] = IsRecovering;
		}

		internal void TryApplySilence(NPC npc) {
			if (Main.netMode == NetmodeID.MultiplayerClient || silenceCycleRemaining > 0
				|| !npc.active || npc.friendly || npc.lifeMax <= 5 || npc.dontTakeDamage)
				return;
			int buffType = ModContent.BuffType<LapplandSilenceDebuff>();
			npc.buffImmune[buffType] = false;
			npc.AddBuff(buffType, SilenceDuration);
			if (!npc.HasBuff(buffType)) return;
			silenceCycleRemaining = SilenceDuration + RecoveryDuration;
			npc.netUpdate = true;
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
			=> writer.Write((ushort)silenceCycleRemaining);

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader) {
			silenceCycleRemaining = reader.ReadUInt16();
			npc.buffImmune[ModContent.BuffType<LapplandSilenceDebuff>()] = IsRecovering;
		}

		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (source is EntitySource_Parent { Entity: NPC parent })
				spawnParent = parent;
		}

		internal static bool IsSilenced(NPC npc) {
			// Include worm segments and NPC children spawned by the boss.
			for (int depth = 0; npc != null && npc.active && depth < 16; depth++) {
				if (npc.HasBuff(ModContent.BuffType<LapplandSilenceDebuff>()))
					return true;
				if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && npc.realLife != npc.whoAmI)
					npc = Main.npc[npc.realLife];
				else
					npc = npc.GetGlobalNPC<LapplandSilenceNPC>().spawnParent;
			}
			return false;
		}
	}

	public sealed class LapplandSilenceProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;
		private NPC sourceNpc;

		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			sourceNpc = source switch {
				EntitySource_Parent { Entity: NPC npc } => npc,
				EntitySource_Parent { Entity: Projectile parent } =>
					parent.GetGlobalProjectile<LapplandSilenceProjectile>().sourceNpc,
				_ => null
			};

			if (projectile.hostile && LapplandSilenceNPC.IsSilenced(sourceNpc))
				// Avoid Kill(): death effects can create more projectiles or explosions.
				projectile.active = false;
		}
	}
}
