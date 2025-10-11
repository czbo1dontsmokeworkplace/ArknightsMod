using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Bagpipe
{
	[AutoloadEquip(EquipType.Body)]
	public class BagpipeBody : ArknightsVanityBody
	{
		public override int Rarity => 6;
	}
}
