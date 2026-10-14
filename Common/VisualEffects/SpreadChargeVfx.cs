using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.VisualEffects;

// Visual reference: ArcNovaDiffuserHoldout (layered core, three orbiting lights)
// and OntologicalDespoilerHoldout (inward motes, dark core, staged pulses).
// Credit: Azafure, LLC, https://github.com/CalamityTeam/CalamityModPublic
// Independently implemented with this mod's particles/assets; no Calamity dependency.
internal sealed class SpreadChargeVfx
{
    internal Vector2 Center { get; private set; }
    internal float Phase { get; private set; }
    private Projectile focus;
    private int identity, ticks, stage;
    internal bool Active => focus != null && focus.active && focus.identity == identity;
    internal static float Growth(float charge) => 1f - MathF.Pow(1f - MathHelper.Clamp(charge, 0f, 1f), 3f);
    internal static float Speed(float charge) => .22f + 2.78f * MathF.Pow(1f - MathHelper.Clamp(charge, 0f, 1f), 2f);
    internal static float ArtScale(SpreadArt art) => art switch {
        SpreadArt.Ember => .72f, SpreadArt.Crystal => .9f, SpreadArt.Rupture => 1f, _ => 1.12f };
    internal static Vector2 AimPoint(Vector2 center, Vector2 aim, float forward, float side)
        => center + aim * forward + new Vector2(-aim.Y, aim.X) * side;

    internal void Tick(Projectile projectile, Vector2 muzzle, float charge, SpreadArt art, Vector2 direction)
    {
        focus = projectile; identity = projectile.identity; Center = muzzle;
        Phase += Speed(charge) / 60f;
        ticks++;
        Vector2 aim = direction.SafeNormalize(Vector2.UnitX);
        int nextStage = charge >= 1f ? 2 : charge >= .45f ? 1 : 0;
        if (nextStage > stage)
        {
            stage = nextStage;
            Pulse(muzzle, aim, art, false, stage == 2 ? 1f : .65f);
        }
        if (ticks % (charge < 1f ? 3 : 5) != 0) return;
        for (int i = 0; i < 2; i++)
        {
            if (!SpreadVfxBudget.Take(muzzle, 150f)) break;
            float angle = ticks * 2.399963f + i * MathHelper.Pi + Main.rand.NextFloat(-.14f, .14f);
            float radius = (60f + 18f * Growth(charge)) * ArtScale(art);
            Vector2 start = AimPoint(muzzle, aim, MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .75f);
            new SpreadAbsorptionParticle(this, art, start, aim, charge).Spawn();
        }
    }

    internal static void Pulse(Vector2 center, Vector2 aim, SpreadArt art, bool release, float strength = 1f)
    {
        if (SpreadVfxBudget.Take(center))
            new SpreadChargePulse(center, aim, SpreadArtsVfx.Edge(art), ArtScale(art) * strength, release).Spawn();
        if (release && SpreadVfxBudget.Take(center))
            new SpreadChargeFlash(center, aim, SpreadArtsVfx.Body(art), ArtScale(art)).Spawn();
    }
}

