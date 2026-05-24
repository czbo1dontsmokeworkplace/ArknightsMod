using ArknightsMod.Content.Buffs.Summoner;
using ArknightsMod.Content.Projectiles.Summoner;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Summoner
{
	public class DeepcolorSketch : ModItem
	{
		public override void SetDefaults() {
			Item.maxStack = 1;
			Item.damage = 14;
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

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// 已满 4 只时，在新点击位置召唤并替换最早的一只
			if (DeepcolorMinion.CountActiveForPlayer(player) >= DeepcolorMinion.MaxTentacles)
				DeepcolorMinion.TryDespawnOldestForPlayer(player);

			player.AddBuff(Item.buffType, 2);
			Vector2 spawnPos = DeepcolorMinion.FindGroundSpawnPosition(Main.MouseWorld, DeepcolorMinion.FrameWidth, DeepcolorMinion.FrameHeight);
			Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
			return false;
		}
	}
}
