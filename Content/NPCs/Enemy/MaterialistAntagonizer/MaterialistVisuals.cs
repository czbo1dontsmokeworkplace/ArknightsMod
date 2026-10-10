using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static class MaterialistVisuals
{
    internal static readonly Color Cyan = new(63, 207, 225);
    internal static readonly Color Amber = new(255, 139, 43);
    internal static readonly Color Violet = new(195, 120, 255);
    internal static readonly Color White = new(255, 235, 191);
    private static readonly Dictionary<string, Asset<Texture2D>> textures = new();
    internal static bool Reduced => ModContent.GetInstance<MaterialistVisualConfig>().ReducedEffects;
    internal static Texture2D Asset(string path)
    {
        if (!textures.TryGetValue(path, out var value)) textures[path] = value = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        if (!value.IsLoaded) value.Wait();
        return value.Value;
    }
    internal static void Unload() => textures.Clear();
    internal static void Glow(Vector2 center, Color color, Vector2 size, float angle = 0)
    {
        Texture2D texture = Asset("ArknightsMod/Common/Particle/DefaultParticle");
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color with { A = 0 }, angle,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
    }
    internal static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 d = b - a;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
            d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Ring(Vector2 center, float radius, Color color, float angle = 0, float width = 2, float squash = 1, int segments = 48)
    {
        Vector2 last = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * squash);
        for (int i = 1; i <= segments; i++)
        {
            float a = angle + i * MathHelper.TwoPi / segments;
            Vector2 next = center + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * squash);
            Line(last, next, color, width); last = next;
        }
    }
    internal static void Reticle(Vector2 center, float radius, Color color, float time)
    {
        Ring(center, radius, color * .7f, time * .01f, 2);
        for (int i = 0; i < 4; i++)
        {
            Vector2 d = (MathHelper.PiOver2 * i).ToRotationVector2();
            Line(center + d * (radius - 9), center + d * (radius + 12), color, 3);
        }
        Ring(center, 9, color, MathHelper.PiOver4, 2, 1, 4);
    }
    internal static void Burst(Vector2 point, Color color, float power = 1)
    {
        if (Main.dedServ) return;
        int count = Math.Min(Reduced ? 8 : 32, (int)(16 * power));
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1, 1) * Main.rand.NextFloat(2, 7) * power;
            new DefaultParticle(point, velocity, 25, Main.rand.NextFloat(.3f, .7f), i % 4 == 0 ? White : color, true)
                { Deformation = new Vector2(.3f, 2.2f) }.Spawn();
            if (i % 5 == 0) Dust.NewDustPerfect(point, DustID.Smoke, velocity * .25f, 120, default, .8f);
        }
    }
    internal static void UpdateBoss(MaterialistAntagonizer boss, int lastTimer)
    {
        var npc = boss.NPC;
        int t = boss.Timer;
        Lighting.AddLight(npc.Center, .55f, boss.Phase == 3 ? .09f : .28f, .08f);
        if (lastTimer < 1 && t >= 1)
        {
            if (boss.Order is FlightOrder.Arrival or FlightOrder.Transition)
            {
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = .65f, Pitch = -.4f }, npc.Center);
                MaterialistEffects.Pulse(npc.Center, 340, Cyan, 1);
            }
            if (boss.Order == FlightOrder.Recover)
            {
                Burst(npc.Center, Cyan, 1.5f);
                MaterialistEffects.Pulse(npc.Center, 270, Cyan, 3);
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = .7f, Pitch = -.4f }, npc.Center);
            }
        }
        if (boss.Order is FlightOrder.Arrival or FlightOrder.Transition && lastTimer < 100 && t >= 100)
        {
            Burst(npc.Center, boss.Phase == 3 ? Amber : Cyan, 2);
            MaterialistEffects.Pulse(npc.Center, 460, Amber, 4);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = .55f, Pitch = .35f }, npc.Center);
        }
        if (boss.Order == FlightOrder.Death)
        {
            if (t % 12 == 0) Burst(npc.Center + Main.rand.NextVector2Circular(115, 42), Amber, .65f);
            if (lastTimer < 104 && t >= 104)
            {
                Burst(npc.Center, Amber, 3);
                MaterialistEffects.Pulse(npc.Center, 650, Amber, 6);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .9f, Pitch = -.4f }, npc.Center);
            }
        }
        if (boss.Order == FlightOrder.Overload && lastTimer < 112 && t >= 112)
        {
            MaterialistEffects.Pulse(npc.Center, 300, Amber, 3);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = .6f }, npc.Center);
        }
        if (!Reduced && boss.Phase == 3 && t % 10 == 0)
            Dust.NewDustPerfect(npc.Center + new Vector2(85, 15), DustID.Smoke, new Vector2(-npc.velocity.X * .2f, -1.8f), 130, default, 1.1f);
        if (!Reduced && boss.Charging && t % 3 == 0) Burst(npc.Center - boss.DashDirection * 90, Amber, .35f);
    }
    internal static void DrawBoss(MaterialistAntagonizer boss, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        NPC npc = boss.NPC;
        Texture2D body = Asset(MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer");
        float time = (float)Main.GlobalTimeWrappedHourly * 60;
        float opacity = boss.Order == FlightOrder.Death ? MathHelper.Clamp((120 - boss.Timer) / 20f, 0, 1) :
            boss.Order == FlightOrder.Arrival ? MathHelper.Clamp(boss.Timer / 65f, 0, 1) : 1;
        Color accent = boss.Phase == 3 ? Amber : Cyan;
        float scale = 1.45f;
        Vector2 position = npc.Center;
        Vector2 core = position + new Vector2(4, 31).RotatedBy(npc.rotation) * scale;
        if (boss.Order == FlightOrder.Death) position += new Vector2(MathF.Sin(time * 2.3f) * 4, MathF.Cos(time * 3.1f) * 3);
        Glow(position, accent * (.23f * opacity), new Vector2(360, 200));
        if (boss.Charging && !Reduced)
            for (int i = npc.oldPos.Length - 1; i > 0; i--)
                if (npc.oldPos[i] != Vector2.Zero)
                    batch.Draw(body, npc.oldPos[i] + npc.Size * .5f - screenPos, null, (accent with { A = 0 }) * ((1 - i / 10f) * .35f),
                        npc.rotation, body.Size() * .5f, scale, SpriteEffects.None, 0);
        // Engine plumes lie behind the original, unmodified pixel-art chassis.
        foreach (float x in new[] { -85f, 78f })
        {
            Vector2 engine = position + new Vector2(x, -8).RotatedBy(npc.rotation) * scale;
            float length = (boss.Charging ? 85 : 32) + MathF.Sin(time * .7f + x) * 7;
            Vector2 end = engine + new Vector2(-npc.velocity.X * 2, length).RotatedBy(npc.rotation);
            Glow((engine + end) * .5f, accent * (.45f * opacity), new Vector2(length * 1.8f, 26), (end - engine).ToRotation());
            Line(engine, end, (accent with { A = 0 }) * (.8f * opacity), 5);
            Line(engine, Vector2.Lerp(engine, end, .65f), (White with { A = 0 }) * opacity, 2);
        }
        batch.Draw(body, position - screenPos, null, Color.Lerp(drawColor, Color.White, .65f) * opacity,
            npc.rotation, body.Size() * .5f, scale, SpriteEffects.None, 0);
        // Rotor strokes and sensor glow animate the supplied single-frame sprite.
        for (int i = 0; i < 3; i++)
        {
            Vector2 rotor = position + (i == 0 ? new Vector2(-85, -19) : i == 1 ? new Vector2(78, -19) : new Vector2(0, -53)).RotatedBy(npc.rotation) * scale;
            Ring(rotor, i == 2 ? 40 : 32, (White with { A = 0 }) * (.20f * opacity), time * .45f, 1, .12f, 20);
            float blade = MathF.Sin(time * .95f + i) * (i == 2 ? 38 : 29);
            Line(rotor - new Vector2(blade, 0), rotor + new Vector2(blade, 0), Color.LightGray * (.4f * opacity), 2);
        }
        Glow(core, (boss.Order == FlightOrder.Recover ? Cyan : Amber) * (.8f * opacity), new Vector2(34 + MathF.Sin(time * .10f) * 5));
        if (boss.Shield > 0)
        {
            float strength = boss.Shield / (float)Math.Max(1, boss.ShieldMax);
            Ring(position, 188, Cyan * (.3f + .3f * strength), time * .003f, 2, .68f, 64);
            if (!Reduced)
                for (int x = -4; x <= 4; x++)
                    for (int y = -2; y <= 2; y++)
                    {
                        Vector2 tile = new(x * 32, y * 30 + (x % 2 == 0 ? 0 : 15));
                        if (tile.LengthSquared() > 150 * 150) continue;
                        float scan = .10f + .13f * Math.Max(0, MathF.Sin(time * .04f - tile.Y * .05f));
                        Ring(position + tile, 19, Cyan * (scan * strength), MathHelper.Pi / 6, 1, 1, 6);
                    }
            foreach (NPC escort in Main.ActiveNPCs)
                if (escort.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(boss) && !wing.Retreating)
                {
                    Vector2 delta = escort.Center - core;
                    for (int i = 0; i < 8; i++)
                    {
                        float u = (i / 8f + time * .004f) % 1;
                        Line(core + delta * u, core + delta * Math.Min(1, u + .035f), Cyan * .55f, 2);
                    }
                }
        }
        if (boss.Order == FlightOrder.Deploy)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 bay = position + new Vector2(side * 120, 25);
                Ring(bay, 38 + MathF.Sin(time * .09f) * 5, Cyan * .7f, time * .04f, 2, .5f);
                Line(bay, bay + new Vector2(side * 180, 60), Cyan * .25f, 2);
            }
        }
        if (boss.Order == FlightOrder.Ram && boss.Timer is >= 28 and < 84)
        {
            Vector2 normal = boss.DashDirection.RotatedBy(MathHelper.PiOver2) * 65;
            Vector2 end = position + boss.DashDirection * 750;
            Line(position - normal, end - normal, Amber * .55f, 2);
            Line(position + normal, end + normal, Amber * .55f, 2);
            Line(position, end, White * .45f, 2);
            Reticle(boss.Aim, 24, Amber, time);
        }
        if (boss.Order == FlightOrder.Overload && boss.Timer is >= 40 and < 112)
        {
            float angle = (boss.Aim - position).ToRotation();
            for (int i = 0; i < 20; i++)
            {
                float a = i * MathHelper.TwoPi / 20;
                if (Math.Abs(MathHelper.WrapAngle(a - angle)) < .56f) continue;
                Vector2 d = a.ToRotationVector2();
                Line(position + d * 135, position + d * 240, Amber * .45f, 2);
            }
            for (int sign = -1; sign <= 1; sign += 2)
                Line(position, position + (angle + sign * .56f).ToRotationVector2() * 500, Cyan * .45f, 2);
        }
        if (boss.Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Recover)
        {
            string key = boss.Order == FlightOrder.Recover ? "CoreExposed" : "Phase" + boss.Phase;
            Utils.DrawBorderString(batch, Language.GetTextValue("Mods.ArknightsMod.MaterialistText." + key),
                position - screenPos + new Vector2(0, -128), accent * opacity, .8f, .5f, .5f);
        }
    }
    internal static void DrawHazard(MaterialistHazard h)
    {
        Projectile p = h.Projectile;
        Color color = h.Kind == DroneShot.Beam ? Violet : Amber;
        if (h.Kind == DroneShot.Bombardment)
        {
            float r = MaterialistRules.BombRadius;
            if (h.Age < h.Delay)
            {
                float charge = h.Age / (float)Math.Max(1, h.Delay);
                Reticle(p.Center, r, Amber * (.45f + .4f * charge), h.Age);
                Ring(p.Center, r * (1.65f - charge * .65f), Amber * .30f, 0, 1);
                Line(p.Center - new Vector2(0, 400), p.Center - new Vector2(0, 18), Amber * .12f, 2);
                Glow(p.Center, Amber * .10f, new Vector2(r * 2));
                if (charge > .65f)
                {
                    Vector2 falling = p.Center - new Vector2(0, (1 - charge) / .35f * 400);
                    Line(falling - new Vector2(0, 80), falling, White * .8f, 4);
                }
            }
            else
            {
                float fade = MathHelper.Clamp((h.Delay + h.Duration - h.Age) / (float)h.Duration, 0, 1);
                Glow(p.Center, Amber * fade, new Vector2(r * 3));
                Glow(p.Center, White * fade, new Vector2(r * 1.4f));
                Ring(p.Center, r, Amber * fade, 0, 4);
            }
            return;
        }
        if (h.Kind == DroneShot.Beam)
        {
            Vector2 direction = h.BeamDirection;
            Vector2 end = p.Center + direction * MaterialistRules.BeamLength;
            if (h.Age < h.Delay)
            {
                Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY);
                for (int side = -1; side <= 1; side += 2)
                    Line(p.Center, p.Center + axis.RotatedBy(side * h.Sweep * .5f) * MaterialistRules.BeamLength, Violet * .20f, 1);
                Line(p.Center, end, Violet * .65f, 2);
                Glow(p.Center, Violet * .7f, new Vector2(20 + h.Age * .5f));
            }
            else
            {
                float strength = h.BeamStrength;
                float width = MaterialistRules.BeamWidth * strength;
                Glow((p.Center + end) * .5f, Violet * (.30f * strength), new Vector2(MaterialistRules.BeamLength, 55), direction.ToRotation());
                Line(p.Center, end, (Violet with { A = 0 }) * strength, width + 5);
                Line(p.Center, end, (White with { A = 0 }) * strength, width * .45f);
                Glow(p.Center, White * strength, new Vector2(44));
            }
            return;
        }
        for (int i = p.oldPos.Length - 1; i >= 1; i--)
            if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
                Line(p.oldPos[i] + p.Size * .5f, p.oldPos[i - 1] + p.Size * .5f, (color with { A = 0 }) * ((1 - i / 10f) * .7f), 5 * (1 - i / 10f));
        Glow(p.Center, color * .6f, new Vector2(32, 20), p.rotation);
        Line(p.Center - p.velocity.SafeNormalize(Vector2.UnitX) * 9, p.Center + p.velocity.SafeNormalize(Vector2.UnitX) * 6, color, 7);
        Glow(p.Center, White, new Vector2(10));
    }
}
