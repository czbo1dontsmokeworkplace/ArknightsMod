using ArknightsMod.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WisdelItem = ArknightsMod.Content.Items.Weapons.WisdelCannon;

namespace ArknightsMod.Content.Projectiles.Wisdel
{
	public static class WisdelProbeHelper {
		public static WisdelProbePlayer wisdel(this Player player) {
			return player.GetModPlayer<WisdelProbePlayer>();
		}
	}
	public class WisdelProbePlayer : ModPlayer
	{
		public int currentUse;
		public int coolDown;
		public int mode;
		public int channelTimer;
		public override void PostUpdate() {
			if (coolDown > 0) {
				coolDown--;
			}
		}
	}
}
