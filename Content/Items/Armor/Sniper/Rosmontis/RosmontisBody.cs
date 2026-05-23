using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	[AutoloadEquip(EquipType.Body)]
	public class RosmontisBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 97;

		public override void SetArmorDefaults() {
			Item.defense = 18;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<RosmontisBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<OptimizedDevice>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class RosmontisBackLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<RosmontisBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Sniper/Rosmontis/RosmontisBody_Back").Value;

				var offset = new Vector2(0, -3) + new Vector2(0, 0);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
