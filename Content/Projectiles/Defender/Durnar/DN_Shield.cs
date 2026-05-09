using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Durnar;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Durnar
{
    public class DN_Shield : ModProjectile
    {
        Player player => Main.player[Projectile.owner];
        Item item => player.HeldItem;
        private Texture2D ShieldTex {get => TextureAssets.Projectile[ModContent.ProjectileType<DN_Shield>()].Value;}
        private float Length {get => MathF.Sqrt(MathF.Pow(ShieldTex.Width,2) + MathF.Pow(ShieldTex.Width,2));}
        public override void SetDefaults()
        {
            Projectile.width = 10; // ?�������?�����
            Projectile.height = 10; // ?�������?��?�
            Projectile.friendly = true; // ?������?��?���
            Projectile.penetrate = -1; // ?�������?�?
            Projectile.tileCollide = false; // ?���?����?��?
            Projectile.usesLocalNPCImmunity = true; // ?��?�����?
            Projectile.ownerHitCheck = true; // ?��?�����?���������?�����??�?�����?�?��?����?�?
            Projectile.DamageType = DamageClass.MeleeNoSpeed; // ?����?��??����
            Projectile.ignoreWater = true;
            Projectile.localNPCHitCooldown = 1;
        }
        private ProjMode projMode = ProjMode.Move;
		public override void AI()
        {
            if (player.dead || !player.active || item.type != ModContent.ItemType<DN_Weapon>()) Projectile.Kill();
            Projectile.timeLeft = 2;
            switch(projMode)
            {
                case ProjMode.Move:
                Move();
                break;
                case ProjMode.Defender:
                Defender();
                break;
            }
        }
		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Draw_Shield(sb);
            sb.End();
            sb.Begin();
            return false;
        }
        private float walkPhase;
        private float armOffsetDeg;
        public void Move() {
            float vx = Math.Abs(player.velocity.X);
            bool isAirborne = Math.Abs(player.velocity.Y) > 0.01f;
            float targetOffsetDeg;
            if (isAirborne) {
                targetOffsetDeg = 20f; // 飞起来后固定
                walkPhase = 0f;
            }
            else if (vx > 0.1f) {
                walkPhase += 0.12f + vx * 0.04f;
                float progress = (MathF.Sin(walkPhase) + 1f) * 0.5f;
                targetOffsetDeg = MathHelper.Lerp(-10f, 25f, progress);
            }
            else {
                walkPhase = 0f;
                targetOffsetDeg = 0f; // 落地停下回正
            }
            armOffsetDeg = MathHelper.ToRadians(targetOffsetDeg);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armOffsetDeg * -player.direction);
            Projectile.rotation = armOffsetDeg * -player.direction;
            Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armOffsetDeg * -player.direction);
            if(Main.myPlayer == player.whoAmI)
            {
                if(Main.mouseRight&&player.itemTime==0)
                {
                    projMode = ProjMode.Defender;
                }
            }
        }
        public void Defender()
        {
            player.itemTime = player.itemAnimation = Projectile.timeLeft = 2;
            if(Main.myPlayer == player.whoAmI)
            {
                if(!Main.mouseRight)
                {
                    projMode = ProjMode.Move;
                }
            }
            player.direction = (Main.MouseWorld - player.MountedCenter).X >=0? 1:-1;
            float rotation = MathF.Atan2((Main.MouseWorld - player.MountedCenter).Y,(Main.MouseWorld - player.MountedCenter).X);
            Projectile.rotation = rotation - MathHelper.Pi/2 + MathHelper.Pi/2 * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation);
            Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation) + new Vector2(8,0).RotatedBy(Projectile.rotation) * player.direction;
        }
        private float TexWidth{get => ShieldTex.Width;}
        private float TexHeight{get => ShieldTex.Height;}
        // public Rectangle ShieldRect(Vector2 pos)
        // {
        //     int x = (int)(pos.X - TexWidth/2f);
        //     int y = (int)(pos.Y - TexHeight/2f);
        //     Rectangle rect = new Rectangle(x,y,(int)TexWidth,(int)TexHeight);
        //     return rect;
        // }
        public void Draw_Shield(SpriteBatch sb)
        {
            SpriteEffects spriteEffects = player.direction == 1? SpriteEffects.None  : SpriteEffects.FlipHorizontally;
            sb.Draw(ShieldTex,Projectile.Center - Main.screenPosition,null,Color.White,Projectile.rotation,new Vector2(TexWidth/2,TexHeight/2),1f,spriteEffects,1);
        }
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
}