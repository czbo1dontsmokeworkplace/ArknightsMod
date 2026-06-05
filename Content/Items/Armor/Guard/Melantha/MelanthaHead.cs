using ArknightsMod.Common;
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

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}

		internal class MelanthaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				int headSlot = EquipLoader.GetEquipSlot(Mod, nameof(MelanthaHead), EquipType.Head);
				return drawInfo.drawPlayer.head == headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>(
					"ArknightsMod/Content/Items/Armor/Guard/Melantha/MelanthaHead_Back",
					ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				var offset = new Vector2(0, -3) + new Vector2(0, -8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}

			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
