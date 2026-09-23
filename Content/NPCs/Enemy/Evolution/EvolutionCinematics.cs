using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Decorative "barrages" are deterministic draw samples, not Projectiles or NPCs.
// They have no collision hooks, damage, packets or access to gameplay RNG.
internal static class EvolutionCinematics
{
    private static readonly Color Wine = new(134, 26, 58);
    private static readonly Color Rose = new(243, 116, 125);
    private static readonly Color Ivory = new(255, 203, 171);
    internal static bool Reduced => ModContent.GetInstance<EvolutionCinematicConfig>()?.ReducedEffects ?? false;
    internal static bool ShakeEnabled => ModContent.GetInstance<EvolutionCinematicConfig>()?.ScreenShake ?? true;

    internal static bool TryFrame(Evolution boss, out EvolutionCinematicFrame frame) =>
        boss.Cinema.TryFrame(boss.Phase, EvolutionRules.CinematicTime(boss.Phase, boss.Timer), (int)boss.NPC.ai[3], boss.NPC.Center, out frame);

    internal static void Update(Evolution boss)
    {
        if (Main.dedServ) return;
        ref EvolutionCinematicPlayback playback = ref boss.Cinema;
        int time = EvolutionRules.CinematicTime(boss.Phase, boss.Timer);
        playback.Update(boss.Phase, time, (int)boss.NPC.ai[3], boss.NPC.Center);
        if (boss.Transitioning)
        {
            bool nearby = Vector2.DistanceSquared(Main.LocalPlayer.Center, boss.NPC.Center) < 2600 * 2600;
            // Sparse, increasingly urgent heartbeats. Net corrections cannot retrigger a cue.
            for (int i = 0; i < 7; i++)
            {
                int beat = i < 5 ? 65 + i * 84 : i == 5 ? 465 : 526;
                if (playback.Cue(time, beat, i) && nearby)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = .32f + i * .035f, Pitch = -.65f + i * .055f, MaxInstances = 1 }, boss.NPC.Center);
                    EvolutionImpactSystem.Emit(boss.NPC.Center, 720 + i * 35, 1.5f, true, 35);
                }
            }
            if (playback.Cue(time, EvolutionCinematicFrame.BurstTime, 7) && nearby)
            {
                EvolutionImpactSystem.Emit(boss.NPC.Center, 1100, boss.Phase == 4 ? 10 : 8, false, 45);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .72f, Pitch = -.45f, MaxInstances = 1 }, boss.NPC.Center);
                SoundEngine.PlaySound(SoundID.Roar with { Volume = .48f, Pitch = boss.Phase == 4 ? -.7f : -.4f, MaxInstances = 1 }, boss.NPC.Center);
            }
        }
        playback.LastTimer = time;
    }

    // Called in PostDrawTiles, before combat projectiles and players: real telegraphs stay on top.
    internal static void DrawBackdrop(EvolutionCinematicFrame f, bool reduced)
    {
        float visibility = f.Entrance * f.Fade * (reduced ? .55f : 1f);
        if (visibility <= .001f) return;
        float preBurst = 1 - EvolutionCinematicFrame.Smooth(EvolutionCinematicFrame.BurstTime, 592, f.Time);
        EvolutionVisuals.Glow(f.Center, Wine * ((.32f + f.Charge * .25f) * visibility), new Vector2(760, 600) * f.Scale);
        DrawMembranes(f, visibility, preBurst);
        DrawInflow(f, visibility * preBurst, reduced);
        DrawShell(f, visibility, reduced);
        if (f.Perfect) DrawCrown(f, visibility);
        DrawEruption(f, visibility, reduced);
        // Tight exposure around the heart, never a full-screen whiteout.
        EvolutionVisuals.Glow(f.Center, Rose * (visibility * (.12f + f.Charge * .32f + f.Burst * .4f)), new Vector2(200 + f.Charge * 90 + f.Burst * 260));
        EvolutionVisuals.Glow(f.Center, Ivory * (visibility * (f.Charge * .24f + f.Burst * .4f)), new Vector2(90 + f.Burst * 110));
    }

    private static void Ring(Vector2 center, Vector2 dimensions, Color color, float rotation)
    {
        Texture2D texture = EvolutionVisuals.Asset("CinematicRing");
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color with { A = 0 }, rotation,
            texture.Size() * .5f, dimensions / texture.Size(), SpriteEffects.None, 0);
    }

    private static void SoftLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        EvolutionVisuals.Glow((start + end) * .5f, color, new Vector2(delta.Length() + width, width), delta.ToRotation());
    }

    private static void DrawMembranes(EvolutionCinematicFrame f, float visibility, float preBurst)
    {
        for (int i = 0; i < 3; i++)
        {
            float breathing = MathF.Sin(f.Time * (.027f + i * .005f)) * 20;
            float width = (400 + i * 110 + breathing) * (1 - f.Charge * .33f) * f.Scale;
            Vector2 dimensions = new(width, width * (f.Perfect ? .43f : .65f));
            Ring(f.Center, dimensions, (i == 1 ? Rose : Wine) * (visibility * preBurst * .27f), f.Time * (i % 2 == 0 ? .003f : -.004f) + i * 1.3f);
        }
        // Curved vascular filaments, not straight lines that could be mistaken for laser warnings.
        int arms = f.Perfect ? 8 : 6;
        float expansion = 165 + f.Charge * 120;
        for (int arm = 0; arm < arms; arm++)
        {
            float angle = arm * MathHelper.TwoPi / arms + f.Time * .003f;
            Vector2 previous = f.Center;
            for (int segment = 1; segment <= 18; segment++)
            {
                float u = segment / 18f;
                float bend = MathF.Sin(u * 4 + f.Time * .018f + arm) * .36f;
                Vector2 point = f.Center + (angle + bend).ToRotationVector2() * u * expansion * new Vector2(1, .8f);
                SoftLine(previous, point, Wine * (visibility * preBurst * (1 - u) * .48f), 14 * (1 - u) + 5);
                previous = point;
            }
        }
    }

    private static void DrawInflow(EvolutionCinematicFrame f, float visibility, bool reduced)
    {
        if (visibility <= .01f) return;
        int count = f.MoteCount(reduced);
        for (int i = 0; i < count; i++)
        {
            Vector2 point = f.Inflow(i, f.Time);
            float distance = Vector2.Distance(point, f.Center);
            float alpha = visibility * EvolutionCinematicFrame.Smooth(20, 95, distance) * (1 - EvolutionCinematicFrame.Smooth(700, 1120, distance));
            if (alpha < .01f || !OnScreen(point, 110)) continue;
            Color color = i % 5 == 0 ? Ivory : i % 2 == 0 ? Rose : Wine;
            Vector2 previous = point;
            for (int k = 1; k <= (reduced ? 2 : 4); k++)
            {
                Vector2 older = f.Inflow(i, f.Time - k * 1.4f);
                if (Vector2.DistanceSquared(older, previous) > 10000) break; // do not draw across a wrapped lifetime
                SoftLine(previous, older, color * (alpha * (.42f - k * .07f)), 10 - k);
                previous = older;
            }
            // Hollow, four-point cells deliberately differ from solid hostile blood spirits.
            float size = 3 + EvolutionCinematicFrame.Hash(i + 27) * 3;
            Vector2 right = new(size, 0), down = new(0, size * .65f);
            SoftLine(point - right, point - down, color * (alpha * .7f), 4);
            SoftLine(point - down, point + right, color * (alpha * .7f), 4);
            SoftLine(point + right, point + down, color * (alpha * .7f), 4);
            SoftLine(point + down, point - right, color * (alpha * .7f), 4);
        }
    }

    private static void DrawShell(EvolutionCinematicFrame f, float visibility, bool reduced)
    {
        Texture2D shell = EvolutionVisuals.Asset(f.Perfect ? "ShellFragment" : "WhiteShell");
        int count = reduced ? 8 : 16;
        float separation = EvolutionCinematicFrame.Smooth(18, 170, f.Time);
        float flight = Math.Max(0, f.Time - EvolutionCinematicFrame.BurstTime);
        for (int i = 0; i < count; i++)
        {
            float hash = EvolutionCinematicFrame.Hash(i + 153);
            float angle = i * MathHelper.TwoPi / count + f.Time * .0015f;
            float radius = (84 + separation * (75 + hash * 75)) * (1 - f.Charge * .45f) + flight * (5 + hash * 6);
            Vector2 point = f.Center + angle.ToRotationVector2() * new Vector2(radius, radius * .7f);
            if (!OnScreen(point, 80)) continue;
            Rectangle fragment = f.Perfect ? shell.Bounds : new Rectangle(i % 2 * 2 * (shell.Width / 3), i / 2 % 2 * (shell.Height / 3), shell.Width / 3, shell.Height / 3);
            float size = 22 + hash * 32;
            Color color = Color.Lerp(new Color(158, 105, 112), Ivory, f.Charge * .55f) * (visibility * .72f);
            Main.spriteBatch.Draw(shell, point - Main.screenPosition, fragment, color, angle + flight * .018f,
                fragment.Size() * .5f, size / Math.Max(fragment.Width, fragment.Height), SpriteEffects.None, 0);
            EvolutionVisuals.Glow(point, Rose * (visibility * (.08f + f.Charge * .2f)), new Vector2(size * 2));
        }
    }

    private static void DrawCrown(EvolutionCinematicFrame f, float visibility)
    {
        float emerge = EvolutionCinematicFrame.Smooth(280, 500, f.Time);
        float unfurl = EvolutionCinematicFrame.Smooth(550, 602, f.Time);
        float reach = MathHelper.Lerp(180 + emerge * 90, 540, unfurl);
        float alpha = visibility * emerge * .48f;
        if (alpha <= .01f) return;
        for (int petal = 0; petal < 10; petal++)
        {
            float angle = petal * MathHelper.TwoPi / 10 - MathHelper.PiOver2 + f.Time * .0007f;
            Vector2 previous = f.Center;
            for (int k = 1; k <= 22; k++)
            {
                float u = k / 22f;
                float curl = MathF.Sin(u * MathHelper.Pi) * (1.15f - unfurl * .7f);
                Vector2 point = f.Center + (angle + curl).ToRotationVector2() * reach * u * new Vector2(1, .78f);
                SoftLine(previous, point, Wine * alpha, (1 - u) * 34 + 7);
                SoftLine(previous, point, Rose * (alpha * .65f), (1 - u) * 7 + 2);
                previous = point;
            }
            EvolutionVisuals.Glow(previous, Ivory * (alpha * .45f), new Vector2(26));
        }
    }

    private static void DrawEruption(EvolutionCinematicFrame f, float visibility, bool reduced)
    {
        float age = f.Time - EvolutionCinematicFrame.BurstTime;
        if (age < 0) return;
        for (int i = 0; i < 3; i++)
        {
            float ringAge = age - i * 9;
            if (ringAge < 0) continue;
            float alpha = (1 - EvolutionCinematicFrame.Smooth(12, 88, ringAge)) * visibility;
            float diameter = 90 + MathF.Pow(ringAge / 90f, .75f) * (f.Perfect ? 2050 : 1700);
            Ring(f.Center, new Vector2(diameter, diameter * (i == 1 ? .42f : .72f)),
                (i == 0 ? Rose : Wine) * (alpha * .34f), i * .7f);
        }
        int count = reduced ? 32 : f.Perfect ? 128 : 96;
        for (int i = 0; i < count; i++)
        {
            float localAge = age - i % 13;
            if (localAge < 0) continue;
            float alpha = visibility * (1 - EvolutionCinematicFrame.Smooth(18, 90, localAge));
            Vector2 point = f.Outflow(i, localAge);
            if (alpha < .01f || !OnScreen(point, 140)) continue;
            Vector2 older = f.Outflow(i, Math.Max(0, localAge - 4));
            SoftLine(older, point, (i % 5 == 0 ? Ivory : Rose) * (alpha * .52f), i % 3 == 0 ? 13 : 7);
        }
        // A broad horizontal lens flare, kept behind entities with no white screen overlay.
        EvolutionVisuals.Glow(f.Center, Ivory * (visibility * f.Burst * .45f), new Vector2(960 * f.Scale, 42));
        EvolutionVisuals.Glow(f.Center, Rose * (visibility * f.Burst * .35f), new Vector2(120, 600 * f.Scale));
    }

    internal static void DrawBodyAura(Evolution boss, Texture2D texture, Vector2 origin, Vector2 scale, SpriteEffects flip)
    {
        if (!boss.Transitioning || !TryFrame(boss, out var f)) return;
        float brightness = (.06f + f.Charge * .2f + f.Burst * .25f) * f.Entrance * (1 - BodyDissolve(boss)) * (Reduced ? .5f : 1);
        float radius = 3 + f.Charge * 5 + f.Burst * 9;
        for (int i = 0; i < 6; i++)
        {
            Vector2 offset = (i * MathHelper.TwoPi / 6).ToRotationVector2() * radius;
            if (boss.Phase == 4) EvolutionVisuals.DrawEvolved(Main.spriteBatch, boss.NPC.Center + offset - Main.screenPosition,
                (Rose with { A = 0 }) * brightness, boss.NPC.rotation, scale, flip);
            else Main.spriteBatch.Draw(texture, boss.NPC.Center + offset - Main.screenPosition, null, (Rose with { A = 0 }) * brightness,
                    boss.NPC.rotation, origin, scale, flip, 0);
        }
    }

    internal static float BodyDissolve(Evolution boss) => boss.Transitioning ? EvolutionCinematicFrame.Smooth(EvolutionCinematicFrame.BurstTime, 583, EvolutionRules.CinematicTime(boss.Phase, boss.Timer)) : 0;
    internal static float BodyReveal(Evolution boss) => boss.Transitioning ? EvolutionCinematicFrame.Smooth(580, 600, EvolutionRules.CinematicTime(boss.Phase, boss.Timer)) : 0;

    internal static void DrawRevealedBody(Evolution boss, SpriteBatch batch, Vector2 screenPosition, Vector2 scale, SpriteEffects flip, float opacity)
    {
        float reveal = BodyReveal(boss);
        if (reveal <= 0) return;
        if (boss.Phase == 2)
        {
            EvolutionVisuals.DrawEvolved(batch, boss.NPC.Center - screenPosition, Color.White * (opacity * reveal), boss.NPC.rotation,
                scale * MathHelper.Lerp(.86f, 1, reveal), flip);
            return;
        }
        Texture2D texture = EvolutionVisuals.Asset(boss.Phase == 4 ? "Perfect" : "Evolved");
        Vector2 origin = boss.Phase == 4 ? new Vector2(119, 112) : new Vector2(95, 108);
        if (flip != SpriteEffects.None) origin.X = texture.Width - origin.X;
        batch.Draw(texture, boss.NPC.Center - screenPosition, null, Color.White * (opacity * reveal), boss.NPC.rotation,
            origin, scale * MathHelper.Lerp(.86f, 1, reveal), flip, 0);
    }

    internal static bool OnScreen(Vector2 point, float margin) =>
        Math.Abs(point.X - Main.screenPosition.X - Main.screenWidth * .5f) <= Main.screenWidth * .5f + margin &&
        Math.Abs(point.Y - Main.screenPosition.Y - Main.screenHeight * .5f) <= Main.screenHeight * .5f + margin;
}

