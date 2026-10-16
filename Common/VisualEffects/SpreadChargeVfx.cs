using ArknightsMod.Common.Particle.T;
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
            new T_Charge_Absorption_Particle(this, art, start, aim, charge).Spawn();
        }
    }

    internal static void Pulse(Vector2 center, Vector2 aim, SpreadArt art, bool release, float strength = 1f)
    {
        if (art is SpreadArt.Ember or SpreadArt.Meteor)
        {
            if (SpreadVfxBudget.Take(center))
                new T_Charge_Flash_Particle(center, aim, SpreadArtsVfx.Body(art), ArtScale(art) * strength).Spawn();
            return;
        }
        if (SpreadVfxBudget.Take(center))
            new T_Charge_Pulse_Particle(center, aim, SpreadArtsVfx.Edge(art), ArtScale(art) * strength, release).Spawn();
        if (release && SpreadVfxBudget.Take(center))
            new T_Charge_Flash_Particle(center, aim, SpreadArtsVfx.Body(art), ArtScale(art)).Spawn();
    }
}

// WORLD-space integration: changing the aim point never translates particles/history.


// Pulses and release flashes retain the event's world position and aim.
