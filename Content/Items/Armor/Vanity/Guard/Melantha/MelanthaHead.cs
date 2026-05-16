using ArknightsMod.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha
{
	[AutoloadEquip(EquipType.Head)]
	public class MelanthaHead : ArknightsVanityHead
	{
		public override int Rarity => 3;
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}
		
		internal class MelanthaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head1 = new(ModContent.ItemType<MelanthaHead>());
				Item head2 = new(ModContent.ItemType<Armor.ArmorMelanthaHead>());
				return (drawInfo.drawPlayer.head == head1.headSlot|| drawInfo.drawPlayer.head == head2.headSlot) && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Guard/Melantha/MelanthaHead_Back", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				var offset = new Vector2(0, -3) + new Vector2(0, -8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
