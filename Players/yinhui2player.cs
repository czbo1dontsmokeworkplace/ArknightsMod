using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;
using ArknightsMod.Content.Items.Weapons;
using System.Security.Policy;
using UtfUnknown.Core.Analyzers.Chinese;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using rail;
using Terraria.Audio;
using System.Collections.Generic;

namespace ArknightsMod.Players
{
    public class yinhui2player : ModPlayer
    {
		public bool yinhui2=false;
		public override void ResetEffects()
        {
			//棘刺2技能特殊效果
			if (yinhui2 == true)
            {
				Player.statDefense *= 2f;
				Player.lifeRegen += (int)(Player.statLifeMax2 * 0.12f);
			}
			if (Main.myPlayer != Player.whoAmI)
				return;  // 只处理本地玩家
			bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<YinHui>();
			if (!isHoldingTargetWeapon) {
				Player.GetModPlayer<yinhui2player>().yinhui2 = false;
			}

		}
    }
	public class yinhui3player : ModPlayer
	{
		public bool yinhui3 = false;
		public override void ResetEffects() {
			if (yinhui3 == true) {
				Player.statDefense *= 0.3f;
			}
			if (Main.myPlayer != Player.whoAmI)
				return;  // 只处理本地玩家
			bool isHoldingTargetWeapon2 = Player.HeldItem.type == ModContent.ItemType<YinHui>();
			if (!isHoldingTargetWeapon2) {
				Player.GetModPlayer<yinhui3player>().yinhui3 = false;
			}

		}
	}
}