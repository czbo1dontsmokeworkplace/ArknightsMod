using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content;

namespace ArknightsMod.Content.Items.Weapons.Guard.Frostleaf
{
	public class FrostleafAxe : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [40, 48, 58];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 60;
			Item.height = 62;
			Item.useTime = 31;
			Item.useAnimation = 31;
			Item.knockBack = 7f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool AltFunctionUse(Player player) => false;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			if (ArknightsKeybinds.SkillActivatePressed(player)) {
				if (mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					mp.SkillTimer = 0;
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}

			// S1 攻击充能
			if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && mp.Skill == 0)
				mp.OffensiveRecovery();

			return base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 寒霜枪刃：激活后下一击 ×1.5 伤害
			if (mp.SkillActive && mp.Skill == 0)
				damage *= 1.5f;
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (!mp.SkillActive) return;

			if (mp.Skill == 0) {
				// S1 寒霜枪刃：命中时冰冻目标，技能立即结束（一次性）
				target.AddBuff(BuffID.Frozen, 3 * 60);
				mp.SkillActive = false;
			} else if (mp.Skill == 1) {
				// S2 凝冰枪刃：每击减速，40% 概率冰冻 2 秒
				target.AddBuff(BuffID.Slow, 3 * 60);
				if (Main.rand.NextFloat() < 0.4f)
					target.AddBuff(BuffID.Frozen, 2 * 60);
			}
		}
	}
}
