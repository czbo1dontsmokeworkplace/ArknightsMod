using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	[AutoloadEquip(EquipType.Head)]
	public class AnselHead : NeoArmorHead
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 114;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<AnselHead>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<OrironShard>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class AnselHeadLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<AnselHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Medic/Ansel/AnselHead_Ear").Value;

				var offset = new Vector2(0, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
		}
	}
}
