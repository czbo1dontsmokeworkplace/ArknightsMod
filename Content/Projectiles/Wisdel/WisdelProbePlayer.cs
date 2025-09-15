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
	public static class WisdelProbeHelper
	{
		public static WisdelProbePlayer wisdel(this Player player)
		{
			return player.GetModPlayer<WisdelProbePlayer>();
		}
	}
	public class WisdelProbePlayer : ModPlayer
	{
		/// <summary>
		/// 当前正在使用的浮游炮
		/// </summary>
		public int currentUse;

		/// <summary>
		/// 攻击冷却
		/// </summary>
		public int coolDown;

		/// <summary>
		/// 模式
		/// </summary>
		public int mode;

		/// <summary>
		/// 模式切换冷却
		/// </summary>
		public int modeSwitchCooldown;

		/// <summary>
		/// 蓄力时间
		/// </summary>
		public int channelTimer;
		public override void PostUpdate()
		{
			if (coolDown > 0)
				coolDown--;

			if (modeSwitchCooldown > 0)
				modeSwitchCooldown--;
		}
	}
}
