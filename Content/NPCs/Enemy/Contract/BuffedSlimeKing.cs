using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Contract
{
	public static class KingSlimeAI
	{
		
		public static bool BuffedKingSlimeAI(NPC npc,Mod mod) {

			float liferatio = npc.life / (float)npc.lifeMax;

			return false;
		}
	}
}
