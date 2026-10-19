using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
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
    private static void Bloom(Vector2 at, Vector2 velocity, Color color, Vector2 size, int life, bool contact = false)
    { if (Budget(at)) new T_BlastCasterFluxBloom(at, velocity, color, size, life, contact).Spawn(); }
    private static void Shard(Vector2 at, Vector2 velocity, Color color, float size, int life)
    { if (Budget(at)) new T_BlastCasterFluxShard(at, velocity, color, size, life).Spawn(); }
    internal static void Muzzle(Vector2 at, Vector2 aim, BlastCasterKind kind, int tick)
    {
        Color color = BlastCasterRules.Tint(kind);
        Bloom(at, aim * 1.2f, color, new Vector2(62, 40), 6);
        Vector2 side = new(-aim.Y, aim.X);
        for (int i = -1; i <= 1; i += 2)
            Shard(at + side * i * 10, aim.RotatedBy(i * .17f) * Main.rand.NextFloat(2.5f, 4),
                Color.Lerp(color, BlastCasterRules.FluxAccent(kind), .3f), 9, 12);
    }
    internal static void FluxWake(Vector2 at, Vector2 velocity, BlastCasterKind kind)
    {
        Vector2 axis = velocity.SafeNormalize(Vector2.UnitX), side = new(-axis.Y, axis.X);
        int sign = Main.rand.NextBool() ? -1 : 1;
        float size = BlastCasterRules.SizeMultiplier(kind);
        Shard(at + side * (sign * 4 * size), axis * .8f + side * sign * .65f,
            Color.Lerp(BlastCasterRules.Tint(kind), BlastCasterRules.FluxAccent(kind), Main.rand.NextFloat()), 7 * size, 13);
    }
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
        Color accent = BlastCasterRules.FluxAccent(kind);
        // Two soft volumes echo the reference hit; asymmetric drifting shards
        // and the operator's palette give this branch its own contact signature.
        Bloom(at, -aim * .2f, color, new Vector2(150, 135), 20, true);
        Bloom(at + aim * 3, aim * .35f, accent, new Vector2(105, 120), 15, true);
        Vector2 side = new(-aim.Y, aim.X);
        for (int i = 0; i < 6; i++)
        {
            float sign = i % 2 == 0 ? -1 : 1;
            Vector2 velocity = side * sign * Main.rand.NextFloat(1.7f, 3.6f) - aim * Main.rand.NextFloat(.4f, 2.8f);
            Shard(at + side * sign * 3, velocity, i < 2 ? accent : color, i < 2 ? 13 : 9, Main.rand.Next(13, 21));
        }
        if (kind != BlastCasterKind.Aphrissa) Soot(at, -Vector2.UnitY * .6f, 20);
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
