using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	internal class LavaSetPlayer : ArknightsArmorPlayer
	{
		public bool LavaHelmetActive;
		public bool LavaSetActive;
		private bool spawnSpGranted;

		public override void ResetEffects() {
			LavaHelmetActive = false;
			LavaSetActive = false;
		}

		public override void PostUpdateEquips() {
			LavaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<LavaHead>());
			LavaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<LavaHead>(),
				ModContent.ItemType<LavaBody>(),
				ModContent.ItemType<LavaLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, LavaSetActive, "Mods.ArknightsMod.ArmorSets.Lava.SetBonus");
		}

		public override void PostUpdate() {
			if (LavaSetActive && Player.GetModPlayer<WeaponPlayer>().SkillActive)
				Player.AddBuff(BuffID.Inferno, 2);

			if (!Player.dead && LavaHelmetActive && !spawnSpGranted) {
				OperatorSPHelper.TryGainSP(Player, 15);
				spawnSpGranted = true;
			}

			if (Player.dead)
				spawnSpGranted = false;
		}

		public override void OnRespawn() {
			if (LavaHelmetActive)
				OperatorSPHelper.TryGainSP(Player, 15);
		}
	}
}
