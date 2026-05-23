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

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	[AutoloadEquip(EquipType.Head)]
	public class MelanthaHead : NeoArmorHead
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 274;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<MelanthaBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<MelanthaLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorEquip(Player Player) {
			Player.GetModPlayer<ArknightsArmorPlayer>().extraDefenseBonus -= 0.2f;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<MelanthaSetPlayer>().MelanthaSetActive = true;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MelanthaHead>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Ester>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class MelanthaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head1 = new(ModContent.ItemType<MelanthaHead>());
				return (drawInfo.drawPlayer.head == head1.headSlot) && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Guard/Melantha/MelanthaHead_Back", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				var offset = new Vector2(0, -3) + new Vector2(0, -8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