// WORLD-space integration: changing the aim point never translates particles/history.
public sealed class SpreadAbsorptionParticle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly SpreadChargeVfx anchor;
    private readonly SpreadArt art;
    private readonly Vector2 origin;
    private readonly Vector2[] history = new Vector2[6];
    private Vector2 motion;
    private readonly float maxSpeed;
    private int orphanTicks;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;

    internal SpreadAbsorptionParticle(SpreadChargeVfx anchor, SpreadArt art, Vector2 start, Vector2 aim, float charge)
    {
        this.anchor = anchor; this.art = art; Position = origin = start;
        Vector2 inward = (anchor.Center - start).SafeNormalize(aim);
        motion = inward * 2f + new Vector2(-inward.Y, inward.X) * 1.8f;
        maxSpeed = MathHelper.Lerp(12f, 7f, SpreadChargeVfx.Growth(charge));
        Lifetime = 34;
        Array.Fill(history, start);
    }
    public override void Update()
    {
        for (int i = history.Length - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = Position;
        Vector2 delta = anchor.Center - Position;
        bool lost = !anchor.Active || delta.LengthSquared() > 180f * 180f;
        if (lost || orphanTicks > 0)
        {
            orphanTicks++;
            motion *= .72f;
            if (orphanTicks >= 6) { Kill(); Color = Color.Transparent; return; }
        }
        else
        {
            float distance = delta.Length();
            if (distance < 6f) { Kill(); Color = Color.Transparent; return; }
            Vector2 desired = delta / distance * Math.Min(maxSpeed, distance * .38f);
            motion = Vector2.Lerp(motion, desired, .36f);
            if (motion.LengthSquared() > maxSpeed * maxSpeed) motion = motion.SafeNormalize(Vector2.Zero) * maxSpeed;
        }
        Position += motion;
        if (Vector2.DistanceSquared(origin, Position) > 170f * 170f) { Kill(); Color = Color.Transparent; return; }
        Rotation = motion.ToRotation();
        float fade = MathHelper.Clamp(Time / 3f, 0f, 1f) * MathHelper.Clamp((Lifetime - Time) / 6f, 0f, 1f)
            * (1f - orphanTicks / 6f);
        Color = SpreadArtsVfx.Edge(art) * fade;
    }
    public override void Draw()
    {
        if (Time >= Lifetime || !SpreadVfxBudget.Visible(Position)) return;
        for (int i = 0; i < history.Length - 1; i++)
            SpreadVfxDetail.Segment(i == 0 ? Position : history[i - 1], history[i],
                OperatorFlightVfx.Add(Color, (1f - i / (float)history.Length) * .55f), 2.2f - i * .32f);
        float size = 8f * MathHelper.Clamp((Lifetime - Time) / 6f, 0f, 1f);
        if (art == SpreadArt.Crystal)
            SpreadVfxDetail.Facet(Position, size * .5f, size * 1.3f, Rotation, Color, .9f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, OperatorFlightVfx.Add(Color, .85f), Rotation,
            Texture.Size() * .5f, new Vector2(size * 2.4f, size * (art == SpreadArt.Rupture ? .75f : 1.4f)) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

// Pulses and release flashes retain the event's world position and aim.
public sealed class SpreadChargePulse : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float strength;
    private readonly bool release;
    private readonly Color tint;
    public override string TexturePath => OperatorFlightVfx.Assets + "RuptureRing";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal SpreadChargePulse(Vector2 center, Vector2 aim, Color tint, float strength, bool release)
    {
        Position = center; Rotation = aim.ToRotation(); this.tint = tint;
        this.strength = strength; this.release = release; Lifetime = release ? 18 : 13;
    }
    public override void Update() => Color = tint * MathF.Pow(1f - LifetimeRatio, 1.5f);
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 140f)) return;
        float opening = 1f - MathF.Pow(1f - LifetimeRatio, 4f);
        float diameter = MathHelper.Lerp(16f, release ? 100f : 76f, opening) * strength;
        Vector2 aim = Rotation.ToRotationVector2();
        for (int i = 0; i < (release ? 2 : 1); i++)
        {
            Vector2 point = Position + aim * (release ? opening * (18f + i * 22f) : 0f);
            Vector2 size = new Vector2(diameter * .38f, diameter) * (1f - i * .22f);
            Main.spriteBatch.Draw(Texture, point - Main.screenPosition, null, Color * (1f - i * .35f), Rotation,
                Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0f);
        }
    }
}

public sealed class SpreadChargeFlash : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color tint;
    private readonly float size;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal SpreadChargeFlash(Vector2 center, Vector2 aim, Color tint, float strength)
    {
        Position = center; Rotation = aim.ToRotation(); this.tint = tint;
        size = strength * 78f; Lifetime = 10;
    }
    public override void Update() => Color = tint * MathF.Pow(1f - LifetimeRatio, 2f);
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 140f)) return;
        Vector2 point = Position + Rotation.ToRotationVector2() * size * .27f;
        for (int i = 0; i < 3; i++)
        {
            Vector2 dimensions = new(size * (1.2f - .3f * i), size * (.52f - i * .12f));
            Color color = Color.Lerp(tint, Microsoft.Xna.Framework.Color.White, i * .22f) * MathF.Pow(1f - LifetimeRatio, 2f);
            Main.spriteBatch.Draw(Texture, point - Main.screenPosition, null, color, Rotation,
                Texture.Size() * .5f, dimensions / Texture.Size(), SpriteEffects.None, 0f);
        }
    }
}
