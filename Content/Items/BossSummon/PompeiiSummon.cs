using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.NPCs.Enemy.OF.Pmp;
using Terraria.Audio;

namespace ArknightsMod.Content.Items.BossSummon
{
	public class PompeiiSummon : ModItem
	{
		public override void AddRecipes() {
			
			CreateRecipe()
				.AddIngredient<Material.OrirockCube>(1)
				.AddIngredient(22, 5)
                .AddIngredient(173,1)
				.Register();
		}

	
	public override void SetDefaults() {
			Item.width = 40;
			Item.height = 40;
			Item.maxStack = 1;
			Item.value = 325;
			Item.rare = 1;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = false;
		}
		public override bool CanUseItem(Player player) {
			return
			!NPC.AnyNPCs(ModContent.NPCType<Pompeii>());
		}
		private int type;
		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				SoundEngine.PlaySound(SoundID.Roar, player.position);
				type = ModContent.NPCType<Pompeii>();
			}
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				NPC.SpawnOnPlayer(player.whoAmI, type);
			}
			
			return true;
		}
	}
}
