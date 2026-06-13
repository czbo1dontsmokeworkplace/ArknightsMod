using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Skadi
{
	public class SkadisGreatsword : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [78, 96, 113];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.5f,
				MaxInstances = 2,
			};
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 94;
			Item.height = 92;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.knockBack = 8f;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = false;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			// 右键：激活技能
			if (player.altFunctionUse == 2) {
				if (mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					mp.SkillTimer = 0;
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}

			// 左键攻击伤害
			if (mp.SkillActive && mp.Skill == 1) {
				// S2 涌潮悲歌：攻击力+170%
				Item.damage = (int)(EliteDamage[EliteStage] * 2.7f);
			} else {
				Item.damage = EliteDamage[EliteStage];
			}

			return base.CanUseItem(player);
		}
	}
}
