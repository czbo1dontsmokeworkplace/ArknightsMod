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
    public class jici2player : ModPlayer
    {
		public bool JiCi2=false;
		public override void ResetEffects()
        {
			//棘刺2技能特殊效果
			if (JiCi2 == true)
            {
                if (JiCi2_JSQ > 0) JiCi2_JSQ++;
                if (JiCi2_JSQ > 36) JiCi2_JSQ = 0;
                Player.statDefense *= 2.1f;
				Player.controlUseItem = false; // 此时设置不会被后续输入覆盖
				Player.itemAnimation = 0;
				Player.itemTime = 0;
			}
			if (Main.myPlayer != Player.whoAmI)
				return;  // 只处理本地玩家
			bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>();
			if (!isHoldingTargetWeapon) {
				Player.GetModPlayer<jici2player>().JiCi2 = false;
			}

		}

        public int JiCi2_JSQ = 0;
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (JiCi2 && JiCi2_JSQ == 0)
            {
                float js = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>() ? 1.6f : .8f;
                JiCi2_JSQ = 1;
                Vector2 velocity = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 10f;
				SoundStyle SoundStyle1 = new SoundStyle("ArknightsMod/Sounds/JiCi2");
				SoundEngine.PlaySound(SoundStyle1);
				Projectile.NewProjectile(Player.GetSource_Death(), Player.Center,
                velocity, ModContent.ProjectileType<JiCi4>(), (int)(Player.HeldItem.damage * js), Player.HeldItem.knockBack, Main.myPlayer);
            }
        }
        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (JiCi2 && JiCi2_JSQ == 0)
            {
                float js = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>() ? 1.6f : .8f;
                JiCi2_JSQ = 1;
                Vector2 velocity = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 10f;
				SoundStyle SoundStyle1 = new SoundStyle("ArknightsMod/Sounds/JiCi2");
				SoundEngine.PlaySound(SoundStyle1);
				Projectile.NewProjectile(Player.GetSource_Death(), Player.Center,
                velocity, ModContent.ProjectileType<JiCi4>(), (int)(Player.HeldItem.damage * js), Player.HeldItem.knockBack, Main.myPlayer);
            }
        }
    }
}