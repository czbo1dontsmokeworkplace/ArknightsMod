
using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Texas
{
	public class ArtsBlade : ModItem
	{
		public override void SetDefaults()
		{
			Item.damage = 72;
			Item.DamageType = DamageClass.Melee;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 33;
			Item.useAnimation = 33;
			Item.useStyle = 1;
			Item.knockBack = 3;
			Item.value = 10000;
			Item.rare = 3;
            Item.UseSound = ArtsBladeSound;
            Item.autoReuse = true;
		}

        SoundStyle ArtsBladeSound = new SoundStyle("ArknightsMod/Content/Projectiles/Vanguard/Texas/ArtsBladeSound") with
        {
            Volume = 0.5f,
            PitchVariance = 0.3f,
            MaxInstances = 0,
            //SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
        };

        public override void AddRecipes()
		{
            CreateRecipe().AddIngredient(ItemID.HellstoneBar, 20)
                .AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaMeteorPhaseblade, 1)
                .AddTile(ModContent.TileType<FactoryTile>()).Register();

        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {

            Lighting.AddLight(player.Center, 1f, 0.9f, 0.8f);
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 270, 0f, 0f, 0, default(Color), 0.9f);
            Main.dust[dust].noGravity = true;

        }

    }
}
