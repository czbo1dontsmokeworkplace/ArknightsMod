using System;
using System.Collections.Generic;
using ArknightsMod.Content.Buffs.Specialist.Scene;
using ArknightsMod.Content.Projectiles.Specialist.Scene;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Scene
{
	// 稀音的摄像机：
	// 左键朝光标射出魔法快门弹丸（消耗魔法值），伴随圆环快门特效；
	// 右键在鼠标位置正下方的地面上部署「移动摄影车」召唤物（5 秒冷却、最多 5 辆、占用仆从位）。
	public class SceneCamera : ModItem
	{
		public override void SetDefaults() {
			Item.width = 34;
			Item.height = 26;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.knockBack = 0f;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Green;
			Item.UseSound = SoundID.Item44;
			Item.autoReuse = true;
			Item.DamageType = DamageClass.Magic; // 左键普通攻击为魔法伤害
			Item.damage = 24;
			Item.mana = 8;                        // 左键消耗魔法值（右键部署经 ModifyManaCost 免除）
			Item.shoot = ModContent.ProjectileType<SceneCameraBullet>();
			Item.shootSpeed = 11f;
			Item.buffType = ModContent.BuffType<CameraTruckBuff>();
		}

		// 左键：魔法快门弹丸；右键：部署移动摄影车。
		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2)
				return player.GetModPlayer<SceneCameraPlayer>().CanRedeploy; // 右键受 5 秒冷却限制
			return true;
		}

		// 仅左键自动连发，右键部署需逐次点击。
		public override bool? CanAutoReuseItem(Player player) => player.altFunctionUse != 2;

		// 右键部署不消耗魔法，仅左键消耗。
		public override void ModifyManaCost(Player player, ref float reduce, ref float mult) {
			if (player.altFunctionUse == 2)
				mult = 0f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			if (player.altFunctionUse == 2)
				return DeployTruck(player, source, knockback);

			// 左键：朝光标方向开火。
			Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX);

			// 圆环快门特效（纯视觉），出现在身前、离玩家略近一点。
			const float ringForwardOffset = 22f;
			Vector2 ringCenter = player.Center + dir * ringForwardOffset;
			Projectile.NewProjectile(source, ringCenter, dir * 4f,
				ModContent.ProjectileType<SceneCameraShot>(), 0, 0f, player.whoAmI);

			// 抛物线魔法弹丸（承担伤害），从枪口射出后受重力下坠。
			Vector2 muzzle = player.Center + dir * 16f;
			Projectile.NewProjectile(source, muzzle, dir * Item.shootSpeed,
				ModContent.ProjectileType<SceneCameraBullet>(), damage, knockback, player.whoAmI);
			return false;
		}

		private bool DeployTruck(Player player, EntitySource_ItemUse_WithAmmo source, float knockback) {
			GetDeployStats(player, out int current, out int effectiveMax, out _, out _);
			if (effectiveMax <= 0)
				return false; // 没有可用仆从位

			if (current >= effectiveMax)
				CameraTruck.TryRemoveOldestForPlayer(player); // 满员：消除最早的一辆，再在新位置部署

			player.AddBuff(Item.buffType, 2);
			int summonDamage = (int)Math.Round(player.GetDamage(DamageClass.Summon).ApplyTo(Item.damage));
			Vector2 spawnPos = CameraTruck.FindGroundSpawnPosition(Main.MouseWorld, CameraTruck.Width, CameraTruck.Height);
			Projectile.NewProjectile(source, spawnPos, Vector2.Zero,
				ModContent.ProjectileType<CameraTruck>(), summonDamage, knockback, player.whoAmI);
			player.GetModPlayer<SceneCameraPlayer>().StartRedeployCooldown();
			return false;
		}

		// 统计当前可部署情况：受 5 辆硬上限与剩余仆从位共同约束。
		private static void GetDeployStats(Player player, out int current, out int effectiveMax, out int remaining, out float otherSlots) {
			current = CameraTruck.CountActiveForPlayer(player);
			float usedSlots = player.slotsMinions;
			int maxSlots = player.maxMinions;
			otherSlots = Math.Max(0f, usedSlots - current); // 非摄影车占用的仆从位
			float freeSlots = Math.Max(0f, maxSlots - usedSlots);
			int slotCapForTrucks = current + (int)Math.Floor(freeSlots);
			effectiveMax = Math.Clamp(Math.Min(CameraTruck.MaxTrucks, slotCapForTrucks), 0, CameraTruck.MaxTrucks);
			remaining = Math.Max(0, effectiveMax - current);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			Player player = Main.LocalPlayer;
			GetDeployStats(player, out int current, out int effectiveMax, out int remaining, out float otherSlots);

			tooltips.Add(new TooltipLine(Mod, "SceneCameraDeploy",
				Language.GetTextValue("Mods.ArknightsMod.Items.SceneCamera.DeployInfo", current, effectiveMax, remaining)) {
				OverrideColor = new Color(130, 230, 120)
			});

			int otherInt = (int)Math.Round(otherSlots);
			if (otherInt > 0) {
				tooltips.Add(new TooltipLine(Mod, "SceneCameraSlotNote",
					Language.GetTextValue("Mods.ArknightsMod.Items.SceneCamera.DeploySlotNote", otherInt)) {
					OverrideColor = new Color(210, 190, 120)
				});
			}
		}
	}
}
