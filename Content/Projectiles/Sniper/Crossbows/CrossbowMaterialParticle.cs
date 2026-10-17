using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// Short impact flashes and detached dark tears. Sparks, droplets and mist have their
// own particle types, since their motion and lifetime envelopes represent different materials.
public sealed class CrossbowMaterialParticle : Particle
{
    private readonly CrossbowMaterial art;
    private readonly Vector2 dimensions;
    private readonly Color tint;
    private readonly bool bright;
    private readonly float spin, growth, gravity, opacity, turn, damping;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => bright ? BlendState.Additive : BlendState.NonPremultiplied;
    public override void Load() => CrossbowMaterials.Load();
    internal CrossbowMaterialParticle(CrossbowMaterial art, Vector2 position, Vector2 velocity, Color color,
        Vector2 size, int life, float rotation, float spin, float growth, float gravity, bool bright,
        float opacity, float turn, float damping)
    {
        this.art = art; Position = position; Velocity = velocity; tint = color; dimensions = size;
        Lifetime = life; Rotation = rotation; this.spin = spin; this.growth = growth; this.gravity = gravity;
        this.bright = bright; this.opacity = opacity; this.turn = turn; this.damping = damping;
    }
    public override void Update()
    {
        Velocity = Velocity.RotatedBy(turn) * damping;
        Velocity.Y += gravity; Rotation += spin;
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y)) Kill();
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        CrossbowMaterials.Draw(art, Position, dimensions * (1f + growth * t), tint,
            Rotation, MathF.Pow(1f - t, 1.5f) * opacity);
    }
}

internal static class CrossbowParticles
{
    private static ulong tick;
    private static int spawned;
    private static int impactSpawned;
    private static bool Available(Vector2 position, bool impact = false)
    {
        if (Main.dedServ || Main.gameMenu || !float.IsFinite(position.X) || !float.IsFinite(position.Y)
            || ParticleManager.activeParticles.Count >= (impact ? 2000 : 1550)) return false;
        if (position.X < Main.screenPosition.X - 240 || position.X > Main.screenPosition.X + Main.screenWidth + 240
            || position.Y < Main.screenPosition.Y - 240 || position.Y > Main.screenPosition.Y + Main.screenHeight + 240) return false;
        if (tick != Main.GameUpdateCount) { tick = Main.GameUpdateCount; spawned = 0; impactSpawned = 0; }
        // Reserve impact capacity so a flight-rich volley cannot consume all hit feedback.
        return impact ? impactSpawned++ < 220 : spawned++ < 150;
    }
    internal static void Energy(Vector2 position, Vector2 velocity, Color color, Color endColor,
        float diameter, int life, float curl = 0f, bool glint = false, bool impact = false)
    {
        if (Available(position, impact))
            new T_Crossbow_Energy_Particle(position, velocity, color, endColor, diameter, life, curl, glint).Spawn();
    }
    internal static void Gem(Vector2 position, Vector2 velocity, Color color, float scale, bool impact = false)
    {
        if (!Available(position, impact)) return;
        Dust dust = Dust.NewDustPerfect(position, DustID.FireworksRGB, velocity, 60, color, scale);
        dust.noGravity = true;
        dust.noLight = true;
    }
    internal static void Shock(Vector2 position, Color color, Vector2 dimensions, float rotation,
        int life, int delay = 0, bool broken = false, float opacity = .75f)
    {
        if (Available(position, true))
            new T_Crossbow_Shock_Particle(position, color, dimensions, rotation, life, delay, broken, opacity).Spawn();
    }
    internal static void Burst(Vector2 position, Color color, Color edge, Vector2 dimensions, float rotation, int life)
    {
        if (Available(position, true))
            new T_Crossbow_Burst_Particle(position, color, edge, dimensions, rotation, life).Spawn();
    }
    internal static void Spark(Vector2 position, Vector2 velocity, Color color, float length = 16f,
        float width = 3f, int life = 10, float gravity = 0f, bool impact = false)
    {
        if (Available(position, impact))
            new T_Crossbow_Spark_Particle(position, velocity, color, length, width, life, gravity).Spawn();
    }
    internal static void Mote(Vector2 position, Vector2 velocity, Color color, float size, int life,
        T_Crossbow_Mote_Particle.Material material, bool impact = false)
    {
        if (Available(position, impact)) new T_Crossbow_Mote_Particle(position, velocity, color, size, life, material).Spawn();
    }
    internal static void Wisp(Vector2 position, Vector2 velocity, Color color, float size, int life, float opacity = .3f,
        bool impact = false)
    {
        if (Available(position, impact)) new T_Crossbow_Wisp_Particle(position, velocity, color, size, life, opacity).Spawn();
    }
    internal static void Loose(CrossbowMaterial art, Vector2 position, Vector2 velocity, Color color, Vector2 size,
        int life, float rotation, float spin = 0f, float growth = -.45f, float gravity = 0f, bool bright = true,
        float opacity = 1f, float turn = 0f, float damping = .94f, bool impact = false)
    {
        if (Available(position, impact)) new CrossbowMaterialParticle(art, position, velocity, color, size, life,
            rotation, spin, growth, gravity, bright, opacity, turn, damping).Spawn();
    }
}
