using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Cardigan;
using ArknightsMod.Content.Items.Weapons.Defender.Cuora;
using ArknightsMod.Content.Items.Weapons.Defender.Durnar;
using ArknightsMod.Content.Projectiles.Defender.Cuora;
using ArknightsMod.Content.Projectiles.Defender.Durnar;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RuneSKill.Content.NeedTool;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Caridgan;

public class Cardi_Shield : ModProjectile
{
	Player player => Main.player[Projectile.owner];
    Item item => player.HeldItem;

    private Texture2D ShieldTex => TextureAssets.Projectile[ModContent.ProjectileType<Cardi_Shield>()].Value;
    private float Length {get => MathF.Sqrt(MathF.Pow(ShieldTex.Width,2) + MathF.Pow(ShieldTex.Width,2));}
    public override void SetDefaults()
    {
        Projectile.width =  60; // ?�������?�����
        Projectile.height = 36; // ?�������?��?�
        Projectile.friendly = true; // ?������?��?���
        Projectile.penetrate = -1; // ?�������?�?
        Projectile.tileCollide = false; // ?���?����?��?
        Projectile.usesLocalNPCImmunity = true; // ?��?�����?
        Projectile.ownerHitCheck = true; // ?��?�����?���������?�����??�?�����?�?��?����?�?
        Projectile.DamageType = DamageClass.MeleeNoSpeed; // ?����?��??����
        Projectile.ignoreWater = true;
    }
    private ProjMode projMode = ProjMode.Move;
	public override void AI()
    {
        Projectile.damage = item.damage;
        if (player.dead || !player.active || item.type != ModContent.ItemType<CardiWeapon>())
	        Projectile.Kill();
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

    public override bool? CanDamage()
    {
        return false;
    }

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
	        var modPlayer = player.GetModPlayer<CardiProj_Player>();
            if(Main.mouseRight&&player.itemTime==0)
            {
                projMode = ProjMode.Defender;
            }
        }
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
        Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation) + new Vector2(20,0).RotatedBy(Projectile.rotation) * player.direction;
    }

    private float TexWidth { get =>ShieldTex.Width; }

	private float TexHeight { get =>ShieldTex.Height; }
    public void Draw_Shield(SpriteBatch sb)
    {
		Vector2 Center =  Projectile.Center - Main.screenPosition;
		Vector2 halfWidth = new Vector2(TexWidth / 2, 0).RotatedBy(Projectile.rotation);
		Vector2 halfHeight = new Vector2(0,TexHeight / 2).RotatedBy(Projectile.rotation);
		if (projMode == ProjMode.Defender)
			halfWidth *= 0.8f;
		Vector2[] pos = [
			Center + halfHeight + halfWidth * player.direction,// 右上角
			Center + halfHeight - halfWidth * player.direction,// 左上角
			Center - halfHeight - halfWidth * player.direction,// 左下角
			Center - halfHeight + halfWidth * player.direction// 右下角
		];
		List<Vertex> vertices = new List<Vertex>(6);
		for(int i =0;i<6;i++)
			vertices.Add(default);
		{
			vertices[0] = new Vertex(pos[1],new Vector3(0,1,1), Color.White);
			vertices[1] = vertices[5] = new Vertex(pos[0],new Vector3(1,1,1), Color.White);
			vertices[2] = vertices[4] = new Vertex(pos[2],new Vector3(0,0,1), Color.White);
			vertices[3] = new Vertex(pos[3],new Vector3(1,0,1), Color.White);
		}
		Main.graphics.GraphicsDevice.Textures[0] = ShieldTex;
		if (vertices.Count > 4)
			Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count / 3);
    }
	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
}