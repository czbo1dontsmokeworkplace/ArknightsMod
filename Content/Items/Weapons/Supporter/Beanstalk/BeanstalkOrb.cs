using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Beanstalk
{
	public class BeanstalkOrb : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [57, 64, 74];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 28;
			Item.height = 28;
			Item.useTime = 37;
			Item.useAnimation = 37;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ProjectileID.MagicMissile;
			Item.mana = 11;
			Item.crit = 4;
			Item.shootSpeed = 9f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.staff[Item.type] = true;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				// S1 战术咏唱·B：攻击速度+75
				// S2 命运：攻击力+100%，全屏范围
				Item.damage = mp.Skill == 1
					? (int)(EliteDamage[EliteStage] * 2.0f)
					: EliteDamage[EliteStage];
				Item.useTime = mp.Skill == 0
					? System.Math.Max(15, Item.useTime - 10)
					: Item.useTime;
			} else {
				Item.damage = EliteDamage[EliteStage];
				Item.useTime = 37;
				Item.useAnimation = 37;
			}
			return base.CanUseItem(player);
		}
	}
}
