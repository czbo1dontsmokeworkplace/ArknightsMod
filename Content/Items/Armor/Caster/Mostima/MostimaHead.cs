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
	[AutoloadEquip(EquipType.Head)]
	public class MostimaHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 183;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MostimaHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<GrindstonePentahydrate>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class MostimaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<MostimaHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Caster/Mostima/MostimaHead_Ring").Value;
				var offset = new Vector2(0, -3) + new Vector2(2, -28);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
	
