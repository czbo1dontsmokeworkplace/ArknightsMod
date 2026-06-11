using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Hongdou
{
	public class HongdouLance : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [44, 52, 60];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 56;
			Item.height = 58;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.knockBack = 5f;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				Item.damage = mp.Skill == 0
					? (int)(EliteDamage[EliteStage] * 1.8f)   // S1 攻击力+80%
					: (int)(EliteDamage[EliteStage] * 3.0f);  // S2 槌音 攻击力+200%，攻击间隔增大
				if (mp.Skill == 1) {
					Item.useTime = 33;
					Item.useAnimation = 33;
				} else {
					Item.useTime = 22;
					Item.useAnimation = 22;
				}
			} else {
				Item.damage = EliteDamage[EliteStage];
				Item.useTime = 22;
				Item.useAnimation = 22;
			}
			return base.CanUseItem(player);
		}
	}
}
