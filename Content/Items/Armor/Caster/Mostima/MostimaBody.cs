using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	[AutoloadEquip(EquipType.Body)]
	public class MostimaBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 92;

		public override void SetArmorDefaults() {
			Item.defense = 10;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MostimaBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<GrindstonePentahydrate>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class MostimaWingLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<MostimaBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Caster/Mostima/MostimaBody_Wing").Value;

				var offset = new Vector2(1, -3) + new Vector2(-2, 8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
