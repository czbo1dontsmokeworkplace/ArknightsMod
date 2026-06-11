using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Melantha
{
	public class MelanthasSword : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [67, 83];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 46;
			Item.height = 48;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.knockBack = 5f;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			Item.damage = mp.SkillActive ? (int)(EliteDamage[EliteStage] * 1.5f) : EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}
	}
}
