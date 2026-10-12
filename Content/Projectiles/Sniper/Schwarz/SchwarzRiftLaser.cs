using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Schwarz;

/// <summary>
/// 敌人命中点是整道光束的中点。随机斜率决定上方起点，末端与起点关于命中点对称。
/// </summary>
public sealed class SchwarzRiftLaser : ModProjectile
{
    private const int Lifetime = 24;
    private const int Windup = 7;
    private const float SkyHeight = 3000f;
    private const float BeamLength = SkyHeight * 2f;
    private const string Assets = "ArknightsMod/Content/Projectiles/Sniper/Schwarz/Assets/";
    private bool Rose => Projectile.ai[1] == 1f;
    private Color Violet => Rose ? new Color(255, 85, 159) : new Color(143, 90, 205);
    private Color Pale => Rose ? new Color(255, 225, 240) : new Color(224, 206, 255);
    private int Age => Lifetime - Projectile.timeLeft;
    private Vector2 Up => -Vector2.UnitY.RotatedBy(Projectile.ai[0]);
    private Vector2 Start => Projectile.Center + Up * SkyHeight;
    private Vector2 End => Projectile.Center - Up * SkyHeight;
    private Vector2 Down => -Up;
    private float Sweep => MathHelper.Clamp((Age - Windup + 1f) / 3f, 0f, 1f);
    private Vector2 VisibleEnd => Vector2.Lerp(Start, End, Sweep);

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6400;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age >= Windup && Age <= Windup + 3 ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Age < Windup || Age > Windup + 3) return false;
        float collisionPoint = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Start, VisibleEnd, 38f, ref collisionPoint);
    }

    public override void AI()
    {
        if (Main.dedServ) return;
        if (Age == 0)
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = .28f, Pitch = -.35f },
                Projectile.Center);
            SpawnMotes(7, .35f);
        }
        if (Age == Windup + 1)
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = .62f, Pitch = -.18f },
                Projectile.Center);
            SpawnMotes(21, 1f);
            OperatorFlightVfx.Ring(Projectile.Center, Violet, Rose ? 185f : 155f, 24);
            Vector2 side = Down.RotatedBy(MathHelper.PiOver2);
            for (int j = -4; j <= 4; j++)
                for (int sign = -1; sign <= 1; sign += 2)
                    OperatorFlightVfx.Spark(Projectile.Center + Down * (j * 34f) + side * sign * 16f,
                        Down * Main.rand.NextFloat(8f, 15f) + side * sign * 1.5f, j % 2 == 0 ? Pale : Violet, 32f, 19, 3f);
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, velocity,
                    100, Violet, Main.rand.NextFloat(.75f, 1.2f));
                dust.noGravity = true;
            }
        }
        if (Age >= Windup && Age < Windup + 5)
            Lighting.AddLight(Projectile.Center, Violet.ToVector3() * .5f);
    }

    private void SpawnMotes(int count, float power)
    {
        if (ParticleManager.activeParticles.Count >= 1400) return;
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 1450; i++)
        {
            float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-.18f, .18f);
            Vector2 direction = angle.ToRotationVector2();
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(6f, 28f);
            new SchwarzRiftMote(position, direction * Main.rand.NextFloat(1.2f, 5f) * power,
                Main.rand.Next(12, 21), Main.rand.NextFloat(15f, 34f) * power,
                i % 4 == 0 ? Pale : Violet).Spawn();
        }
    }

    private static void DrawSized(Texture2D texture, Vector2 center, Vector2 size,
        Color tint, float rotation = 0f)
        => Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);

    private static void BeginAdditive()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    private static void EndAdditive()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullNone, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float telegraph = MathHelper.Clamp(Age / (float)Windup, 0f, 1f);
        float active = Age >= Windup ? MathF.Pow(1f - (Age - Windup) / (float)(Lifetime - Windup), 1.2f) : 0f;
        float opacity = Age < Windup ? .18f + .25f * telegraph : active;
        float width = Age < Windup ? 7f : 53f * active;
        Texture2D beam = ModContent.Request<Texture2D>(Assets + "SchwarzRiftBeam").Value;
        Texture2D end = ModContent.Request<Texture2D>(Assets + "SchwarzRiftEnd").Value;
        Texture2D impact = ModContent.Request<Texture2D>(Assets + "SchwarzRiftImpact").Value;
        Texture2D core = ModContent.Request<Texture2D>(Assets + "SchwarzBlackCore").Value;
        float visibleLength = Age < Windup ? BeamLength : BeamLength * Sweep;
        Vector2 midpoint = Age < Windup ? Projectile.Center : (Start + VisibleEnd) * .5f;
        float rotation = Down.ToRotation() - MathHelper.PiOver2;

        // Fran 裂隙的结构：宽的有色边缘、窄的黑色主体、末端高精度爆发纹理。
        // (X,Y) -> 命中点 -> (-X,-Y)：完整长度六千像素，命中点恰为中点。
        BeginAdditive();
        DrawSized(beam, midpoint, new Vector2(width * 1.45f, visibleLength),
            Violet * (opacity * .62f), rotation);
        EndAdditive();
        DrawSized(beam, midpoint, new Vector2(width * .78f, visibleLength),
            (Rose ? new Color(61, 8, 36) : Color.Black) * (opacity * .92f), rotation);
        if (Age >= Windup + 1)
            DrawSized(core, Projectile.Center, new Vector2(118f * active),
                Color.Black * (opacity * .72f));
        BeginAdditive();
        if (Age >= Windup)
        {
            Vector2 side = Down.RotatedBy(MathHelper.PiOver2);
            // Two sharp coloured edges flank the opaque core. The moving front comes from the sky.
            for (int sign = -1; sign <= 1; sign += 2)
                DrawSized(beam, midpoint + side * sign * width * .39f, new Vector2(3f * active, visibleLength), Pale * (active * .7f), rotation);
            DrawSized(end, VisibleEnd, new Vector2(width * 1.5f, 90f), Pale * active, rotation);
        }
        DrawSized(end, Projectile.Center, new Vector2(82f * opacity, 50f * opacity),
            Pale * (opacity * .7f), rotation);
        DrawSized(impact, Projectile.Center, new Vector2(165f * opacity, 112f * opacity),
            Violet * (opacity * .64f), Projectile.ai[0]);
        EndAdditive();
        return false;
    }
}

public sealed class SchwarzRiftMote : Particle
{
    private readonly Color initialColor;
    private readonly float width;
    public override string TexturePath =>
        "ArknightsMod/Content/Projectiles/Sniper/Schwarz/Assets/SchwarzSmear";
    public override BlendState DrawBlendState => BlendState.Additive;

    public SchwarzRiftMote(Vector2 position, Vector2 velocity, int lifetime,
        float width, Color color)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        this.width = width;
        initialColor = Color = color;
        Rotation = velocity.ToRotation();
    }

    public override void Update()
    {
        Velocity *= .9f;
        Color = initialColor * MathF.Pow(1f - LifetimeRatio, 1.5f);
    }

    public override void Draw()
        => Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(width / Texture.Width, 9f / Texture.Height),
            SpriteEffects.None, 0f);
}
