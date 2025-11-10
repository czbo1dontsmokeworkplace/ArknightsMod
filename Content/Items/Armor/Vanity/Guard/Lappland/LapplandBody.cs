using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Lappland
{
	[AutoloadEquip(EquipType.Body)]
	public class LapplandBody : ArknightsVanityBody
	{
		public override int Rarity => 5;
	}
}