public sealed class EvolutionCinematicSystem : ModSystem
{
    private static Evolution VisibleBoss()
    {
        if (Main.dedServ || Main.gameMenu) return null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Evolution boss && EvolutionCinematics.TryFrame(boss, out var frame) &&
                Vector2.DistanceSquared(Main.LocalPlayer.Center, frame.Center) < 2800 * 2800) return boss;
        return null;
    }

    public override void PostDrawTiles()
    {
        Evolution boss = VisibleBoss();
        if (boss == null || !EvolutionCinematics.TryFrame(boss, out var frame)) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        EvolutionCinematics.DrawBackdrop(frame, EvolutionCinematics.Reduced);
        Main.spriteBatch.End();
    }

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        Evolution boss = VisibleBoss();
        if (boss == null || !EvolutionCinematics.TryFrame(boss, out var frame)) return;
        float amount = frame.Atmosphere * (EvolutionCinematics.Reduced ? .4f : 1);
        // Restrained environmental grading instead of an opaque overlay over bullets or UI.
        backgroundColor = Color.Lerp(backgroundColor, new Color(65, 30, 44), amount);
        tileColor = Color.Lerp(tileColor, new Color(174, 129, 137), amount * .45f);
    }

    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu) return;
        if (EvolutionCinematics.Reduced || !EvolutionCinematics.ShakeEnabled) return;
        Evolution boss = VisibleBoss();
        if (boss == null || !EvolutionCinematics.TryFrame(boss, out var frame)) return;
        float distance = Vector2.Distance(Main.LocalPlayer.Center, frame.Center);
        float strength = frame.Shake * (1 - EvolutionCinematicFrame.Smooth(1100, 2500, distance));
        // Never retarget/zoom/lock the camera or change the player's position.
        Main.screenPosition += new Vector2(MathF.Sin(frame.Time * 2.37f), MathF.Cos(frame.Time * 1.91f)) * strength;
    }
}
