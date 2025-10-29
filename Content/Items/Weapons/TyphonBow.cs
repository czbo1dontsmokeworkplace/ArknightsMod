using ArknightsMod.Content.Projectiles.Typhon;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons
{
	public class TyphonBow : ModItem
	{
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Material.PolymerizationPreparation>(4);
			recipe.AddIngredient<Material.RefinedSolvent>(7);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
		public override void SetDefaults()
		{
			Item.damage = 50;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 60;
			Item.useAnimation = 60;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.knockBack = 6;
			Item.value = Item.buyPrice(gold: 1);
			Item.rare = ItemRarityID.Red;
			Item.autoReuse = true;
			Item.shoot = ModContent.ProjectileType<TyphonArrow>();
			Item.shootSpeed = 10;
		}

        public override Vector2? HoldoutOrigin()
        {
            return base.HoldoutOrigin();
        }

        public override Vector2? HoldoutOffset()
        {
            return new(-8, 0);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
			//float r = -Vector2.UnitY.ToRotation();
			//float r2 = MathHelper.WrapAngle(velocity.ToRotation() - r);
			//float r3 = Utils.Remap(r2, -MathHelper.Pi, MathHelper.Pi, -MathHelper.Pi / 3, MathHelper.Pi / 3);
			//velocity = velocity.RotatedBy(r3 - r2);
			velocity = velocity.RotatedBy(-velocity.ToRotation()).RotatedBy((Main.MouseWorld + new Vector2(0, -1000) - position).ToRotation());
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
			if (player.whoAmI == Main.myPlayer)
				Projectile.NewProjectile(source, position, velocity * 2, type, damage, knockback, player.whoAmI, Main.MouseWorld.X, Main.MouseWorld.Y);
            return false;
        }
	}
}
