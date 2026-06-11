using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Vanilla
{
	public class VanillaAxe : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [31, 43];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 46;
			Item.height = 48;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 5f;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			Item.damage = mp.SkillActive ? (int)(EliteDamage[EliteStage] * 1.35f) : EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}
	}
}
