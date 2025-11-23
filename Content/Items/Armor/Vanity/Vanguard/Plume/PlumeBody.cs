using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Plume
{
	[AutoloadEquip(EquipType.Body)]
	public class PlumeBody : ArknightsVanityBody
	{
		public override int Rarity => 6;
	}
}
