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

namespace ArknightsMod.Content.Items.Armor.Sniper.Wisadel
{
	[AutoloadEquip(EquipType.Body)]
	public class WisadelBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 95;

		public override void SetStaticDefaultsNoServer()
		{ 
			Item.hasVanityEffects = true;
		}

		public override void SetArmorDefaults() {
			Item.defense = 20;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<WisadelBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<TransmutedSaltAgglomerate>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class WisadelWingLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<WisadelBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Sniper/Wisadel/WisadelBody_Back").Value;

				var offset = new Vector2(1, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
