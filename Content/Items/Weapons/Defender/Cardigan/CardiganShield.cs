using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender.Cardigan
{
	public class CardiganShield : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [24, 30];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 48;
			Item.height = 46;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.knockBack = 6f;
			Item.value = Item.sellPrice(silver: 15);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override void HoldItem(Player player) {
			base.HoldItem(player);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 生命回复·α型：立即恢复最大生命40%（在技能激活时触发一次）
			if (mp.SkillActive && mp.SkillTimer == 1)
				player.statLife = System.Math.Min(player.statLife + (int)(player.statLifeMax * 0.4f), player.statLifeMax);
		}
	}
}
