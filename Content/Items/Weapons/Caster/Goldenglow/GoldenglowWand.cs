using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Caster.Goldenglow
{
	public class GoldenglowWand : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [30, 36, 41];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 54;
			Item.height = 54;
			Item.useTime = 23;
			Item.useAnimation = 23;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.MagicMissile;
			Item.mana = 8;
			Item.crit = 4;
			Item.shootSpeed = 10f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;
			Item.staff[Item.type] = true;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			Item.damage = mp.SkillActive
				? (int)(EliteDamage[EliteStage] * (mp.Skill == 2 ? 1.8f : mp.Skill == 1 ? 1.6f : 1.4f))
				: EliteDamage[EliteStage];
			return base.CanUseItem(player);
		}
	}
}
