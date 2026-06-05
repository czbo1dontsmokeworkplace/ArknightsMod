using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	[AutoloadEquip(EquipType.Head)]
	public class UtageHead : NeoArmorHead
	{
		public override int Rarity => 4;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}
	}
}
