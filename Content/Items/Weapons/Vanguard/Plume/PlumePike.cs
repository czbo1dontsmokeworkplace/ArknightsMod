using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Plume
{
	public class PlumePike : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [40, 52];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 50;
			Item.height = 52;
			Item.useTime = 12;
			Item.useAnimation = 12;
			Item.knockBack = 3f;
			Item.value = Item.sellPrice(silver: 25);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 迅捷打击·α：攻击力+25%，攻击速度+25
			if (mp.SkillActive) {
				Item.damage = (int)(EliteDamage[EliteStage] * 1.25f);
				Item.useTime = 9;
				Item.useAnimation = 9;
			} else {
				Item.damage = EliteDamage[EliteStage];
				Item.useTime = 12;
				Item.useAnimation = 12;
			}
			return base.CanUseItem(player);
		}
	}
}
