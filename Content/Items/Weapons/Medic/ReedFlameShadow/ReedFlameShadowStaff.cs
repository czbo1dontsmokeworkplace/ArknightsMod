using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content;
using ArknightsMod.Players;
using ArknightsMod.Content.Projectiles.Medic.ReedFlameShadow;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Weapons.Medic.ReedFlameShadow
{
	// 焰影苇草的法杖 —— 医疗干员，火焰系普攻。
	// 三个技能已接入通用技力系统：一技能「迅捷打击」强化普攻，二技能「枯荣共息」环绕火焰弹幕，
	// 三技能「生命火种」攻击力 +60% + 普攻分叉两股 + 必定灼痕（灼痕带每秒流失与死亡爆炸）。
	public class ReedFlameShadowStaff : ExpansionWeaponBase
	{
		// 占位阶段保持三个精英化阶段的原有普攻伤害一致。
		protected override int[] EliteDamage => [42, 42, 42];

		/// <summary>三技能的攻击力倍率。</summary>
		public const float Skill3DamageMultiplier = 1.6f;
		/// <summary>三技能普攻分叉的夹角（左右各偏这么多）。</summary>
		public const float Skill3ForkAngle = MathHelper.Pi / 22.5f; // 8°
		/// <summary>标记"这一发是技能三射出的"的 ai[0] 值（普攻弹幕平时不用 ai）。</summary>
		public const float Skill3ShotFlag = 1f;

		private static SoundStyle SkillActivate2Sfx;
		private static SoundStyle SkillActivate3Sfx;

		public override void Load() {
			SkillActivate2Sfx = new SoundStyle("ArknightsMod/Sounds/SkillActive2") {
				Volume = 0.5f,
				MaxInstances = 2,
			};
			SkillActivate3Sfx = new SoundStyle("ArknightsMod/Sounds/SkillActive3") {
				Volume = 0.5f,
				MaxInstances = 2,
			};
		}

		public override void SetDefaults() {
			Item.width = 66;
			Item.height = 66;

			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.mana = 8;
			Item.knockBack = 3.5f;
			Item.crit = 6;

			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;

			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
			Item.UseSound = SoundID.Item34 with { Volume = 0.5f, Pitch = 0.35f };

			Item.shoot = ModContent.ProjectileType<ReedFlameShadowFlame>();
			Item.shootSpeed = 11f;
		}

		// 从杖头而不是玩家中心发射，观感上"火从杖尖出来"
		public override Vector2? HoldoutOffset() => new Vector2(-4f, -2f);

		public override bool CanUseItem(Player player) {
			if (ArknightsKeybinds.SkillActivatePressed(player)) {
				TryActivateSkill(player);
				return false;
			}

			return base.CanUseItem(player);
		}

		private static bool Skill1Active(Player player) {
			WeaponPlayer weaponPlayer = player.GetModPlayer<WeaponPlayer>();
			return weaponPlayer.SkillActive && weaponPlayer.Skill == 0
				&& weaponPlayer.CurrentSkill?.Key.Item == nameof(ReedFlameShadowStaff);
		}

		private static bool Skill3Active(Player player) {
			WeaponPlayer weaponPlayer = player.GetModPlayer<WeaponPlayer>();
			return weaponPlayer.SkillActive && weaponPlayer.Skill == 2
				&& weaponPlayer.CurrentSkill?.Key.Item == nameof(ReedFlameShadowStaff);
		}

		public override float UseSpeedMultiplier(Player player) => Skill1Active(player) ? 1.45f : 1f;

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			if (Skill1Active(player))
				damage *= 1.45f;
			else if (Skill3Active(player))
				damage *= Skill3DamageMultiplier;
		}

		private static void TryActivateSkill(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return;

			WeaponPlayer weaponPlayer = player.GetModPlayer<WeaponPlayer>();
			if (weaponPlayer.CurrentSkill?.Key.Item != nameof(ReedFlameShadowStaff)
				|| weaponPlayer.StockCount <= 0 || weaponPlayer.SkillActive)
				return;

			weaponPlayer.SkillActive = true;
			weaponPlayer.SkillTimer = 0;
			weaponPlayer.DelStockCount();

			switch (weaponPlayer.Skill) {
				case 1:
					ActivateSkill2(player);
					break;
				case 2:
					ActivateSkill3(player);
					break;
			}
		}

		// 二技能「枯荣共息」：在 160px 轨道上召唤三颗环绕火焰弹幕。
		// 相位推进、每 20 帧补弹全部由 ReedFlameShadowStaffPlayer 维护，
		// 这里只负责"开启瞬间"的初始化与表现。
		private static void ActivateSkill2(Player player) {
			player.GetModPlayer<ReedFlameShadowStaffPlayer>().BeginS2();
			PlayActivateFeedback(player, SkillActivate2Sfx, 26, 46f);
		}

		// 三技能「生命火种」：没有需要初始化的常驻状态——攻击力加成走 ModifyWeaponDamage，
		// 普攻分叉走 Shoot，灼痕的施加在命中时处理（见 ReedFlameShadowFlame.OnHitNPC），
		// 灼痕的寿命由 ReedFlameShadowStaffPlayer.UpdateSkill3Scar 维护。
		private static void ActivateSkill3(Player player) {
			PlayActivateFeedback(player, SkillActivate3Sfx, 34, 54f);
		}

		/// <summary>技能开启表现：音效 + 玩家身上一圈火焰粒子。</summary>
		private static void PlayActivateFeedback(Player player, SoundStyle sound, int dustCount, float radius) {
			if (Main.dedServ)
				return;

			SoundEngine.PlaySound(sound, player.Center);

			for (int i = 0; i < dustCount; i++) {
				Vector2 dir = (MathHelper.TwoPi * i / dustCount).ToRotationVector2();
				Dust d = Dust.NewDustPerfect(player.MountedCenter + dir * radius,
					Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
					dir * Main.rand.NextFloat(1.2f, 2.4f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
					100, default, Main.rand.NextFloat(1.0f, 1.6f));
				d.noGravity = true;
				d.fadeIn = 0.7f;
			}
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
				Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// 杖尖位置：沿瞄准方向从手部往外推一段
			Vector2 muzzle = position + Vector2.Normalize(velocity) * 34f;

			if (Skill3Active(player)) {
				// 三技能：普攻分叉为两股，各打满伤。
				// 把"这是技能三射击 / 灼痕该持续多少帧 / 当前攻击力"写进弹幕的 ai——
				// 这些值会随弹幕一起同步到其它端，因此各端算出的灼痕与伤害完全一致
				// （技能状态本身是客户端本地的，服务器读不到）。
				int scarTicks = player.GetModPlayer<ReedFlameShadowStaffPlayer>().S3RemainingTicks();
				int power = Math.Max(1, player.GetWeaponDamage(Item));

				float[] offsets = [-Skill3ForkAngle, Skill3ForkAngle];
				foreach (float offset in offsets) {
					Vector2 forkVel = velocity.RotatedBy(offset).RotatedByRandom(MathHelper.ToRadians(1.5f));
					Projectile.NewProjectile(source, muzzle, forkVel, type, damage, knockback, player.whoAmI,
						Skill3ShotFlag, scarTicks, power);
				}
			}
			else {
				// 轻微散射，连发时不会每一发完全重叠
				Vector2 vel = velocity.RotatedByRandom(MathHelper.ToRadians(2.5f));
				Projectile.NewProjectile(source, muzzle, vel, type, damage, knockback, player.whoAmI);
			}

			// 杖尖起手火花
			if (!Main.dedServ) {
				for (int i = 0; i < 8; i++) {
					Dust d = Dust.NewDustPerfect(muzzle,
						Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
						velocity * Main.rand.NextFloat(0.05f, 0.25f) + Main.rand.NextVector2Circular(1.6f, 1.6f),
						100, default, Main.rand.NextFloat(0.9f, 1.6f));
					d.noGravity = true;
				}
			}

			return false;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrirockConcentration>(8)
			.AddIngredient<LoxicKohl>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
