using ArknightsMod.Content.Projectiles.Vanguard.Yato;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Yato
{
	public class YatoKatana : ModItem
	{
		private SoundStyle use;
		public override void Load() => use = new SoundStyle("ArknightsMod/Sounds/YatoKatanaS0") {
			Volume = 0.4f,
			MaxInstances = 1, //This dicatates how many instances of a sound can be playing at the same time. The default is 1. Adjust this to allow overlapping sounds.
		};
		public override void SetStaticDefaults() {
			// DisplayName.SetDefault("Yato's Katana");
			// Tooltip.SetDefault("Yato has joined the team.");
		}

		public override void SetDefaults() {
			Item.damage = 15;
			Item.knockBack = 4f;
			Item.useStyle = ItemUseStyleID.Rapier; // Makes the player do the proper arm motion
			Item.useAnimation = 33;
			Item.useTime = 33;
			Item.width = 40;
			Item.height = 40;
			Item.DamageType = DamageClass.MeleeNoSpeed;
			Item.channel = true; //Channel so that you can held the weapon [Important]
			Item.autoReuse = false;
			Item.noUseGraphic = true; // The sword is actually a "projectile", so the item should not be visible when used
			Item.noMelee = true; // The projectile will do the damage and not the item

			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(0, 0, 3, 20);

			Item.shoot = ModContent.ProjectileType<YatoKatana_Projectile>(); // The projectile is what makes a shortsword work
			Item.shootSpeed = 2.3f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values

			// The sound that this item plays when used. Need "using Terraria.Audio;"
			Item.UseSound = SoundID.Item1;
		}

		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Material.OrirockCube>(5); //Please check here: https://github.com/tModLoader/tModLoader/wiki/Intermediate-Recipes#recipegroups
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}