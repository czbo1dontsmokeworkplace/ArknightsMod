using ArknightsMod.Content.Items.Weapons.Specialist.Red;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories
{
	/// <summary>
	/// 放在背包并标记为收藏时，将当前手持明日方舟武器的当前技能充能时间压缩至约一秒。
	/// </summary>
	public sealed class RhodesIslandEmergencyChargeManual : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalStorm;

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 0;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 30;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.sellPrice(gold: 5);
		}

		public override void UpdateInventory(Player player) {
			if (Item.favorited)
				player.GetModPlayer<RhodesIslandEmergencyChargePlayer>().Enabled = true;
		}


		public override bool CanResearch() => false;
	}

	public sealed class RhodesIslandEmergencyChargePlayer : ModPlayer
	{
		internal bool Enabled;

		public override void ResetEffects() {
			Enabled = false;
		}
	}

	/// <summary>
	/// 在全部 ModPlayer 更新完成后再推进充能，避免专属武器的显示镜像覆盖本物品效果。
	/// </summary>
	public sealed class RhodesIslandEmergencyChargeSystem : ModSystem
	{
		public override void PostUpdatePlayers() {
			if (Main.gameMenu)
				return;

			foreach (Player player in Main.ActivePlayers) {
				if (player.whoAmI != Main.myPlayer || player.dead
					|| !player.GetModPlayer<RhodesIslandEmergencyChargePlayer>().Enabled)
					continue;

				player.GetModPlayer<WeaponPlayer>().ApplyEmergencyOneSecondCharge();
				player.GetModPlayer<RedDaggerPlayer>().ApplyEmergencyOneSecondCharge();
			}
		}
	}
}
