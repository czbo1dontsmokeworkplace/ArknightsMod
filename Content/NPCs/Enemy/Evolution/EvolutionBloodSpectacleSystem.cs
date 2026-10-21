using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal enum EvolutionBloodCue : byte { Volley, Dash, Impact, Axis, Morph, Finale, Weave, Echo, Rupture }

internal static class EvolutionBloodSpectacleRules
{
    internal const int ParticleCapacity = 1152, SplatCapacity = 80, WaveCapacity = 24;
    internal static bool Terminal(EvolutionBloodCue cue) => cue is EvolutionBloodCue.Finale or EvolutionBloodCue.Rupture;
    internal static int ParticleLimit(bool reduced, bool terminal = false) => terminal ? reduced ? 224 : ParticleCapacity : reduced ? 112 : 768;
    internal static int DecorationCount(int phase, bool reduced) => phase == 6 ? reduced ? 32 : 256 : reduced ? 16 : phase >= 4 ? 128 : 80;
    internal static int EmissionCount(EvolutionBloodCue cue, bool reduced) => reduced ? Terminal(cue) ? 14 : 6 : cue switch
    {
        EvolutionBloodCue.Finale => 220, EvolutionBloodCue.Rupture => 40,
        EvolutionBloodCue.Morph => 102, EvolutionBloodCue.Volley => 16, _ => 52
    };
    internal static int Filaments(EvolutionBloodCue cue, bool reduced) => Terminal(cue) ? reduced ? 12 : 48 : reduced ? 6 : 20;
    internal const int FinaleLife = 96;
    internal static float Envelope(float age, float life) => Math.Clamp(age / 4, 0, 1) * MathF.Pow(Math.Max(0, 1 - age / life), 1.5f);
    internal static float ClearPlayer(float distance) => EvolutionCinematicFrame.Smooth(110, 230, distance);
    internal static float Shake(EvolutionBloodCue cue) => cue switch
    {
        EvolutionBloodCue.Finale => 9, EvolutionBloodCue.Rupture => 3, EvolutionBloodCue.Morph or EvolutionBloodCue.Axis => 7,
        EvolutionBloodCue.Impact => 6, EvolutionBloodCue.Dash => 4, _ => 0
    };
}

