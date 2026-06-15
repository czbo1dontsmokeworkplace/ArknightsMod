using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter
{
	[AutoloadEquip(EquipType.Body)]
	public class ExusiaiAlterBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 118;

		public override void SetArmorDefaults() {
			Item.defense = 11;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ExusiaiAlterBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			// .AddIngredient<环烃预制体>(5) 材料缺失！
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class ExusiaiAlterWingLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<ExusiaiAlterBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Specialist/ExusiaiAlter/ExusiaiAlter_Wings").Value;

				var offset = new Vector2(1, -3) + new Vector2(-2, 8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
