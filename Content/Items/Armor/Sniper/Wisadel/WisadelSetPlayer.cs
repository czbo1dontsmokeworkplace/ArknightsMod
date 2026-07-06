using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Projectiles.Sniper.Wisadel;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Wisadel
{
	internal class WisadelSetPlayer : ArknightsArmorPlayer
	{
		public bool WisadelHelmetActive;
		public bool WisadelSetActive;

		/// <summary>每个槽位的复活冷却（帧），<= 0 即可生成</summary>
		public int[] revenantSlotCooldown = new int[3];
		/// <summary>每个槽位上帧是否存活，用于检测刚死</summary>
		private bool[] prevSlotAlive = new bool[3];

		private const int MaxShadows = 3;
		private const float OrbitSpeed = 0.04f;
		private const int RevenantCooldownMax = 60 * 60; // 60 秒
		private const int MarkDuration = 3 * 60; // 3 秒

		/// <summary>三个魂影的公共旋转角，由 SetPlayer 统一推进</summary>
		public float globalOrbitAngle;

		public override void ResetEffects() {
			WisadelHelmetActive = false;
			WisadelSetActive = false;
		}

		public override void PostUpdateEquips() {
			WisadelHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<WisadelHead>())
				&& Player.armor[0].neoarmor().hasUpgraded;
			WisadelSetActive = WisadelHelmetActive
				&& Player.armor[1].type == ModContent.ItemType<WisadelBody>() && Player.armor[1].neoarmor().hasUpgraded
				&& Player.armor[2].type == ModContent.ItemType<WisadelLegs>() && Player.armor[2].neoarmor().hasUpgraded;

			OperatorSetEquipHelper.ApplySetBonusText(Player, WisadelSetActive, "Mods.ArknightsMod.ArmorSets.Wisadel.SetBonus");
		}

		public override void PostUpdate() {
			int shadowType = ModContent.ProjectileType<RevenantsShadow>();

			// 统一推进三个魂影的公共旋转角
			if (WisadelHelmetActive)
				globalOrbitAngle += OrbitSpeed;

			// 收集当前存活的槽位
			bool[] slotAlive = new bool[3];
			foreach (Projectile p in Main.ActiveProjectiles) {
				if (p.type == shadowType && p.owner == Player.whoAmI) {
					int s = (int)p.ai[2];
					if (s >= 0 && s < 3)
						slotAlive[s] = true;
				}
			}

			for (int i = 0; i < 3; i++) {
				if (!WisadelHelmetActive) {
					revenantSlotCooldown[i] = 0;
					prevSlotAlive[i] = false;
					continue;
				}

				if (slotAlive[i]) {
					revenantSlotCooldown[i] = 0;
				} else {
					// 该槽位没有魂影
					if (prevSlotAlive[i]) {
						// 上帧还活着 → 刚死，启动冷却
						revenantSlotCooldown[i] = RevenantCooldownMax;
					} else if (revenantSlotCooldown[i] > 0) {
						// 冷却中
						revenantSlotCooldown[i]--;
					}
					// cooldown <= 0 且上帧也死 → 冷却完成，待生成
				}

				prevSlotAlive[i] = slotAlive[i];
			}

			// 冷却结束或从未生成 → 补位
			if (WisadelHelmetActive) {
				for (int i = 0; i < 3; i++) {
					if (!slotAlive[i] && revenantSlotCooldown[i] <= 0) {
						Projectile.NewProjectile(
							Player.GetSource_Accessory(Player.armor[0]),
							Player.Center, Vector2.Zero, shadowType, 0, 0f, Player.whoAmI,
							ai0: RevenantsShadow.MaxBlockLife,
							ai2: i);
					}
				}
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (WisadelSetActive && item.DamageType.CountsAsClass(DamageClass.Ranged)) {
				modifiers.SourceDamage *= 1.15f;
				target.AddBuff(ModContent.BuffType<WisadelMarkDebuff>(), MarkDuration);
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (WisadelSetActive && proj.DamageType.CountsAsClass(DamageClass.Ranged)) {
				modifiers.SourceDamage *= 1.15f;
				target.AddBuff(ModContent.BuffType<WisadelMarkDebuff>(), MarkDuration);
			}
		}
	}
}