// Local-only scenery: none of these objects is a Projectile, NPC or a collision shape.
// Liquid flecks use drag and gravity, then settle into short-lived surface stains.
// Dark liquid bodies, highlights and embers share Evolution's existing local materials.
public sealed class EvolutionBloodSpectacleSystem : ModSystem
{
    private struct Fleck
    {
        internal Vector2 Position, Velocity;
        internal Color Theme;
        internal float Size, Rotation, Spin, SpatialScale;
        internal int Age, Life;
        internal byte Kind; // 0 liquid, 1 ember, 2 smoke, 3 blade afterimage
    }
    private struct Splat { internal Vector2 Position; internal Color Theme; internal float Size, Rotation; internal int Age, Life; }
    private struct Wave
    {
        internal Vector2 Center, Direction;
        internal Color Theme;
        internal EvolutionBloodCue Cue;
        internal float Power, SpatialScale;
        internal int Age, Life;
    }
    private static readonly Fleck[] flecks = new Fleck[EvolutionBloodSpectacleRules.ParticleCapacity];
    private static readonly Splat[] splats = new Splat[EvolutionBloodSpectacleRules.SplatCapacity];
    private static readonly Wave[] waves = new Wave[EvolutionBloodSpectacleRules.WaveCapacity];
    // Never consume Main.rand: decorative detail cannot change server attack randomness.
    private static readonly UnifiedRandom random = new(742918);
    private static int fleckCursor, splatCursor, waveCursor, emissionCount;
    private static ulong emissionTick;
    internal static int ActiveFlecks
    {
        get { int count = 0; foreach (var p in flecks) if (p.Life > 0) count++; return count; }
    }
    internal static void Emit(Vector2 center, Vector2 direction, EvolutionBloodCue cue, Color theme, float power = 1, float spatialScale = 1)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, center) > 3000 * 3000) return;
        if (emissionTick != Main.GameUpdateCount) { emissionTick = Main.GameUpdateCount; emissionCount = 0; }
        if (++emissionCount > 4) return;
        direction = direction.SafeNormalize(Vector2.UnitY);
        power = Math.Clamp(power, .3f, 2.2f);
        spatialScale = Math.Clamp(spatialScale, 1, 2);
        bool terminal = EvolutionBloodSpectacleRules.Terminal(cue);
        int life = cue == EvolutionBloodCue.Finale ? EvolutionBloodSpectacleRules.FinaleLife : cue == EvolutionBloodCue.Morph ? 76 : cue == EvolutionBloodCue.Volley ? 26 : 48;
        waves[waveCursor++ % waves.Length] = new Wave { Center = center, Direction = direction, Theme = theme, Cue = cue, Power = power, Life = life, SpatialScale = spatialScale };
        bool reduced = EvolutionCinematics.Reduced;
        int count = EvolutionBloodSpectacleRules.EmissionCount(cue, reduced);
        for (int i = 0; i < count; i++)
        {
            bool radial = cue is EvolutionBloodCue.Morph or EvolutionBloodCue.Echo || terminal;
            // Combat bursts resolve into evenly spaced ribs. Terminal rupture is deliberately turbulent.
            float offset = terminal ? random.NextFloat(-MathHelper.Pi, MathHelper.Pi) :
                radial ? i * MathHelper.TwoPi / count : (i % 13 / 12f - .5f) * 1.5f;
            Vector2 travel = direction.RotatedBy(offset);
            float speed = (terminal ? random.NextFloat(4, 22) : 5 + i % 3 * 3 + random.NextFloat(-.5f, .5f)) * power * spatialScale;
            byte kind = (byte)(i % 11 == 0 ? 2 : i % 7 == 0 ? 3 : i % 4 == 0 ? 1 : 0);
            flecks[fleckCursor++ % EvolutionBloodSpectacleRules.ParticleLimit(reduced, terminal)] = new Fleck
            {
                Position = center + travel * ((terminal ? random.NextFloat(6, 48) : 18 + i % 3 * 14) * spatialScale), Velocity = travel * speed,
                Theme = theme, Kind = kind, Size = random.NextFloat(terminal ? .8f : .85f, terminal ? 1.9f : 1.35f) * power, SpatialScale = spatialScale,
                Rotation = travel.ToRotation(), Spin = random.NextFloat(-.025f, .025f), Life = kind == 2 ? 60 : random.Next(26, 65)
            };
        }
        float shake = EvolutionBloodSpectacleRules.Shake(cue);
        if (shake > 0) EvolutionImpactSystem.Emit(center, (cue == EvolutionBloodCue.Finale ? 1250 : 540 * power) * spatialScale, shake, false, 30);
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool reduced = EvolutionCinematics.Reduced;
        bool terminal = false;
        foreach (Wave wave in waves) if (wave.Life > 0 && EvolutionBloodSpectacleRules.Terminal(wave.Cue)) { terminal = true; break; }
        int limit = EvolutionBloodSpectacleRules.ParticleLimit(reduced, terminal);
        for (int i = 0; i < flecks.Length; i++)
        {
            ref Fleck p = ref flecks[i];
            if (i >= limit || p.Life <= 0) { p.Life = 0; continue; }
            if (++p.Age >= p.Life) { p.Life = 0; continue; }
            Vector2 oldPosition = p.Position;
            p.Velocity *= p.Kind == 2 ? .94f : p.Kind == 3 ? .97f : .982f;
            p.Velocity.Y += (p.Kind == 0 ? .24f : p.Kind == 2 ? -.07f : .01f) * p.SpatialScale;
            p.Position += p.Velocity;
            p.Rotation += p.Spin;
            if (p.Kind == 2) p.Size *= 1.013f;
            if (p.Kind == 0 && p.Age > 3 && WorldGen.InWorld((int)p.Position.X / 16, (int)p.Position.Y / 16, 2) &&
                Collision.SolidCollision(p.Position - new Vector2(2), 4, 4))
            {
                splats[splatCursor++ % (reduced ? 12 : splats.Length)] = new Splat
                {
                    Position = oldPosition, Size = p.Size * (1 + Math.Min(p.Velocity.Length() * .05f, .8f)),
                    Rotation = Math.Clamp(p.Velocity.X * .03f, -.3f, .3f), Theme = p.Theme, Life = random.Next(80, 140)
                };
                p.Life = 0;
            }
        }
        for (int i = 0; i < splats.Length; i++) if (splats[i].Life > 0 && ++splats[i].Age >= splats[i].Life) splats[i].Life = 0;
        for (int i = 0; i < waves.Length; i++) if (waves[i].Life > 0 && ++waves[i].Age >= waves[i].Life) waves[i].Life = 0;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool active = ActiveFlecks > 0;
        foreach (var wave in waves) active |= wave.Life > 0;
        foreach (var splat in splats) active |= splat.Life > 0;
        foreach (NPC npc in Main.ActiveNPCs) if (npc.ModNPC is Evolution) { active = true; break; }
        if (!active) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try { DrawScene(); }
        finally { Main.spriteBatch.End(); }
    }
    internal static void DrawScene()
    {
        // Draw below enemies, players, hostile shots and their warning lines.
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Evolution boss && npc.DistanceSQ(Main.LocalPlayer.Center) < 3200 * 3200) DrawBarrage(boss);
        foreach (var p in splats) if (p.Life > 0) DrawSplat(p);
        foreach (var p in waves) if (p.Life > 0) DrawWave(p);
        foreach (var p in flecks) if (p.Life > 0) DrawFleck(p);
    }
    private static bool Visible(Vector2 at, float padding = 180) => Math.Abs(at.X - Main.screenPosition.X - Main.screenWidth * .5f) < Main.screenWidth / Main.GameViewMatrix.Zoom.X * .5f + padding &&
        Math.Abs(at.Y - Main.screenPosition.Y - Main.screenHeight * .5f) < Main.screenHeight / Main.GameViewMatrix.Zoom.Y * .5f + padding;
    private static void Blob(Vector2 at, Vector2 size, Color color, float rotation = 0)
    {
        Texture2D bloom = EvolutionVisuals.Asset("Bloom");
        Main.spriteBatch.Draw(bloom, at - Main.screenPosition, null, color, rotation, bloom.Size() * .5f, size / bloom.Size(), SpriteEffects.None, 0);
    }
    private static void DrawFleck(Fleck p)
    {
        if (!Visible(p.Position)) return;
        float fade = EvolutionBloodSpectacleRules.Envelope(p.Age, p.Life);
        // Decorative liquid is dimmer than the bright cores that signal real damage.
        fade *= MathHelper.Lerp(.22f, 1, EvolutionBloodSpectacleRules.ClearPlayer(Vector2.Distance(p.Position, Main.LocalPlayer.Center)));
        Color dark = Color.Lerp(new Color(32, 0, 7), p.Theme, .32f);
        float speed = p.Velocity.Length(), angle = p.Velocity.ToRotation();
        if (p.Kind == 2)
        {
            Texture2D smoke = EvolutionVisuals.Asset("ArsenalSmoke");
            int variant = (int)(Math.Abs(p.Spin) * 120) % 7;
            Rectangle frame = new(variant * 80, Math.Min(5, p.Age * 6 / p.Life) * 80, 80, 80);
            Main.spriteBatch.Draw(smoke, p.Position - Main.screenPosition, frame, dark * (.32f * fade), p.Rotation,
                frame.Size() * .5f, new Vector2(100, 70) * p.Size / frame.Size(), SpriteEffects.None, 0);
        }
        else if (p.Kind == 0)
        {
            Vector2 size = new Vector2(9 + Math.Min(speed * 1.4f, 30), 8) * p.Size;
            Blob(p.Position, size * 1.3f, new Color(32, 0, 5) * (.8f * fade), angle);
            Blob(p.Position, size, Color.Lerp(dark, p.Theme, .55f) * fade, angle);
            EvolutionVisuals.Glow(p.Position + new Vector2(-1, -1) * p.Size, EvolutionVisuals.Core * (.28f * fade), new Vector2(3, 2) * p.Size);
        }
        else
        {
            float length = p.Kind == 3 ? 95 * p.Size * (1 - p.Age / (float)p.Life) : Math.Min(40, 10 + speed * 2) * p.Size;
            EvolutionArsenalVisuals.Spark(p.Position - angle.ToRotationVector2() * length, p.Position,
                (p.Kind == 3 ? 4.5f : 1.5f) * p.Size, p.Theme * (.78f * fade));
            if (p.Kind == 1) EvolutionVisuals.Glow(p.Position, EvolutionVisuals.Core * (.45f * fade), new Vector2(5) * p.Size);
        }
    }
    private static void DrawSplat(Splat p)
    {
        if (!Visible(p.Position)) return;
        float fade = EvolutionBloodSpectacleRules.Envelope(p.Age, p.Life);
        Blob(p.Position, new Vector2(38, 11) * p.Size, new Color(25, 0, 6) * (.6f * fade), p.Rotation);
        Blob(p.Position, new Vector2(30, 8) * p.Size, Color.Lerp(new Color(40, 0, 6), p.Theme, .32f) * (.65f * fade), p.Rotation);
        EvolutionVisuals.Glow(p.Position + new Vector2(-2, -1), p.Theme * (.18f * fade), new Vector2(10, 2) * p.Size);
    }
    private static void DrawWave(Wave p)
    {
        float u = p.Age / (float)p.Life, fade = MathF.Pow(1 - u, 2);
        bool terminal = EvolutionBloodSpectacleRules.Terminal(p.Cue);
        float reach = (p.Cue == EvolutionBloodCue.Finale ? 1550 : p.Cue == EvolutionBloodCue.Morph ? 1100 : p.Cue == EvolutionBloodCue.Axis ? 900 : 540) * p.Power;
        float radius = (35 + reach * (1 - MathF.Pow(1 - u, 3))) * .7f * p.SpatialScale;
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        float alpha = fade * (EvolutionCinematics.Reduced ? .22f : terminal ? .66f : .58f);
        int rings = p.Cue is EvolutionBloodCue.Dash or EvolutionBloodCue.Volley ? 1 : terminal ? 4 : 2;
        Vector2 ringShape = p.Cue == EvolutionBloodCue.Dash ? new Vector2(2.8f, .65f) :
            p.Cue == EvolutionBloodCue.Impact ? new Vector2(2.6f, .5f) : new Vector2(2, 1.6f);
        float ringAngle = p.Cue == EvolutionBloodCue.Impact ? 0 : p.Direction.ToRotation();
        for (int i = 0; i < rings; i++)
            Main.spriteBatch.Draw(ring, p.Center - Main.screenPosition, null, (p.Theme with { A = 0 }) * (alpha / (i + 1)),
                ringAngle + (rings == 1 ? 0 : u * (i % 2 == 0 ? .6f : -.4f)), ring.Size() * .5f,
                ringShape * radius * (1 + i * .12f) / ring.Size(), SpriteEffects.None, 0);
        if (p.Cue == EvolutionBloodCue.Volley) return;
        int count = EvolutionBloodSpectacleRules.Filaments(p.Cue, EvolutionCinematics.Reduced);
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathHelper.TwoPi / count + p.Direction.ToRotation();
            if (terminal) angle += .22f * MathF.Sin(i * 2.31f + u * 7);
            if (p.Cue == EvolutionBloodCue.Dash) angle = p.Direction.ToRotation() + (i / (float)(count - 1) - .5f) * 1.3f;
            if (p.Cue == EvolutionBloodCue.Impact) angle = -MathHelper.Pi + (i + .5f) / count * MathHelper.Pi;
            Vector2 d = angle.ToRotationVector2();
            Vector2 from = p.Center + d * (radius * (terminal ? .35f + .2f * MathF.Sin(i * 4.7f) : .45f));
            Vector2 to = p.Center + d * (radius * (1 + (terminal ? .18f : .035f) * MathF.Cos(i * 3.1f)));
            if (p.Cue == EvolutionBloodCue.Dash) to += d * radius * .8f;
            if (p.Cue == EvolutionBloodCue.Weave)
            {
                // A liquid curtain, with uneven filaments curling in the rise/fall direction.
                Vector2 normal = p.Direction.RotatedBy(MathHelper.PiOver2);
                from = p.Center + normal * ((i / (float)(count - 1) - .5f) * radius * 2);
                to = from + p.Direction * radius * (.7f + .12f * MathF.Cos(i * MathHelper.Pi / 4));
            }
            float clear = EvolutionBloodSpectacleRules.ClearPlayer(Vector2.Distance((from + to) * .5f, Main.LocalPlayer.Center));
            Vector2 bend = (from + to) * .5f + d.RotatedBy(MathHelper.PiOver2) * (MathF.Sin(i * MathHelper.Pi / 4 + u * 4) * radius * (terminal ? .12f : .025f));
            float width = (4 + 7 * (1 - u)) * p.Power;
            EvolutionArsenalVisuals.Spark(from, bend, width, p.Theme * (alpha * clear * .75f));
            EvolutionArsenalVisuals.Spark(bend, to, width * .6f, p.Theme * (alpha * clear));
        }
    }
    private static void DrawBarrage(Evolution boss)
    {
        if (boss.Phase < 2 || boss.Timer < 0) return;
        float clock = boss.NPC.ai[3];
        float intensity = boss.Transitioning ? .65f : boss.Phase == 6 ? 1.2f : boss.Charging || boss.AxisHealthLocked ? .7f : .34f;
        intensity *= EvolutionCinematics.Reduced ? .35f : 1;
        if (boss.Phase == 2) intensity *= EvolutionPresentationRules.MorphColor(boss.Timer);
        Vector2 center = boss.PatternSerial == boss.Serial ? boss.PatternCenter : boss.NPC.Center;
        if (boss.Transitioning || boss.Phase == 6) center = boss.NPC.Center;
        int count = EvolutionBloodSpectacleRules.DecorationCount(boss.Phase, EvolutionCinematics.Reduced);
        // Deterministic world-space streams: no player tracking and no hostile-projectile slots.
        for (int i = 0; i < count; i++)
        {
            bool terminal = boss.Phase == 6;
            int spoke = i % 32, row = i / 16;
            float seed = terminal ? i * 2.399963f : spoke * MathHelper.TwoPi / 32;
            float cycle = terminal ? (clock * (.008f + i % 5 * .0008f) + i * .618034f) % 1 :
                (clock * .0065f + row / (count / 16f) + i % 2 * .045f) % 1;
            Vector2 from, to;
            if (boss.AxisHealthLocked || boss.Transitioning || boss.Phase == 6)
            {
                Vector2 d = seed.ToRotationVector2();
                float radius = 180 + cycle * 1350;
                from = center + d * radius;
                to = from - d * (25 + cycle * 90);
            }
            else
            {
                float x = (i % 16 - 7.5f) * 168;
                float y = boss.CorridorActive ? MathHelper.Lerp(boss.CorridorTop - 120, boss.CorridorBottom + 120, cycle) : center.Y - 700 + cycle * 1400;
                from = new Vector2(center.X + x + MathF.Sin(clock * .012f + row) * 8, y);
                to = from + new Vector2(4 * MathF.Sin(clock * .01f + row), 24 + row % 3 * 6);
            }
            if (!Visible(from)) continue;
            float clear = EvolutionBloodSpectacleRules.ClearPlayer(Vector2.Distance(from, Main.LocalPlayer.Center));
            float fade = MathF.Sin(cycle * MathHelper.Pi) * clear * intensity;
            EvolutionArsenalVisuals.Spark(from, to, 2 + i % 3, boss.ThemeColor * (.72f * fade));
            if (i % 5 == 0) Blob(from, new Vector2(9, 5), new Color(80, 0, 17) * (.5f * fade));
        }
    }
    public override void OnWorldUnload()
    {
        Array.Clear(flecks); Array.Clear(splats); Array.Clear(waves);
        fleckCursor = splatCursor = waveCursor = emissionCount = 0; emissionTick = 0;
    }
    public override void Unload() => OnWorldUnload();
}
