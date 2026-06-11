using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Gitano
{
	public class GitanoUmbrella : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [34, 57];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 48;
			Item.height = 42;
			Item.useTime = 33;
			Item.useAnimation = 33;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.MagicMissile;
			Item.mana = 7;
			Item.crit = 4;
			Item.shootSpeed = 10f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.staff[Item.type] = true;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 迅捷打击·α：攻击力+25%，攻击速度+25
			if (mp.SkillActive) {
				Item.damage = (int)(EliteDamage[EliteStage] * 1.25f);
				Item.useTime = 25;
				Item.useAnimation = 25;
			} else {
				Item.damage = EliteDamage[EliteStage];
				Item.useTime = 33;
				Item.useAnimation = 33;
			}
			return base.CanUseItem(player);
		}
	}
}
