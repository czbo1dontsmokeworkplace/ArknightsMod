using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Shaw
{
	public class ShawAxe : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [39, 47, 57];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 52;
			Item.height = 52;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 6f;
			Item.value = Item.sellPrice(silver: 35);
			Item.rare = ItemRarityID.Green;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.crit = 4;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();

			if (player.altFunctionUse == 2) {
				if (mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					mp.SkillTimer = 0;
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);

					// S2 高压水炮：立即击退并伤害前方范围内所有敌人
					if (mp.Skill == 1) {
						int dmg = (int)(player.GetWeaponDamage(Item) * 3.0f);
						Vector2 knockDir = new Vector2(player.direction, -0.2f);
						foreach (NPC npc in Main.npc) {
							if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
							if (Vector2.Distance(npc.Center, player.Center) > 160f) continue;
							// 仅击退前方（同向）的敌人
							if ((npc.Center.X - player.Center.X) * player.direction < -20f) continue;
							npc.StrikeNPC(npc.CalculateHitInfo(dmg, player.direction, false, 14f, DamageClass.Melee));
							npc.velocity += knockDir * 16f;
						}
						mp.SkillActive = false;
					}
				}
				return false;
			}

			mp.OffensiveRecovery();
			return base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			var mp = player.GetModPlayer<WeaponPlayer>();
			// S1 水蒸气泵：下次攻击 ×1.5
			if (mp.SkillActive && mp.Skill == 0)
				damage *= 1.5f;
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (!mp.SkillActive || mp.Skill != 0) return;
			// S1：大力击退目标，单次触发后消耗
			target.velocity += new Vector2(player.direction * 18f, -3f);
			mp.SkillActive = false;
		}
	}
}
