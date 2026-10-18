using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// Five different silhouettes, not five recolors. No projectile body sprites are required.
internal static class MedicArtsVfx
{
    internal static readonly Dictionary<string, Texture2D> Materials = [];
    internal const string MistPath = "ArknightsMod/Content/Projectiles/Sniper/Typhon/Effects/ChargeSmoke03";
    internal static Texture2D VeilTexture;
    private static Texture2D SoftVeil()
    {
        if (VeilTexture != null) return VeilTexture;
        const int size = 64;
        var colors = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + .5f) / 32f - 1, (y + .5f) / 32f - 1).Length();
            byte alpha = (byte)(MathF.Exp(-radius * radius * 3) * MathHelper.Clamp((1 - radius) * 5, 0, 1) * 255);
            colors[y * size + x] = new Color(alpha, alpha, alpha, alpha);
        }
        VeilTexture = new Texture2D(Main.instance.GraphicsDevice, size, size);
        VeilTexture.SetData(colors);
        return VeilTexture;
    }
    internal static Texture2D Texture(string path)
    {
        if (!Materials.TryGetValue(path, out var texture)) Materials[path] = texture = ModContent.Request<Texture2D>(path).Value;
        return texture;
    }
    internal static Color Light(Color color, float opacity = 1) => new Color(color.R, color.G, color.B, 0) * MathHelper.Clamp(opacity, 0, 1);
    private static bool Visible(Vector2 position) => !Main.dedServ && position.DistanceSQ(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) < 2000 * 2000;
    internal static void Glow(Vector2 position, Vector2 size, Color color, float opacity, float rotation = 0)
    {
        Texture2D texture = Texture(OperatorTextureEffects.GlowPath);
        Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, Light(color, opacity), rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }
    internal static void Material(string name, Vector2 position, Vector2 size, Color color, float opacity, float rotation = 0)
    {
        var texture = Texture(OperatorFlightVfx.Assets + name);
        // LinearSpark's authored long axis is vertical; callers specify along/across the path.
        if (name == "LinearSpark") { size = new Vector2(size.Y, size.X); rotation += MathHelper.PiOver2; }
        Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, Light(color, opacity), rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }

    // Soft-edged segments instead of opaque rectangular line drawing.
    internal static void Thread(Vector2 a, Vector2 b, Color color, float width, float opacity)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < .05f || length > 100 || !float.IsFinite(length)) return;
        Glow((a + b) * .5f, new Vector2(length * 1.6f + 3, width * 3), color, opacity * .35f, delta.ToRotation());
        Material("LinearSpark", (a + b) * .5f, new Vector2(length + 3, width), color, opacity, delta.ToRotation());
    }
    internal static void Arc(Vector2 center, Vector2 radius, float rotation, float start, float sweep, Color color, float width, float opacity)
    {
        int count = Math.Clamp((int)(MathF.Abs(sweep) * Math.Max(radius.X, radius.Y) / 8), 12, 100);
        Vector2 previous = center + (new Vector2(MathF.Cos(start), MathF.Sin(start)) * radius).RotatedBy(rotation);
        for (int i = 1; i <= count; i++)
        {
            float phase = start + sweep * i / count;
            Vector2 next = center + (new Vector2(MathF.Cos(phase), MathF.Sin(phase)) * radius).RotatedBy(rotation);
            Thread(previous, next, color, width, opacity);
            previous = next;
        }
    }
    internal static void Mote(Vector2 position, Vector2 velocity, int profile, MedicalMoteKind kind, float size, int life, float phase = 0)
    {
        if (!Visible(position) || ParticleManager.activeParticles.Count > 1500) return;
        new T_Medic_Arts_Mote(position, velocity, profile, kind, size, life, phase).Spawn();
    }

    internal static void Cast(Vector2 position, Vector2 aim, int id, bool skill)
    {
        if (!Visible(position)) return;
        var p = MedicCatalog.Get(id);
        int count = skill ? 18 : 6 + p.Tier;
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = skill ? (i * MathHelper.TwoPi / count).ToRotationVector2() : aim.RotatedByRandom(.7f);
            Mote(position, direction * Main.rand.NextFloat(1, skill ? 5 : 3), id,
                MedicalMoteKind.Petal, Main.rand.NextFloat(5, 10), Main.rand.Next(18, 32), i);
        }
        Mote(position, Vector2.Zero, id, MedicalMoteKind.Pulse, skill ? 65 : 28, skill ? 30 : 16);
    }

    internal static void Flight(MedicArt art, Vector2 previous)
    {
        Projectile projectile = art.Projectile;
        if (!Visible(projectile.Center)) return;
        var p = art.Profile;
        float ready = .35f + .65f * art.Armed;
        float bodyScale = p.Branch == MedicBranch.MultiTarget ? 2f : 1f;
        if (!Main.gameMenu) Lighting.AddLight(projectile.Center, p.Body.ToVector3() * (.25f + art.Armed * .35f));
        // Spatial interpolation preserves the stream when extraUpdates is only one.
        int samples = Math.Clamp((int)(Vector2.Distance(previous, projectile.Center) / 4) + 1, 2, 5);
        Vector2 axis = projectile.velocity.SafeNormalize(Vector2.UnitX), side = axis.RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < samples; i++)
        {
            Vector2 point = Vector2.Lerp(previous, projectile.Center, (i + .5f) / samples);
            float spread = (p.Branch == MedicBranch.MultiTarget ? 6 : 3) * ready;
            Mote(point + Main.rand.NextVector2Circular(spread, spread), -projectile.velocity * .05f,
                art.ProfileId, MedicalMoteKind.Bloom, (5 + p.Tier * .6f) * ready * bodyScale, 12 + p.Tier);
            if (p.Tier <= 1)
            {
                Dust dust = Dust.NewDustPerfect(point, DustID.TintableDustLighted, Vector2.Zero, 130, p.Body, .65f + p.Tier * .1f);
                dust.noGravity = true; dust.velocity *= 0;
            }
        }
        int mistCadence = p.Branch switch { MedicBranch.Chain or MedicBranch.Wandering => 10,
            MedicBranch.Therapist => 6, MedicBranch.MultiTarget => 3, _ => 4 };
        if ((int)art.Age % mistCadence == 0)
            Mote(projectile.Center + Main.rand.NextVector2Circular(8, 8),
                -projectile.velocity * .1f + new Vector2(Main.rand.NextFloat(-.4f, .4f), -.4f), art.ProfileId,
                MedicalMoteKind.Mist, p.Branch == MedicBranch.MultiTarget ? 30 : 17, 30 + p.Tier * 2);

        switch (p.Branch)
        {
            case MedicBranch.Physician:
                if ((int)art.Age % 4 == 0)
                    Mote(projectile.Center + side * Main.rand.NextFloat(-7, 7), side * Main.rand.NextFloat(-.4f, .4f) + Vector2.UnitY * .5f,
                        art.ProfileId, MedicalMoteKind.Drop, 4, 26);
                break;
            case MedicBranch.Therapist:
                if ((int)art.Age % 3 == 0)
                    Mote(projectile.Center - axis * 4, -axis * .6f + side * MathF.Sin(art.Age * .15f),
                        art.ProfileId, MedicalMoteKind.Pearl, 4, 30);
                if (art.HomingStrength > 0)
                {
                    // Luminous beads and short drifting star fragments follow the actual turn.
                    Mote(projectile.Center - axis * 5 + Main.rand.NextVector2Circular(4, 4),
                        -axis * Main.rand.NextFloat(.4f, 1.1f) + side * Main.rand.NextFloat(-.18f, .18f),
                        art.ProfileId, MedicalMoteKind.Bloom, 5 + art.HomingStrength * 4, 16);
                    if ((int)art.Age % 3 == 0)
                        Mote(projectile.Center - axis * 7 + side * Main.rand.NextFloat(-5, 5),
                            -axis * Main.rand.NextFloat(.7f, 1.4f), art.ProfileId, MedicalMoteKind.Purity, 6, 16);
                    if (art.Age < p.ArmingDelay + 1)
                        for (int spark = 0; spark < 8; spark++)
                            Mote(projectile.Center, (spark * MathHelper.TwoPi / 8).ToRotationVector2() * 1.5f,
                                art.ProfileId, MedicalMoteKind.Purity, 8, 24, spark);
                }
                break;
            case MedicBranch.Chain:
                for (int strand = 0; strand < 2; strand++)
                {
                    float phase = art.Age * .25f + strand * MathHelper.Pi;
                    Mote(projectile.Center + side * MathF.Sin(phase) * 8, -axis * .3f,
                        art.ProfileId, MedicalMoteKind.Thread, 5, 22, phase);
                }
                break;
            case MedicBranch.Wandering:
                if ((int)art.Age % 3 == 0)
                    Mote(projectile.Center + side * MathF.Sin(art.Age * .2f) * 8, -axis * .4f + side * .2f,
                        art.ProfileId, MedicalMoteKind.Purity, 6, 24);
                break;
            case MedicBranch.MultiTarget:
                if ((int)art.Age % 3 == 0)
                    Mote(projectile.Center + new Vector2(Main.rand.NextFloat(-9, 9), 5), Vector2.UnitY * .6f,
                        art.ProfileId, p.Tier == 5 ? MedicalMoteKind.Feather : MedicalMoteKind.Petal, 7, 36);
                break;
        }
        if (art.Age >= p.ArmingDelay && art.Age < p.ArmingDelay + 1)
            Mote(projectile.Center, Vector2.Zero, art.ProfileId, MedicalMoteKind.Pulse, 26, 18);
    }

    internal static void DrawArt(MedicArt art)
    {
        if (Main.dedServ) return;
        var projectile = art.Projectile; var p = art.Profile;
        float fade = Math.Min(MathHelper.Clamp(art.Age / 6, 0, 1), MathHelper.Clamp(projectile.timeLeft / 24f, 0, 1));
        float ready = .4f + .6f * art.Armed;
        float radius = p.ArtRadius;
        float time = art.Age;
        float rotation = projectile.rotation;
        Vector2 center = projectile.Center;
        Vector2 axis = rotation.ToRotationVector2(), side = axis.RotatedBy(MathHelper.PiOver2);
        // The cold outer wake and warm inner filament follow the same actual curved trajectory.
        if (p.Tier > 0 && p.Branch != MedicBranch.MultiTarget)
        {
            float length = p.Branch == MedicBranch.Therapist ? 110 : 72 + p.Tier * 6;
            MedicRibbon.Draw(projectile, p.Body * fade, p.Core * fade, 5 + p.Tier * .8f, length, art.Age);
            MedicRibbon.Draw(projectile, p.Core * (fade * .65f), Color.White * (fade * .75f), 1.4f, length * .75f, art.Age);
        }
        Glow(center, new Vector2(radius * 4.7f), p.Body, .3f * fade * ready);
        switch (p.Branch)
        {
            case MedicBranch.Physician:
                // A rounded, breathing mass with short liquid wake; Shining adds a translucent dark veil.
                if (art.ProfileId == 5)
                {
                    Texture2D cloud = SoftVeil();
                    Main.EntitySpriteDraw(cloud, center - Main.screenPosition, null, new Color(20, 23, 40, 150) * fade,
                        -time * .025f, cloud.Size() * .5f, new Vector2(radius * 3.1f, radius * 2.4f) / cloud.Size(), SpriteEffects.None);
                }
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = new Vector2(MathF.Cos(time * .09f + i * 2.1f), MathF.Sin(time * .13f + i * 2.1f)) * radius * .2f;
                    Glow(center + offset, new Vector2(radius * (2.7f - i * .35f), radius * (1.9f - i * .25f)),
                        i == 2 ? p.Core : p.Body, fade * ready * (.38f + i * .12f), rotation);
                }
                Material("OrbitSmear", center, new Vector2(radius * 2.3f), p.Core, fade * .45f * ready, time * .045f);
                Glow(center + axis * 3 - side * 2, new Vector2(radius * .9f), p.Core, fade * ready);
                if (art.ProfileId == 2)
                    Material("ImpactPetals", center, new Vector2(radius * 2.3f), p.Body, fade * .2f * ready, -time * .02f);
                if (art.ProfileId == 3)
                    Arc(center, new Vector2(radius * 1.1f, radius * .6f), rotation, time * .04f, MathHelper.Pi * 1.4f, p.Core, .8f, fade * ready * .55f);
                if (art.ProfileId == 4)
                    Glow(center, new Vector2(radius * (1.8f + MathF.Sin(time * .13f) * .25f)), p.Core, fade * ready * .3f);
                break;
            case MedicBranch.Therapist:
                // Trailing caustic arcs retain a water silhouette during homing turns.
                Glow(center + axis * 3, new Vector2(radius * 1.7f, radius * 1.3f), p.Core, fade * ready, rotation);
                Material("OrbitSmear", center - axis * 2, new Vector2(radius * 2.3f, radius * 1.6f), p.Body, fade * ready * .7f, rotation + MathF.Sin(time * .08f) * .15f);
                Arc(center, new Vector2(radius * .8f, radius * .5f), rotation, -.8f, 2.4f, p.Core, .8f, fade * ready * .7f);
                if (art.ProfileId == 9) Material("StarCore", center + axis * 3, new Vector2(18, 24), p.Core, fade * ready * .8f, time * .015f);
                if (art.ProfileId == 8)
                    Material("OrbitSmear", center - axis * 7, new Vector2(radius * 3.2f, radius * 1.9f), p.Body, fade * .23f, -time * .018f);
                if (art.HomingStrength > 0)
                    DrawHomingLight(art, axis, radius, fade * art.HomingStrength);
                break;
            case MedicBranch.Chain:
                DrawBraid(center, axis, radius, time, p, fade * ready, false);
                Glow(center + axis * 2, new Vector2(radius * 1.1f), p.Core, fade * ready);
                break;
            case MedicBranch.Wandering:
                DrawBraid(center, axis, radius, time, p, fade * ready, true);
                Material("StarCore", center, new Vector2(radius * 1.5f, radius * 1.1f), p.Core, fade * ready * .8f, rotation);
                break;
            case MedicBranch.MultiTarget:
                // A heavy seed: a rounded body, circulating inner energy and a short shedding wake.
                Glow(center, new Vector2(radius * 2.7f, radius * 2.1f), p.Body, fade * ready * .65f, rotation);
                Material("OrbitSmear", center, new Vector2(radius * 2.2f), p.Body, fade * ready * .7f, time * .035f);
                Material("OrbitSmear", center, new Vector2(radius * 1.7f), p.Core, fade * ready * .5f, -time * .026f);
                Glow(center, new Vector2(radius * 1.05f), p.Core, fade * ready * .8f);
                break;
        }
    }

    private static void DrawHomingLight(MedicArt art, Vector2 axis, float radius, float opacity)
    {
        Projectile projectile = art.Projectile; var p = art.Profile;
        int count = Math.Min(18, projectile.oldPos.Length);
        for (int i = count - 1; i >= 0; i--)
        {
            if (projectile.oldPos[i] == Vector2.Zero) continue;
            float tail = 1f - i / (float)count;
            Vector2 point = projectile.oldPos[i] + projectile.Size * .5f;
            Glow(point, new Vector2(radius * (1.1f + tail)), p.Body, opacity * tail * tail * .16f);
            if (i % 3 == 0)
                Material("StarCore", point, new Vector2(radius * 1.5f, radius * .42f) * tail,
                    p.Core, opacity * tail * .32f, projectile.rotation);
        }
        float pulse = 1f + MathF.Sin(art.Age * .24f + projectile.identity * .4f) * .06f;
        Vector2 center = projectile.Center + axis * 3;
        Glow(center, new Vector2(radius * 3.5f) * pulse, p.Body, opacity * .38f);
        Glow(center, new Vector2(radius * 1.25f), p.Core, opacity * .9f);
        Material("StarCore", center, new Vector2(radius * 3.1f, radius * .85f) * pulse,
            p.Core, opacity * .85f, projectile.rotation);
        Material("StarCore", center, new Vector2(radius * 1.65f, radius * .45f),
            Color.White, opacity * .48f, projectile.rotation + MathHelper.PiOver2);
    }

    private static void DrawBraid(Vector2 center, Vector2 axis, float radius, float time, MedicProfile p, float opacity, bool purify)
    {
        Vector2 side = axis.RotatedBy(MathHelper.PiOver2);
        int strands = purify ? 2 : 3;
        for (int strand = 0; strand < strands; strand++)
        {
            Vector2 previous = center;
            for (int i = 0; i <= 26; i++)
            {
                float t = i / 26f;
                float phase = time * (purify ? .14f : .11f) - t * (purify ? 8 : 11) + strand * MathHelper.TwoPi / strands;
                float envelope = MathF.Sin(MathHelper.Pi * (.15f + t * .85f));
                Vector2 next = center - axis * (t * (purify ? 58 : 46))
                    + side * MathF.Sin(phase) * radius * (purify ? .85f : .65f) * envelope;
                float depth = .45f + .55f * (.5f + .5f * MathF.Cos(phase));
                if (i > 0)
                {
                    Thread(previous, next, p.Body, 2.2f * depth, opacity * (1 - t) * .85f);
                    Thread(previous, next, p.Core, .65f * depth, opacity * (1 - t));
                }
                if (!purify && i % 8 == 3)
                    Material("StarCore", next, new Vector2(5 + depth * 3), p.Core, opacity * (1 - t), phase);
                previous = next;
            }
        }
    }

    internal static void Treatment(Vector2 center, int profile, int cleansed)
    {
        if (!Visible(center)) return;
        var p = MedicCatalog.Get(profile);
        Mote(center, Vector2.Zero, profile, MedicalMoteKind.Pulse, 32 + p.Tier * 3, 24);
        int count = 10 + p.Tier * 2;
        for (int i = 0; i < count; i++)
        {
            Vector2 radial = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.15f, .15f)).ToRotationVector2();
            bool inward = p.Branch != MedicBranch.MultiTarget;
            Mote(center + radial * (inward ? 26 : 6), radial * (inward ? -1.1f : 1.2f) - Vector2.UnitY * .3f,
                profile, p.Branch == MedicBranch.Therapist ? MedicalMoteKind.Pearl : MedicalMoteKind.Petal,
                Main.rand.NextFloat(5, 10), Main.rand.Next(22, 36), i);
        }
        for (int i = 0; i < Math.Min(18, cleansed * 3); i++)
            Mote(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2, 4), profile, MedicalMoteKind.Purity, 8, 28, i);
    }
    internal static void Dissolve(Vector2 center, Vector2 velocity, int profile, bool field)
    {
        if (!Visible(center)) return;
        var p = MedicCatalog.Get(profile);
        int count = field ? 18 : 8 + p.Tier;
        for (int i = 0; i < count; i++)
            Mote(center + Main.rand.NextVector2Circular(6, 6), velocity * .12f + Main.rand.NextVector2Unit() * Main.rand.NextFloat(.4f, 2),
                profile, i % 3 == 0 ? MedicalMoteKind.Mist : MedicalMoteKind.Drop, i % 3 == 0 ? 22 : 5, 24 + i % 10);
    }
    internal static void Rejuvenation(Player player, int profile, int layers)
    {
        if (!Visible(player.Center)) return;
        int orbits = Math.Clamp((layers + 3) / 4, 1, 5);
        for (int i = 0; i < orbits; i++)
        {
            float angle = player.miscCounter * .045f + i * MathHelper.TwoPi / orbits;
            Vector2 point = player.Center + new Vector2(MathF.Cos(angle) * 20, MathF.Sin(angle) * 9 - 4);
            Mote(point, new Vector2(-MathF.Sin(angle) * .5f, -.35f), profile, MedicalMoteKind.Thread, 4 + layers * .09f, 26, angle);
        }
    }

    internal static void FieldAmbient(MedicHealingField field)
    {
        Projectile projectile = field.Projectile;
        if (!Visible(projectile.Center)) return;
        var p = field.Profile;
        if (field.Age % 3 == 0)
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 radial = angle.ToRotationVector2();
            Mote(projectile.Center + radial * p.FieldRadius * field.Visibility, radial.RotatedBy(MathHelper.PiOver2) * .35f - Vector2.UnitY * .3f,
                field.ProfileId, p.Tier == 5 ? MedicalMoteKind.Feather : MedicalMoteKind.Petal, 5, 50, angle);
            Mote(projectile.Center + Main.rand.NextVector2Circular(p.FieldRadius * .8f, p.FieldRadius * .8f),
                -Vector2.UnitY * Main.rand.NextFloat(.25f, .7f), field.ProfileId, MedicalMoteKind.Pearl, 3, 50);
        }
        if (field.Age % 10 == 0)
            Mote(projectile.Center + Main.rand.NextVector2Circular(p.FieldRadius * .7f, p.FieldRadius * .7f),
                -Vector2.UnitY * .1f, field.ProfileId, MedicalMoteKind.Mist, 36, 55);
    }
    internal static void DrawField(MedicHealingField field)
    {
        if (Main.dedServ) return;
        var p = field.Profile; Vector2 center = field.Projectile.Center;
        float vis = field.Visibility, radius = p.FieldRadius * (1 - MathF.Pow(1 - vis, 3));
        float time = field.Age;
        // The true circular healing boundary is drawn as a circle, not a misleading floor ellipse.
        Glow(center, new Vector2(radius * 2.5f), p.Body, .065f * vis);
        Material("OrbitSmear", center, new Vector2(radius * 2.02f), p.Body, .13f * vis, time * .002f);
        Arc(center, new Vector2(radius), 0, time * .005f, MathHelper.TwoPi, p.Body, 1.2f, .42f * vis);
        int petals = 5 + p.Tier;
        for (int i = 0; i < petals; i++)
        {
            float phase = i * MathHelper.TwoPi / petals + time * .0015f;
            Vector2 anchor = center + phase.ToRotationVector2() * radius * .75f;
            Material(p.Tier == 5 ? "ImpactPetals" : "OrbitSmear", anchor,
                new Vector2(radius * .65f, radius * .42f), p.Core, .07f * vis, -phase + time * .003f);
            Vector2 star = center + phase.ToRotationVector2() * radius;
            Glow(star, new Vector2(9 + MathF.Sin(time * .05f + i) * 2), p.Core, .6f * vis);
        }
        // Whiteface's regular pulse and Nightingale's layered veil remain low-opacity.
        if (p.Tier >= 4)
        {
            float travel = time % 180 / 180f;
            Arc(center, new Vector2(radius * travel), 0, 0, MathHelper.TwoPi, p.Core, .9f,
                MathF.Sin(travel * MathHelper.Pi) * .16f * vis);
            Material("CrystalVortex", center, new Vector2(radius * 1.25f), p.Body, .07f * vis, -time * .001f);
        }
        // A small center marks the landing point without obscuring combat in the field.
        Glow(center, new Vector2(28), p.Core, .32f * vis);
    }
}

internal enum MedicalMoteKind { Bloom, Mist, Drop, Pearl, Petal, Thread, Purity, Feather, Pulse }
