using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherVisuals
{
    private static readonly Dictionary<CrusherKind, Texture2D> art = new();
    internal static bool Visible(Vector2 point) => !Main.dedServ && Vector2.DistanceSquared(point,
        Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) < 1800 * 1800;
    internal static void Unload() => art.Clear();
    internal static void Manifest(Vector2 center, CrusherKind kind)
    {
        if (!Visible(center)) return;
        // Match the immediate activation cue with original-game sounds, never Calamity audio.
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .8f, Pitch = -.55f, MaxInstances = 2 }, center);
        SoundEngine.PlaySound(SoundID.Item92 with { Volume = .55f, Pitch = -.4f, MaxInstances = 2 }, center);
        for (int i = 0; i < 18 && ParticleManager.activeParticles.Count < 1200; i++) {
            Vector2 ray = (i * MathHelper.TwoPi / 18).ToRotationVector2();
            new T_Crusher_Fragment(center + ray * 60, -ray * 3.2f, kind, 22, 13, false).Spawn();
        }
    }
    internal static void IdleMotes(Vector2 grip, Vector2 tip, CrusherKind kind, float opacity)
    {
        if (!Visible(tip) || opacity < .05f || ParticleManager.activeParticles.Count > 1100) return;
        Vector2 point = Vector2.Lerp(grip, tip, Main.rand.NextFloat(.25f, 1));
        Vector2 axis = (tip - grip).SafeNormalize(Vector2.UnitX);
        new T_Crusher_Fragment(point, axis.RotatedBy(-.6f) * 1.8f, kind, 18, 7, false).Spawn();
    }
    internal static void DrawBlade(Vector2 grip, Vector2 tip, CrusherKind kind, float opacity, bool hot)
    {
        if (!Visible(grip) || opacity <= 0) return;
        if (!art.TryGetValue(kind, out Texture2D sprite)) art[kind] = sprite = ModContent.Request<Texture2D>(CrusherRules.Art(kind), AssetRequestMode.ImmediateLoad).Value;
        Vector2 native = CrusherRules.Head(kind) - CrusherRules.Grip(kind), axis = tip - grip;
        float scale = axis.Length() / native.Length(), rotation = axis.ToRotation() - native.ToRotation();
        float mantleScale = Math.Clamp(axis.Length() / 210f, .4f, 2.8f);
        // Actual anchor art is vertical, with the anchor crown below its grip. This is +90deg
        // in source coordinates, versus about -45deg for the three conventional blades.
        Main.EntitySpriteDraw(sprite, grip - Main.screenPosition, null, Color.White * opacity,
            rotation, CrusherRules.Grip(kind), scale, SpriteEffects.None);
        using (new ChargedOperatorVfx.Pass(true)) {
            // Layered emissive silhouette stays alive in standby, following the weapon's
            // own transformed sprite rather than detached flashes around the player.
            for (int i = 0; i < 8; i++) {
                Vector2 halo = (i * MathHelper.TwoPi / 8).ToRotationVector2() * (2.1f + .5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8));
                Main.EntitySpriteDraw(sprite, grip - Main.screenPosition + halo, null, CrusherRules.Tint(kind) * (.105f * opacity),
                    rotation, CrusherRules.Grip(kind), scale, SpriteEffects.None);
            }
            if (hot) {
                for (int i = 0; i < 4; i++) {
                    float t = .4f + i * .17f;
                    Vector2 point = Vector2.Lerp(grip, tip, t);
                    float flutter = .85f + .15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 23 + i * 2.4f);
                    ChargedOperatorVfx.Sprite("ForwardFlame", point, new Vector2(43, 23) * flutter * mantleScale,
                        CrusherRules.Tint(kind) * (.62f * opacity), axis.ToRotation() - .3f);
                }
            }
            ChargedOperatorVfx.Sprite("StarCore", tip, new Vector2(hot ? 31 : 15) * mantleScale, CrusherRules.Edge(kind) * (.55f * opacity), rotation);
        }
    }
    internal static void Shed(Vector2 grip, Vector2 tip, Vector2 oldTip, CrusherKind kind, int tick)
    {
        if (!Visible(tip) || ParticleManager.activeParticles.Count > 1100) return;
        Vector2 tangent = (tip - oldTip).SafeNormalize((tip - grip).SafeNormalize(Vector2.UnitX));
        int count = 2 + (int)kind / 2;
        for (int i = 0; i < count; i++) {
            Vector2 point = Vector2.Lerp(grip, tip, Main.rand.NextFloat(.55f, 1));
            new T_Crusher_Fragment(point, tangent * Main.rand.NextFloat(1, 4) + Main.rand.NextVector2Circular(1.8f, 1.8f), kind, 14 + i * 3, 7 + i * 2, false).Spawn();
        }
        if (tick % 4 == 0) {
            ChargedOperatorVfx.Sparkle(tip, tangent * 1.3f, CrusherRules.Edge(kind), tangent.ToRotation(), .55f);
            Dust dust = Dust.NewDustPerfect(tip, DustID.TintableDustLighted, tangent * 2, 120, CrusherRules.Tint(kind), .8f); dust.noGravity = true;
        }
    }
    internal static void AuraMote(Vector2 foot, CrusherKind kind, int age, float gravity)
    {
        if (!Visible(foot) || ParticleManager.activeParticles.Count > 1000) return;
        float angle = age * .12f;
        Vector2 point = foot + new Vector2(MathF.Cos(angle) * 56, MathF.Sin(angle) * 15);
        new T_Crusher_Fragment(point, new Vector2(MathF.Sin(angle) * -.3f, -.65f * gravity), kind, 30, 4, false).Spawn();
    }
    internal static IEnumerable<Particle> BurstParticles(Vector2 point, Vector2 forward, CrusherKind kind, float strength)
    {
        int count = Math.Clamp((int)(22 * strength), 8, 35);
        for (int i = 0; i < count; i++) {
            float angle = MathHelper.TwoPi * (i + Main.rand.NextFloat(-.25f, .25f)) / count;
            Vector2 ray = angle.ToRotationVector2();
            yield return new T_Crusher_Fragment(point + ray * 5, ray * Main.rand.NextFloat(3, 9) * strength + forward * 2,
                kind, Main.rand.Next(18, 34), Main.rand.NextFloat(8, 18) * strength, false);
            if (i % 5 == 0) yield return new T_Crusher_Fragment(point + ray * 9, ray * 2 + forward * 1.5f,
                kind, 24 + i % 10, 28 * strength, true);
        }
    }
    internal static void Impact(Vector2 point, Vector2 forward, CrusherKind kind, float strength)
    {
        if (!Visible(point)) return;
        foreach (Particle p in BurstParticles(point, forward, kind, strength)) if (ParticleManager.activeParticles.Count < 1200) p.Spawn();
        for (int i = 0; i < 7; i++) {
            Vector2 ray = (MathHelper.TwoPi * i / 7).ToRotationVector2();
            ChargedOperatorVfx.Sparkle(point + ray * 15, ray * 1.2f, CrusherRules.Edge(kind), ray.ToRotation(), .7f * strength);
        }
        for (int i = 0; i < 4; i++) { Dust d = Dust.NewDustPerfect(point, DustID.TintableDustLighted, Main.rand.NextVector2Circular(4, 4), 80, CrusherRules.Tint(kind), 1); d.noGravity = true; }
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .6f * strength, Pitch = -.58f, MaxInstances = 3 }, point);
        SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = .5f * strength, Pitch = -.4f, MaxInstances = 3 }, point);
    }
    internal static void Activate(Player owner, CrusherKind kind)
    {
        if (!Visible(owner.Center)) return;
        for (int i = 0; i < 16; i++) {
            Vector2 ray = (i * MathHelper.TwoPi / 16).ToRotationVector2();
            new T_Crusher_Fragment(owner.Bottom + ray * new Vector2(54, 16), new Vector2(ray.X, -2), kind, 28, 8, false).Spawn();
        }
        SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -.4f, Volume = .6f }, owner.Center);
    }
}
