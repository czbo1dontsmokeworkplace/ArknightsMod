using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

public sealed class MidnightVeil : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.DamageType = DamageClass.Melee; Projectile.friendly = true;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        float speed = Math.Min(22, Projectile.velocity.Length() * MathF.Pow(1.012f, 1f / Projectile.MaxUpdates));
        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        return projHitbox.Intersects(targetHitbox) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, 24, ref collision);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX),
                ModContent.ProjectileType<MidnightVeilImpact>(), 0, 0, Projectile.owner);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1, (180 - Projectile.timeLeft + 1) / 10f) * Math.Min(1, Projectile.timeLeft / 24f);
        MidnightVeilMaterial.Draw(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 78,
            new Vector2(224, 70), Projectile.rotation, fade, 0, Main.GlobalTimeWrappedHourly + Projectile.identity * .17f);
        return false;
    }
}

public sealed class MidnightVeilImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 20;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = .28f, Pitch = -.45f, MaxInstances = 3 }, Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float progress = 1 - Projectile.timeLeft / 20f, size = Projectile.ai[1] > 0 ? Projectile.ai[1] : 1;
        MidnightVeilMaterial.Draw(Projectile.Center, new Vector2(92, 76) * size, Projectile.velocity.ToRotation(),
            MathF.Pow(1 - progress, 1.35f), 1, progress);
        return false;
    }
}

// Opaque ink and a narrow cool rim are drawn with straight-alpha blending, not additive black.
public sealed class MidnightVeilMaterial : ModSystem
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    public override void Unload()
    {
        Effect old = material; material = null;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
    internal static void Draw(Vector2 center, Vector2 size, float rotation, float opacity, int mode, float time)
    {
        if (Main.dedServ || opacity <= 0) return;
        material ??= new Effect(Main.instance.GraphicsDevice,
            ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().GetFileBytes("Assets/Effects/MidnightVeil.fxc"));
        Vector2 axis = rotation.ToRotationVector2() * size.X * .5f;
        Vector2 side = (rotation + MathHelper.PiOver2).ToRotationVector2() * size.Y * .5f;
        Vector2 at = center - Main.screenPosition;
        var a = new VertexPositionColorTexture(new Vector3(at - axis - side, 0), Color.White, Vector2.Zero);
        var b = new VertexPositionColorTexture(new Vector3(at + axis - side, 0), Color.White, Vector2.UnitX);
        var c = new VertexPositionColorTexture(new Vector3(at + axis + side, 0), Color.White, Vector2.One);
        var d = new VertexPositionColorTexture(new Vector3(at - axis + side, 0), Color.White, Vector2.UnitY);
        quad[0] = a; quad[1] = b; quad[2] = c; quad[3] = a; quad[4] = c; quad[5] = d;
        var device = Main.instance.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix *
                Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
            material.Parameters["Time"].SetValue(time); material.Parameters["Opacity"].SetValue(opacity);
            material.CurrentTechnique = material.Techniques[mode == 0 ? "NightFlight" : "NightImpact"];
            material.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
