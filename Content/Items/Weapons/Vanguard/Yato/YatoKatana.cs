using ArknightsMod.Content.Projectiles.Vanguard.Yato;
using ArknightsMod.Content.Tiles.Infrastructure;

using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Yato
{
	public class YatoKatana : UpgradeWeaponBase
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
			Item.useStyle = ItemUseStyleID.Rapier; 
			Item.useAnimation = 33;
			Item.useTime = 33;
			Item.width = 38;
			Item.height = 42;
			Item.DamageType = DamageClass.Melee;
			Item.channel = true; 
			Item.autoReuse = false;
			Item.noUseGraphic = true; 
			Item.noMelee = true; 

			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(0, 0, 3, 20);

			Item.shoot = ModContent.ProjectileType<YatoKatana_Projectile>(); // The projectile is what makes a shortsword work
			Item.shootSpeed = 2.3f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values

			Item.UseSound = SoundID.Item1;
		}

		public override void AddRecipes()
		{
			int[] metal5 = { ItemID.TinBar, ItemID.CopperBar };
			int[] metal3 = { ItemID.IronBar, ItemID.LeadBar }; 
			foreach (int bar5 in metal5)
			foreach (int bar3 in metal3)
			{
				Recipe recipe = CreateRecipe();
				recipe.AddIngredient(bar5, 5);  // 5 锡/铜
				recipe.AddIngredient(bar3, 3);  // 3 铁/铅
				recipe.AddTile(ModContent.TileType<FactoryTile>());
				recipe.Register();
			}
		}

		public class YatoPlayer : ModPlayer
        {
            public override void UpdateEquips() {
				var it = Player.HeldItem;
				if (it.type == ModContent.ItemType<YatoKatana>() ) {
					Player.moveSpeed += 0.1f;
					if (Main.mouseRight) Player.moveSpeed += 0.15f;

				}
				base.UpdateEquips();
			}
        }

	}
}