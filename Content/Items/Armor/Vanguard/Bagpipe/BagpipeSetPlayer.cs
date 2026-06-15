using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	internal class BagpipeSetPlayer : ArknightsArmorPlayer
	{
		public bool BagpipeHelmetActive;
		public bool BagpipeSetActive;

		private bool spawnSpGranted;

		public override void ResetEffects() {
			BagpipeHelmetActive = false;
			BagpipeSetActive = false;
		}

		public override void PostUpdateEquips() {
			BagpipeHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<BagpipeHelmet>());
			BagpipeSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<BagpipeHelmet>(),
				ModContent.ItemType<BagpipeChestplate>(),
				ModContent.ItemType<BagpipeGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, BagpipeSetActive, "Mods.ArknightsMod.ArmorSets.Bagpipe.SetBonus");
		}

		public override void PostUpdate() {
			if (BagpipeHelmetActive && !Player.dead && !spawnSpGranted) {
				GrantVanguardSp();
				spawnSpGranted = true;
			}

			if (Player.dead)
				spawnSpGranted = false;
		}

		public override void OnRespawn() {
			if (BagpipeHelmetActive)
				GrantVanguardSp();
		}

		private static void GrantVanguardSp() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead)
					continue;

				if (OperatorVanguardSetHelper.WearsFullVanguardSet(player))
					OperatorSPHelper.TryGainSP(player, 8);
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (BagpipeSetActive && item.DamageType.CountsAsClass(DamageClass.Melee) && Main.rand.NextFloat() < 0.2f)
				modifiers.SourceDamage *= 1.5f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (BagpipeSetActive && proj.DamageType.CountsAsClass(DamageClass.Melee) && Main.rand.NextFloat() < 0.2f)
				modifiers.SourceDamage *= 1.5f;
		}
	}
}
