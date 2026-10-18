using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

internal static class BlastCasterVisuals
{
    private static ulong budgetTick;
    private static int spent;
    internal static bool Visible(Vector2 at)
    {
        if (Main.dedServ || Main.gameMenu || !float.IsFinite(at.X) || !float.IsFinite(at.Y)) return false;
        Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) / Main.GameViewMatrix.Zoom / 2f;
        Vector2 center = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
        return Math.Abs(at.X - center.X) < half.X + 140 && Math.Abs(at.Y - center.Y) < half.Y + 140;
    }
    private static bool Budget(Vector2 at)
    {
        if (!Visible(at)) return false;
        if (budgetTick != Main.GameUpdateCount) { budgetTick = Main.GameUpdateCount; spent = 0; }
        return spent++ < 48 && ParticleManager.activeParticles.Count < 2200;
    }
    private static void Ember(Vector2 at, Vector2 velocity, Color color, float size, int life = 16, bool optical = false)
    { if (Budget(at)) new T_BlastCasterEmber(at, velocity, color, Math.Clamp(size, 2, 24), Math.Clamp(life, 6, 28), optical).Spawn(); }
    private static void Soot(Vector2 at, Vector2 velocity, float size)
    { if (Budget(at)) new T_BlastCasterSoot(at, velocity, Math.Clamp(size, 8, 38)).Spawn(); }
    internal static void Muzzle(Vector2 at, Vector2 aim, BlastCasterKind kind, int tick)
    {
        Color color = kind == BlastCasterKind.Aphrissa ? BlastCasterRules.Spectrum(tick * .008f) : BlastCasterRules.Tint(kind);
        if (kind == BlastCasterKind.Aphrissa)
        { Ember(at, aim.RotatedByRandom(.14f) * 2.5f, color, 6, 9, true); return; }
        Vector2 side = new(-aim.Y, aim.X);
        for (int i = -1; i <= 1; i += 2)
            Ember(at + side * i * 2, aim.RotatedBy(i * .18f) * Main.rand.NextFloat(3.3f, 4.7f),
                i < 0 ? Color.Lerp(color, new Color(255, 199, 95), .45f) : color, 12, 13);
    }
    internal static void FlameWake(Projectile p, BlastCasterKind kind)
    {
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitX), side = new(-axis.Y, axis.X);
        float spread = MathHelper.Lerp(3, 13, Math.Clamp(p.localAI[0] / 72f, 0, 1));
        Color tint = BlastCasterRules.Tint(kind);
        for (int i = -1; i <= 1; i += 2)
            Ember(p.Center + side * i * spread, p.velocity * .17f + side * i * .8f - Vector2.UnitY * .5f,
                i < 0 ? tint : Color.Lerp(tint, new Color(255, 179, 73), .35f), kind == BlastCasterKind.Ifrit ? 14 : 12, 16);
        if ((int)p.localAI[0] > 45 && (int)p.localAI[0] % 18 == 0) Soot(p.Center, p.velocity * .08f, 20);
    }
    internal static void OpticalWake(Vector2 at, Vector2 velocity, float phase)
        => Ember(at + Main.rand.NextVector2Circular(4, 4), velocity * .08f + Main.rand.NextVector2Circular(.6f, .6f),
            BlastCasterRules.Spectrum(phase), Main.rand.NextFloat(5, 9), 12, true);
    internal static void Hit(Projectile p, NPC target, Vector2 at, BlastCasterKind kind)
    {
        if (!Visible(at)) return;
        BlastCasterImpactClock clock = target.GetGlobalNPC<BlastCasterImpactClock>();
        if (Main.GameUpdateCount < clock.NextFlash) return;
        clock.NextFlash = Main.GameUpdateCount + 8;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        ImpactParticles(at, aim, kind);
    }
    internal static void ImpactParticles(Vector2 at, Vector2 aim, BlastCasterKind kind)
    {
        Color color = BlastCasterRules.Tint(kind);
        bool optical = kind == BlastCasterKind.Aphrissa;
        for (int i = 0; i < (optical ? 4 : 6); i++)
            Ember(at, (-aim * Main.rand.NextFloat(1, optical ? 3 : 4) + Main.rand.NextVector2Circular(2, optical ? 2 : 3)) - Vector2.UnitY,
                i < (optical ? 1 : 2) ? (optical ? new Color(255, 221, 146) : Color.Lerp(color, new Color(255, 221, 146), .7f)) : color,
                optical ? 6 : 14, optical ? 16 : 19, optical);
        if (kind != BlastCasterKind.Aphrissa)
        {
            Vector2 side = new(-aim.Y, aim.X);
            for (int i = -1; i <= 1; i += 2)
                Ember(at + side * i * 4, side * i * 2.2f - aim * 1.3f, color, 18, 15);
            Soot(at, -Vector2.UnitY * 1.5f, 26);
            if (Budget(at))
            {
                Dust d = Dust.NewDustPerfect(at, kind == BlastCasterKind.Corroserum ? DustID.CursedTorch : DustID.Torch,
                    -aim + Main.rand.NextVector2Circular(1, 1), 80, default, .85f);
                d.noGravity = true;
            }
        }
        else if (Budget(at))
            CasterSparkles.Spawn(at, -aim * .6f, color, .5f, 10, aim.ToRotation());
    }
    internal static void Skill(Vector2 at, BlastCasterKind kind)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 axis = (MathHelper.TwoPi * i / 8).ToRotationVector2();
            Ember(at + axis * 10, axis * 2.5f, BlastCasterRules.Tint(kind), 8, 18, kind == BlastCasterKind.Aphrissa);
        }
    }
    internal static void HeatBurst(Vector2 at, bool burst)
    {
        for (int i = 0; i < (burst ? 12 : 5); i++)
        {
            Vector2 axis = (MathHelper.TwoPi * i / (burst ? 12 : 5)).ToRotationVector2();
            Ember(at + axis * 6, axis * (burst ? 4 : 1), new Color(255, 140, 42), burst ? 18 : 10, 19);
        }
        Soot(at, -Vector2.UnitY, 32);
    }
    internal static void Scorch(Vector2 at)
    {
        Vector2 seed = at + Main.rand.NextVector2Circular(30, 15);
        Ember(seed, new Vector2(Main.rand.NextFloat(-.5f, .5f), -1.7f), new Color(255, 98, 24), 14, 19);
    }
    internal static void RelayFlash(Vector2 at, Vector2 aim)
    {
        for (int i = -1; i <= 1; i++) Ember(at, aim.RotatedBy(i * .8f) * 2.2f, new Color(97, 242, 210), 8, 10, true);
    }
    internal static void RelayIdle(Vector2 at, float rotation)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 axis = (rotation + MathHelper.PiOver2 + i * MathHelper.Pi).ToRotationVector2();
            Ember(at + axis * 20, axis.RotatedBy(MathHelper.PiOver2) * .6f, new Color(84, 220, 200), 5, 14, true);
        }
    }
    internal static void PacketEnd(Vector2 at, Vector2 velocity)
        => Ember(at, -velocity.SafeNormalize(Vector2.UnitX) * .6f, new Color(115, 221, 188), 4, 8, true);
    internal static void DrawRelay(Vector2 at, float rotation, float opacity)
    {
        if (!Visible(at)) return;
        Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/ChargedSniper/BreachRing").Value;
        Texture2D core = ModContent.Request<Texture2D>("ArknightsMod/Common/Particle/DefaultParticle").Value;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(ring, at - Main.screenPosition, null, new Color(69, 222, 199) * (.65f * opacity),
            rotation, ring.Size() / 2, new Vector2(42, 32) / ring.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(core, at - Main.screenPosition, null, new Color(165, 245, 202) * (.65f * opacity),
            rotation, core.Size() / 2, new Vector2(17, 10) / core.Size(), SpriteEffects.None, 0);
        RestoreBatch();
    }
    internal static void RestoreBatch()
    {
        Main.spriteBatch.End();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
    }
}

public sealed class BlastCasterImpactClock : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal ulong NextFlash;
}
