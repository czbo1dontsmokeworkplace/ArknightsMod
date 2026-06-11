using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Matoimaru
{
	public class MataimaruGlaive : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [60, 72, 87];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 54;
			Item.height = 56;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.knockBack = 6f;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive) {
				// S1 生命回复·B：触发时恢复50%最大生命（在激活一次）
				if (mp.Skill == 0 && mp.SkillTimer == 1)
					player.statLife = System.Math.Min(player.statLife + (int)(player.statLifeMax * 0.5f), player.statLifeMax);
				// S2 恶鬼之力：防御归零，攻击力+150%
				if (mp.Skill == 1)
					Item.damage = (int)(EliteDamage[EliteStage] * 2.5f);
				else
					Item.damage = EliteDamage[EliteStage];
			} else {
				Item.damage = EliteDamage[EliteStage];
			}
			return base.CanUseItem(player);
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (mp.SkillActive && mp.Skill == 1)
				modifiers.Defense.Base -= target.defense; // 防御降至0
		}
	}
}
