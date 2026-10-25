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

// Shared emission primitives; every saved operator has an explicit identity and trajectory.
internal static class MedicArtsVfx
{
    internal static readonly Dictionary<string, Texture2D> Materials = [];
    internal const string MistPath = "ArknightsMod/Content/Projectiles/Sniper/Typhon/Effects/ChargeSmoke03";
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
                TrailMote(id), Main.rand.NextFloat(5, 10), Main.rand.Next(18, 32), i);
        }
        Mote(position, Vector2.Zero, id, MedicalMoteKind.Signature, skill ? 65 : 28, skill ? 30 : 16);
    }

    private static MedicalMoteKind TrailMote(int id) => id switch {
        0 or 2 or 5 or 12 or 16 or 19 => MedicalMoteKind.Thread,
        1 or 6 or 7 or 8 or 9 or 23 => MedicalMoteKind.Drop,
        3 or 13 => MedicalMoteKind.Feather,
        10 or 17 or 22 => MedicalMoteKind.Petal,
        18 or 20 => MedicalMoteKind.Pearl,
        14 or 15 or 21 => MedicalMoteKind.Purity,
        _=>MedicalMoteKind.Bloom
    };

    internal static void Flight(MedicArt art, Vector2 previous)
    {
        Projectile projectile = art.Projectile;
        if (!Visible(projectile.Center)) return;
        var p = art.Profile;
        if(!MedicMaterialVfx.UsesLineArt(art.ProfileId))
        {
            if(!Main.gameMenu)Lighting.AddLight(projectile.Center,p.Body.ToVector3()*.4f);
            MedicMaterialVfx.Flight(projectile,art.ProfileId,art.Age);
            return;
        }
        float ready = .35f + .65f * art.Armed;
        float bodyScale = p.Branch == MedicBranch.MultiTarget ? 2f : 1f;
        if (!Main.gameMenu) Lighting.AddLight(projectile.Center, p.Body.ToVector3() * (.25f + art.Armed * .35f));
        // Spatial interpolation preserves the stream when extraUpdates is only one.
        int samples = 1;
        Vector2 axis = projectile.velocity.SafeNormalize(Vector2.UnitX), side = axis.RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < samples; i++)
        {
            Vector2 point = Vector2.Lerp(previous, projectile.Center, (i + .5f) / samples);
            float spread = (p.Branch == MedicBranch.MultiTarget ? 6 : 3) * ready;
            Mote(point + Main.rand.NextVector2Circular(spread, spread), -projectile.velocity * .05f,
                art.ProfileId, TrailMote(art.ProfileId), (5 + p.Tier * .6f) * ready * bodyScale, 12 + p.Tier);
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

        int id=art.ProfileId;
        int cadence=id switch {2=>2,3=>5,8=>3,12=>6,13=>4,16=>2,20=>7,_=>4};
        if((int)art.Age%cadence==0)
        {
            float phase=art.Age*MedicIdentity.Get(id).Frequency;
            Vector2 offset=id switch {
                3=>new Vector2(MathF.Sin(phase)*12,-8),
                8=>new Vector2(MathF.Sin(phase)*18,8),
                13=>side*MathF.Sin(phase)*18,
                18=>side*(MathF.Sin(phase)>0?9:-9),
                20=>Main.rand.NextVector2Circular(13,13),
                _=>side*MathF.Sin(phase)*6
            };
            Vector2 velocity=id is 8 or 17?Vector2.UnitY*.7f:-axis*.3f+side*MathF.Cos(phase)*.2f;
            Mote(projectile.Center+offset,velocity,id,TrailMote(id),id==20?9:5,24,phase);
        }
        if (art.Age >= p.ArmingDelay && art.Age < p.ArmingDelay + 1)
            Mote(projectile.Center, Vector2.Zero, art.ProfileId, MedicalMoteKind.Pulse, 26, 18);
    }

    internal static void DrawArt(MedicArt art) => MedicIdentityVisuals.Draw(art);

    internal static void Treatment(Vector2 center, int profile, int cleansed)
    {
        if (!Visible(center)) return;
        var p = MedicCatalog.Get(profile);
        Mote(center, Vector2.Zero, profile, MedicalMoteKind.Signature, 40 + p.Tier * 3, 24);
        int count = 10 + p.Tier * 2;
        for (int i = 0; i < count; i++)
        {
            Vector2 radial = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.15f, .15f)).ToRotationVector2();
            bool inward = p.Branch != MedicBranch.MultiTarget;
            Mote(center + radial * (inward ? 26 : 6), radial * (inward ? -1.1f : 1.2f) - Vector2.UnitY * .3f,
                profile, TrailMote(profile),
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
    internal static void DrawField(MedicHealingField field) => MedicIdentityVisuals.Field(field);
}

internal enum MedicalMoteKind { Bloom, Mist, Drop, Pearl, Petal, Thread, Purity, Feather, Pulse, Signature }
