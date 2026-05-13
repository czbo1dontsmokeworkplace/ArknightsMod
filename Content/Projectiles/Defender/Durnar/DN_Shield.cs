using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Durnar;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            Projectile.width =  30; // ?�������?�����
            Projectile.height = 36; // ?�������?��?�
            Projectile.friendly = true; // ?������?��?���
            Projectile.penetrate = -1; // ?�������?�?
            Projectile.tileCollide = false; // ?���?����?��?
            Projectile.usesLocalNPCImmunity = true; // ?��?�����?
            Projectile.ownerHitCheck = true; // ?��?�����?���������?�����??�?�����?�?��?����?�?
            Projectile.DamageType = DamageClass.MeleeNoSpeed; // ?����?��??����
            Projectile.ignoreWater = true;
            Projectile.localNPCHitCooldown = (int)attackMaxTime+1;
        }
        private ProjMode projMode = ProjMode.Move;
		public override void AI()
        {
	        Projectile.damage = item.damage;
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
                case ProjMode.Attack:
                Attack();
                break;
            }
        }

        public override bool? CanDamage()
        {
	        if (projMode == ProjMode.Attack)
		        return true;
	        return false;
        }

        private float attackRad;
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
        private bool press = false;
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
                targetOffsetDeg = MathHelper.Lerp(-20f, 50f, progress);
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
                var modPlayer = player.GetModPlayer<DNProj_Player>();
                if (modPlayer.ShieldAttackMode &&PlayerInput.MouseInfo.LeftButton == ButtonState.Pressed && !press) {
	                press = true;
	                attackRad = MathF.Atan2((Main.MouseWorld - player.MountedCenter).Y,(Main.MouseWorld - player.MountedCenter).X);
					projMode = ProjMode.Attack;
					player.direction = (Main.MouseWorld - player.MountedCenter).X >=0? 1:-1;
					attackTime = 0;
					CDTime = CDTimeMax;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
	        var modPlayer = player.GetModPlayer<WeaponPlayer>();
	        if (modPlayer.SkillActive)
		        modifiers.SourceDamage *= 1.8f;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
	        var modPlayer = player.GetModPlayer<WeaponPlayer>();
	        if (modPlayer.SkillActive)
		        modifiers.SourceDamage *= 1.8f;
        }

        private Vector2 AttackLength = new Vector2(10, 0);
        private float attackTime;
        private float attackMaxTime = 30;

        private float CDTime;

        private float CDTimeMax=10;
        public void Attack() {
	        float progress = attackTime / attackMaxTime;
	        float prog2 =1;
	        Projectile.rotation = attackRad - MathHelper.Pi/2 + MathHelper.Pi/2 * player.direction;
	        float mineRad = Projectile.rotation - MathHelper.Pi;
	        float accelProgress = progress * progress;
	        float Length = MathHelper.Lerp(-10, 20, accelProgress);
	        if(progress<=1f)
		        attackTime++;
	        else{
		        CDTime--;
		        prog2 = CDTime/CDTimeMax;
		        Length = MathHelper.Lerp(0, Length, prog2);
		        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None,Projectile.rotation);
		        Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.None,Projectile.rotation);
	        }
	        if (progress < 0.4) {
		        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None,mineRad);
		        Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.None, mineRad);
	        }
	        else if(progress <=1f){
		        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None,Projectile.rotation);
		        Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.None,Projectile.rotation);
	        }
	        if(prog2<0)
	        {
		        press = false;
		        projMode = ProjMode.Move;
	        }
	        Vector2 fix = new Vector2(1, 0).RotatedBy(attackRad);
	        Projectile.Center += fix * Length;
        }
        public void Defender()
        {
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