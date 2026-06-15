using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai
{
    [AutoloadEquip(EquipType.Head)]
	public class ExusiaiHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 168;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}



		public override void UpdateVanityEquip(Player player) {
			Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ExusiaiHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<SugarLump>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
		internal class ExusiaiHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<ExusiaiHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Sniper/Exusiai/ExusiaiHead_Ring").Value;

				var offset = new Vector2(1, -3) + new Vector2(0, -26);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
