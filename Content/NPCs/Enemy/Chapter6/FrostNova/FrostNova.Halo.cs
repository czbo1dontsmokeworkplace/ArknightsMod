using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

internal static class FrostNovaHaloRules
{
    internal const int Curves = 6, RelocationStart = 132, RelocationWarning = 60;
    internal static Vector2 Follow(Vector2 previous, Vector2 target, bool committed)
    {
        Vector2 delta = (target - previous) * (committed ? .065f : .13f);
        float cap = committed ? 16 : 26;
        if (delta.LengthSquared() > cap * cap) delta = delta.SafeNormalize(Vector2.Zero) * cap;
        return previous + delta;
    }
    // Six inclined great circles, with slow precession of their orbital planes.
    // No position history: movement cannot stretch a trail back to a stale world origin.
    internal static Vector2 Orbit(int curve, float time, float tail)
    {
        float phase = curve * MathHelper.TwoPi / Curves;
        float angle = time * (.95f + curve * .055f) + phase - tail * 2.7f;
        float radius = 58 + curve % 3 * 7;
        Vector3 point = new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, 0);
        Matrix plane = Matrix.CreateRotationX(.35f + curve * .43f)
            * Matrix.CreateRotationY(time * .19f + phase)
            * Matrix.CreateRotationZ(time * .10f * (curve % 2 == 0 ? 1 : -1) + phase * .45f);
        point = Vector3.Transform(point, plane);
        return new Vector2(point.X, point.Y * .72f + point.Z * .55f);
    }
}

public partial class FrostNova
{
    private bool haloInitialized, relocationArmed;
    private Vector2 haloCenter, relocationOrigin, relocationTarget;
    internal Vector2 HaloCenter => haloInitialized ? haloCenter : NPC.Center;
    private void UpdateHaloAnchor()
    {
        if (!haloInitialized || !float.IsFinite(haloCenter.X) || !float.IsFinite(haloCenter.Y)) {
            haloCenter = NPC.Center; haloInitialized = true;
        }
        bool committed = relocationArmed || FrostNovaCombatRules.IsMotion(CombatPattern);
        haloCenter = FrostNovaHaloRules.Follow(haloCenter, NPC.Center, committed);
    }
    private bool TickPlannedRelocation(Player player, int pattern)
    {
        bool planned = AirbornePhase ? pattern == 7 : pattern == 5;
        int start = FrostNovaHaloRules.RelocationStart, end = start + FrostNovaHaloRules.RelocationWarning;
        if (!planned || combatTimer < start || combatTimer > end) { relocationArmed = false; return false; }
        if (combatTimer == start && Main.netMode != NetmodeID.MultiplayerClient) {
            relocationOrigin = NPC.Center;
            float side = attackSerial % 2 == 0 ? -1 : 1;
            // This is an intentional local move, never a catch-up teleport to an out-of-bounds player.
            relocationTarget = NPC.Center + new Vector2(side * 320, AirbornePhase ? -100 : -90);
            if (!AirbornePhase) relocationTarget = FindSurface(relocationTarget) - new Vector2(0, NPC.height * .5f + 8);
            relocationArmed = !Collision.SolidCollision(relocationTarget - NPC.Size * .5f, NPC.width, NPC.height)
                && Vector2.DistanceSquared(relocationTarget, relocationOrigin) < 500 * 500;
            NPC.netUpdate = true;
        }
        if (!relocationArmed) return false;
        NPC.damage = 0; NPC.velocity *= .7f;
        if (combatTimer == start) FrostNovaVisuals.Cast(NPC.Center, 0, .55f);
        if (combatTimer == end) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                NPC.Center = relocationTarget; NPC.velocity = Vector2.Zero;
                attackAnchor = NPC.Center; NPC.netUpdate = true;
            }
            // haloCenter is deliberately retained, then catches up over subsequent ticks.
            FrostNovaVisuals.Cast(relocationTarget, 0, .7f);
            relocationArmed = false;
        }
        return true;
    }
    private void DrawOrbitHalo(SpriteBatch batch, Vector2 screenPos)
    {
        if (!AirbornePhase || NPC.Opacity <= .01f) return;
        float opacity = Math.Clamp(ReBirthTimer / 45f, 0, 1) * NPC.Opacity;
        FrostNovaHaloVisuals.Draw(batch, HaloCenter + new Vector2(0, 8), screenPos,
            Main.GlobalTimeWrappedHourly, opacity);
    }
    private void DrawRelocationWarning(SpriteBatch batch, Vector2 screenPos)
    {
        if (!relocationArmed || !CombatHazardsActive) return;
        float progress = Math.Clamp((combatTimer - FrostNovaHaloRules.RelocationStart) / (float)FrostNovaHaloRules.RelocationWarning, 0, 1);
        FrostNovaHaloVisuals.Marker(batch, relocationOrigin, screenPos, 45 + progress * 15, .35f);
        FrostNovaHaloVisuals.Marker(batch, relocationTarget, screenPos, 65 - progress * 24, .65f + progress * .35f);
        for (int i = 1; i < 12; i++) {
            Vector2 at = Vector2.Lerp(relocationOrigin, relocationTarget, i / 12f);
            FrostNovaHaloVisuals.Stroke(batch, at - Vector2.UnitX * 2, at + Vector2.UnitX * 2, screenPos, .5f, 2);
        }
    }
}

internal static class FrostNovaHaloVisuals
{
    private static readonly Color Gold = new(250, 224, 91);
    internal static void Stroke(SpriteBatch batch, Vector2 from, Vector2 to, Vector2 screenPos, float opacity, float width)
    {
        Vector2 delta = to - from;
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() < .001f || delta.LengthSquared() > 150 * 150) return;
        var glow = Terraria.GameContent.TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        Vector2 at = (from + to) * .5f - screenPos;
        // Draw in the caller's current NPC batch, including modded offscreen targets.
        // Subtract the supplied screenPos exactly once; never restart SpriteBatch here.
        batch.Draw(glow, at, null, (Gold with { A = 0 }) * (opacity * .20f), delta.ToRotation(), glow.Size() * .5f,
            new Vector2(delta.Length() + width * 2, width * 5) / glow.Size(), SpriteEffects.None, 0);
        batch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, from - screenPos, new Rectangle(0, 0, 1, 1),
            (Gold with { A = 0 }) * (opacity * .6f), delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Draw(SpriteBatch batch, Vector2 center, Vector2 screenPos, float time, float opacity)
    {
        const int segments = 36;
        for (int curve = 0; curve < FrostNovaHaloRules.Curves; curve++)
            for (int j = 0; j < segments; j++) {
                float tail = j / (float)segments;
                Vector2 a = center + FrostNovaHaloRules.Orbit(curve, time, tail);
                Vector2 b = center + FrostNovaHaloRules.Orbit(curve, time, (j + 1f) / segments);
                float fade = MathF.Pow(1 - tail, 1.4f);
                Stroke(batch, a, b, screenPos, opacity * fade, .65f + 2.2f * fade);
            }
    }
    internal static void Marker(SpriteBatch batch, Vector2 center, Vector2 screenPos, float radius, float opacity)
    {
        for (int i = 0; i < 48; i++) {
            if (i % 8 is 6 or 7) continue;
            Vector2 a = center + (i * MathHelper.TwoPi / 48).ToRotationVector2() * new Vector2(radius, radius * .42f);
            Vector2 b = center + ((i + 1) * MathHelper.TwoPi / 48).ToRotationVector2() * new Vector2(radius, radius * .42f);
            Stroke(batch, a, b, screenPos, opacity, 1.6f);
        }
    }
}
