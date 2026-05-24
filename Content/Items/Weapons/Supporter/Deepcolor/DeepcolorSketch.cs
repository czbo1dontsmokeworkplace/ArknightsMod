using ArknightsMod.Content.Buffs.Supporter.Deepcolor;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Supporter.Deepcolor;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Deepcolor
{
	public class DeepcolorSketch : UpgradeWeaponBase
	{
		private static SoundStyle SkillActiveSound;

		public override void Load() {
			SkillActiveSound = new SoundStyle("ArknightsMod/Sounds/SkillActive1") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
		}

		public override void SetDefaults() {
			Item.maxStack = 1;
			Item.damage = 46;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 32;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.noUseGraphic = true;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Green;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item44;
			Item.shoot = ModContent.ProjectileType<DeepcolorMinion>();
			Item.shootSpeed = 0f;
			Item.buffType = ModContent.BuffType<DeepcolorMinionBuff>();
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2) {
				if (Main.myPlayer == player.whoAmI) {
					var modPlayer = player.GetModPlayer<WeaponPlayer>();
					if (modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
						modPlayer.SkillActive = true;
						modPlayer.SkillTimer = 0;
						modPlayer.DelStockCount();
						SoundEngine.PlaySound(SkillActiveSound, player.Center);
					}
				}
				return false;
			}

			if (!player.GetModPlayer<DeepcolorSketchPlayer>().CanRedeploy)
				return false;

			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			if (DeepcolorMinion.CountActiveForPlayer(player) >= DeepcolorMinion.MaxTentacles)
				DeepcolorMinion.TryDespawnOldestForPlayer(player);

			player.AddBuff(Item.buffType, 2);
			Vector2 spawnPos = DeepcolorMinion.FindGroundSpawnPosition(Main.MouseWorld, DeepcolorMinion.FrameWidth, DeepcolorMinion.FrameHeight);
			Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
			player.GetModPlayer<DeepcolorSketchPlayer>().StartRedeployCooldown();
			return false;
		}
	}
}
