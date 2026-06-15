using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Cardigan
{
	// 卡缇盾牌推击弹幕：从玩家身前猛力推出，高速后迅速减速，营造用力推盾的手感。
	public class CardiganShieldBash : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Cardigan/CardiganShield_protile";

		private const float Deceleration = 0.78f; // 每帧速度乘数，越小减速越猛
		private const float StopThreshold = 1.2f;  // 速度低于此值时销毁弹幕

		public override void SetDefaults() {
			Projectile.width = 44;
			Projectile.height = 44;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.timeLeft = 30;
			Projectile.scale = 1.1f;
		}

		public override void OnSpawn(IEntitySource source) {
			SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (player.dead || !player.active) { Projectile.Kill(); return; }

			// 锁定玩家朝向并维持手持动画
			player.ChangeDir(Projectile.velocity.X >= 0 ? 1 : -1);
			player.heldProj = Projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;

			// 减速：模拟猛力推出后摩擦制动
			Projectile.velocity *= Deceleration;
			if (Projectile.velocity.Length() < StopThreshold)
				Projectile.Kill();

			// 弹幕跟随玩家中心偏移（始终贴近玩家手臂前方）
			float ang = Projectile.velocity.ToRotation();
			Projectile.Center = player.Center + ang.ToRotationVector2() * 28f;

			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ang - MathHelper.PiOver2);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			float ang = Projectile.velocity.ToRotation();
			// 面朝左时垂直翻转，保持盾牌正面朝外
			bool faceLeft = Math.Cos(ang) < 0;
			SpriteEffects fx = faceLeft ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 origin = tex.Size() / 2f;
			Vector2 drawPos = Projectile.Center - Main.screenPosition;
			Main.spriteBatch.Draw(tex, drawPos, null, lightColor, ang, origin, Projectile.scale, fx, 0f);
			return false;
		}

		public override bool ShouldUpdatePosition() => false;
	}
}
