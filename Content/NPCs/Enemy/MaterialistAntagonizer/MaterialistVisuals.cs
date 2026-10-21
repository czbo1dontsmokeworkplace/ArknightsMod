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
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static float layerOpacity = 1;
    internal static readonly Color Cyan = new(63, 207, 225);
    internal static readonly Color Amber = new(255, 139, 43);
    internal static readonly Color Violet = new(195, 120, 255);
    internal static readonly Color White = new(255, 235, 191);
    internal static Color AmmoColor(MaterialistHazard h) => h.Kind switch {
        DroneShot.PincerBeam => new Color(106, 244, 218),
        DroneShot.SaucerRound => new Color(255, 232, 112),
        DroneShot.Beam or DroneShot.ChaseBeam or DroneShot.ArtsOrb => Violet,
        DroneShot.ScoutPulse => new Color(255, 179, 94),
        DroneShot.Prism => Color.Lerp(Cyan, new Color(175, 255, 77), .5f + .5f * MathF.Sin((float)Main.GlobalTimeWrappedHourly * 2 + h.Projectile.identity * .37f)),
        DroneShot.Seeker => new Color(166, 245, 75),
        DroneShot.FluxMine => Violet,
        DroneShot.RailSlug => new Color(255, 209, 97),
        DroneShot.TrailBomb => new Color(255, 92, 36),
        DroneShot.Lattice => new Color(85, 180, 255),
        DroneShot.Petal => new Color(255, 114, 177),
        DroneShot.ArcSpark => new Color(65, 244, 190),
        DroneShot.ClusterOrb => new Color(255, 184, 70),
        DroneShot.OrbitSpark => Violet,
        DroneShot.Needle => White,
        DroneShot.CoreLance => new Color(255, 155, 48),
        DroneShot.MachineRound => new Color(255, 201, 77),
        DroneShot.GravityOrb => new Color(138, 255, 159),
        DroneShot.ScanBurst => Cyan,
        DroneShot.Beacon => h.Bend > .5f ? new Color(255, 163, 74) : Cyan,
        DroneShot.DropMissile => new Color(157, 244, 222),
        DroneShot.CommandRocket => Cyan,
        DroneShot.RaptorRocket => new Color(255, 201, 77),
        DroneShot.CannonShell or DroneShot.ArtilleryBlast => new Color(255, 176, 71),
        _ => h.Style == FireStyle.Containment ? Cyan : h.IsBeam ? Violet : Amber
    };
    internal static Color EngineColor(int phase, FlightOrder order, int shield)
    {
        if (order is FlightOrder.Recover) return new Color(53, 239, 176);
        if (order is FlightOrder.Deploy or FlightOrder.Arrival) return new Color(57, 150, 255);
        if (order is FlightOrder.Ram or FlightOrder.Overload || phase >= 3) return new Color(255, 62, 37);
        if (shield > 0) return new Color(180, 95, 255);
        return order == FlightOrder.Command ? Amber : Cyan;
    }
    internal static void EngineFlame(Vector2 engine, Vector2 velocity, float rotation, bool charging, Color tint, float time, float opacity)
    {
        // One wall-clock cadence in every phase; engine colour and power do not speed up its flicker.
        float length = 2 * ((charging ? 85 : 32) + MathF.Sin(time * .18f) * 7);
        Vector2 end = engine + new Vector2(-velocity.X * 4, length).RotatedBy(rotation);
        Vector2 normal = (end - engine).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        Glow((engine + end) * .5f, tint * (.9f * opacity), new Vector2(length * 1.8f, 52), (end - engine).ToRotation());
        Line(engine, end, (tint with { A = 0 }) * (1.6f * opacity), 10);
        Line(engine, Vector2.Lerp(engine, end, .65f), (White with { A = 0 }) * (1.35f * opacity), 4);
        for (int i = -1; i <= 1; i += 2)
        {
            Vector2 flare = Vector2.Lerp(engine, end, .78f) + normal * (i * (5 + MathF.Sin(time * .13f + i) * 3));
            Line(engine + normal * i * 3, flare, (tint with { A = 0 }) * (.65f * opacity), 3);
        }
    }
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
        color *= layerOpacity;
        Texture2D texture = Asset("ArknightsMod/Common/Particle/DefaultParticle");
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color with { A = 0 }, angle,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
    }
    internal static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        color *= layerOpacity;
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
    internal static void Ellipse(Vector2 center, float radius, float squash, float rotation, Color color, float width)
    {
        Vector2 last = center + new Vector2(radius, 0).RotatedBy(rotation);
        for (int i = 1; i <= 32; i++)
        {
            float a = i * MathHelper.TwoPi / 32;
            Vector2 next = center + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * squash).RotatedBy(rotation);
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
    internal static void Brackets(Vector2 center, Vector2 size, Color color, float opacity)
    {
        Vector2 half = size * .5f;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 p = center + half * new Vector2(x, y);
                float length = Math.Min(22, Math.Min(size.X, size.Y) * .28f);
                Line(p, p - new Vector2(x * length, 0), color * opacity, 3);
                Line(p, p - new Vector2(0, y * length), color * opacity, 3);
            }
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = center + half * (i switch { 0 => new Vector2(-1,-1), 1 => new Vector2(1,-1), 2 => Vector2.One, _ => new Vector2(-1,1) });
            Vector2 b = center + half * (((i + 1) % 4) switch { 0 => new Vector2(-1,-1), 1 => new Vector2(1,-1), 2 => Vector2.One, _ => new Vector2(-1,1) });
            Line(a, b, color * (opacity * .25f), 1);
            Line(a + (a - center).SafeNormalize(Vector2.UnitX) * 3, b + (b - center).SafeNormalize(Vector2.UnitX) * 3, Cyan * (opacity * .14f), 1);
        }
        float scan = (float)(Main.GlobalTimeWrappedHourly * .45f % 1);
        Line(center + new Vector2(-half.X, MathHelper.Lerp(-half.Y, half.Y, scan)),
            center + new Vector2(half.X, MathHelper.Lerp(-half.Y, half.Y, scan)), color * (opacity * .35f), 2);
    }
    internal static void Nano(Vector2 point, Vector2 velocity, Color color, float scale = 1, int life = 28)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= (Reduced ? 350 : 1400) ||
            Math.Abs(point.X - Main.screenPosition.X - Main.screenWidth * .5f) > Main.screenWidth * .5f + 300 ||
            Math.Abs(point.Y - Main.screenPosition.Y - Main.screenHeight * .5f) > Main.screenHeight * .5f + 300) return;
        int count = Main.rand.NextFloat() < .4f ? 2 : 1;
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < (Reduced ? 350 : 1400); i++)
            new MaterialistNanoParticle(point + (i == 0 ? Vector2.Zero : Main.rand.NextVector2Circular(3, 3)),
                velocity, color, scale, life, Main.rand.NextBool()).Spawn();
    }
    internal static void Burst(Vector2 point, Color color, float power = 1)
    {
        if (Main.dedServ) return;
        int count = Math.Min(Reduced ? 8 : 32, (int)(16 * power));
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1, 1) * Main.rand.NextFloat(2, 7) * power;
            Nano(point, velocity, i % 4 == 0 ? White : color, Main.rand.NextFloat(.8f, 1.8f), 28);
            if (i % 5 == 0) Dust.NewDustPerfect(point, DustID.Smoke, velocity * .25f, 120, default, .8f);
        }
    }
    internal static void UpdateBoss(MaterialistAntagonizer boss, int lastTimer)
    {
        if (boss.Order == FlightOrder.Death) return; // The persistent local death timeline owns all terminal cues.
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
        int activationCue = boss.Order == FlightOrder.Arrival ? 180 : 100;
        if (boss.Order is FlightOrder.Arrival or FlightOrder.Transition && lastTimer < activationCue && t >= activationCue)
        {
            Burst(npc.Center, boss.Phase == 3 ? Amber : Cyan, 2);
            MaterialistEffects.Pulse(npc.Center, 460, Amber, 4);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = .55f, Pitch = .35f }, npc.Center);
        }
        if (boss.Order == FlightOrder.Overload && lastTimer < 112 && t >= 112)
        {
            MaterialistEffects.Pulse(npc.Center, 300, Amber, 3);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = .6f }, npc.Center);
        }
        if (!Reduced && boss.Phase == 3 && t % 10 == 0)
            Dust.NewDustPerfect(npc.Center + new Vector2(85, 15), DustID.Smoke, new Vector2(-npc.velocity.X * .2f, -1.8f), 130, default, 1.1f);
        if (!Reduced && boss.Charging && t % 3 == 0) Burst(npc.Center - boss.DashDirection * 90, Amber, .35f);
        float engineStartup = boss.Order == FlightOrder.Arrival ? MaterialistFormation.Smooth((t - 164) / 46f) : 1;
        if (!Reduced && t % 3 == 0 && engineStartup > .05f)
            foreach (float x in new[] { -85f, 78f })
                Nano(npc.Center + new Vector2(x * 1.45f, 24), new Vector2(-npc.velocity.X * .2f, boss.Charging ? 8 : 4),
                    EngineColor(boss.Phase, boss.CoreExposure > 0 ? FlightOrder.Recover : boss.Order, boss.Shield), .75f * engineStartup, 16);
    }
    internal static void DrawBoss(MaterialistAntagonizer boss, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        NPC npc = boss.NPC;
        Texture2D body = Asset(MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer");
        if (boss.Order == FlightOrder.Death)
        {
            DrawDeathChassis(boss, body, batch, screenPos, drawColor);
            DrawLockShield(boss);
            return;
        }
        float time = (float)Main.GlobalTimeWrappedHourly * 60;
        float opacity = boss.Order == FlightOrder.Arrival ? MaterialistFormation.Smooth((boss.Timer - 105) / 65f) : 1;
        float engineOpacity = boss.Order == FlightOrder.Arrival ? MaterialistFormation.Smooth((boss.Timer - 164) / 46f) : opacity;
        float rimOpacity = boss.Order == FlightOrder.Arrival ? MaterialistFormation.Smooth((boss.Timer - 140) / 48f) : opacity;
        Color hullTint = EngineColor(boss.Phase, boss.CoreExposure > 0 ? FlightOrder.Recover : boss.Order, boss.Shield);
        Color accent = boss.Phase == 3 ? Amber : Cyan;
        float scale = 1.45f;
        Vector2 position = npc.Center;
        Vector2 core = position + new Vector2(4, 31).RotatedBy(npc.rotation) * scale;
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
            EngineFlame(engine, npc.velocity, npc.rotation, boss.Charging,
                hullTint, time, engineOpacity);
        }
        if (boss.Order == FlightOrder.Arrival) DrawArrival(boss, body, position, scale, opacity);
        if (rimOpacity > 0 && !MaterialistTechRenderer.ChassisMaterial(body, position, null, npc.rotation, new Vector2(scale),
            SpriteEffects.None, hullTint * (.85f * rimOpacity), 4))
            for (int i = 0; i < 8; i++)
                batch.Draw(body, position - screenPos + (i * MathHelper.TwoPi / 8).ToRotationVector2() * 2.5f, null,
                    (hullTint with { A = 0 }) * (.65f * rimOpacity), npc.rotation, body.Size() * .5f, scale, SpriteEffects.None, 0);
        batch.Draw(body, position - screenPos, null, Color.Lerp(drawColor, Color.White, .65f) * opacity,
            npc.rotation, body.Size() * .5f, scale, SpriteEffects.None, 0);
        if (boss.Phase >= 3)
            MaterialistTechRenderer.OverclockChassis(body, position, null, npc.rotation, new Vector2(scale), SpriteEffects.None, opacity * .65f);
        Glow(core, (boss.Order == FlightOrder.Recover ? Cyan : Amber) * (.8f * opacity), new Vector2(34 + MathF.Sin(time * .10f) * 5));
        DrawCoreTelegraph(boss);
        DrawSupport(boss);
        DrawLockShield(boss);
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
                if (escort.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(boss) && !wing.Retreating &&
                    MaterialistRules.Duty(boss.Phase, wing.Slot) == WingDuty.Containment)
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
                Brackets(bay, new Vector2(100, 65), Cyan, .8f);
                Line(bay - new Vector2(32, 18), bay + new Vector2(32, -18), White * .45f, 5);
                Line(bay - new Vector2(32, -18), bay + new Vector2(32, 18), Cyan * .45f, 2);
                Line(bay, bay + new Vector2(side * 180, 60), Cyan * .25f, 2);
            }
        }
        if (boss.Order == FlightOrder.Ram && boss.Timer is >= 114 and < 170)
        {
            Vector2 normal = boss.DashDirection.RotatedBy(MathHelper.PiOver2) * 65;
            Vector2 end = position + boss.DashDirection * 750;
            Line(position - normal, end - normal, Amber * .55f, 2);
            Line(position + normal, end + normal, Amber * .55f, 2);
            Line(position, end, White * .45f, 2);
            Reticle(boss.Aim, 24, Amber, time);
        }
        if (boss.EscortFire)
        {
            if (boss.Timer < 80)
                Brackets(boss.FormationCenter, new Vector2(180 + boss.Timer * 3, 120 + boss.Timer * 2), Cyan, .55f);
        }
        if (boss.Order == FlightOrder.Overload && boss.Timer is >= 40 and < 112)
        {
            float angle = (boss.Aim - position).ToRotation();
            for (int i = 0; i < MaterialistRules.SpreadRingCount(20); i++)
            {
                float a = i * MathHelper.TwoPi / MaterialistRules.SpreadRingCount(20);
                if (Math.Abs(MathHelper.WrapAngle(a - angle)) < .56f) continue;
                Vector2 d = a.ToRotationVector2();
                Line(position + d * 135, position + d * 240, Amber * .45f, 2);
            }
            for (int sign = -1; sign <= 1; sign += 2)
                Line(position, position + (angle + sign * .56f).ToRotationVector2() * 500, Cyan * .45f, 2);
        }
    }
    internal static void DrawHazard(MaterialistHazard h)
    {
        float previous = layerOpacity;
        layerOpacity *= h.Visibility;
        try { DrawHazardLayers(h); }
        finally { layerOpacity = previous; }
    }
    private static void DrawHazardLayers(MaterialistHazard h)
    {
        Projectile p = h.Projectile;
        Color color = AmmoColor(h);
        if (DrawSpecialAmmo(h, color)) return;
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
        if (h.IsBeam) { DrawNativeBeam(h, color); return; }
        for (int i = p.oldPos.Length - 1; i >= 1; i--)
            if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
                Line(p.oldPos[i] + p.Size * .5f, p.oldPos[i - 1] + p.Size * .5f, (color with { A = 0 }) * ((1 - i / (float)p.oldPos.Length) * .7f), 5 * (1 - i / (float)p.oldPos.Length));
        Glow(p.Center, color * .6f, new Vector2(32, 20), p.rotation);
        Line(p.Center - p.velocity.SafeNormalize(Vector2.UnitX) * 9, p.Center + p.velocity.SafeNormalize(Vector2.UnitX) * 6, color, 7);
        Glow(p.Center, White, new Vector2(10));
    }
}
