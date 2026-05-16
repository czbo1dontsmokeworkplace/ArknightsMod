using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Shaw
{
    public class ShawAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 53;
            Item.height = 40;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.axe = 55;
            Item.damage = 25;
            Item.knockBack = 4.5f;
            //Item.value = Item.buyPrice(gold: 1);
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;

            Item.attackSpeedOnlyAffectsWeaponAnimation = true;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            base.MeleeEffects(player, hitbox);
        }
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Material.Oriron>(2);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
	}
}
