using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    internal const int ShieldTargetSize = 120;
    internal Rectangle BodyContactBox => new((int)NPC.Center.X - 20, (int)NPC.Center.Y - 29, 40, 58);
    private void UpdateShieldTarget() {
        int width = AirbornePhase ? ShieldTargetSize : 40, height = AirbornePhase ? ShieldTargetSize : 58;
        if (NPC.width == width && NPC.height == height) return;
        Vector2 center = NPC.Center;
        NPC.width = width; NPC.height = height; NPC.Center = center;
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) =>
        CombatHazardsActive && NPC.damage > 0 && BodyContactBox.Intersects(target.Hitbox);

    // Infernum's align / tell / commit / brake rhythm, scaled to early Hardmode.
    // All locked geometry uses the existing encounter snapshot, never private client aim.
    private bool TickNewMotion(Player player, int pattern) {
        if (pattern is not (9 or 10)) return false;
        int t = combatTimer;
        if (pattern == 10) {
            if (t < 60) {
                MoveTo(attackAim + new Vector2(0, -330), FrostNovaCombatRules.FlightSpeed, .1f);
                return true;
            }
            if (t == 60 && Main.netMode != NetmodeID.MultiplayerClient) {
                attackAnchor = NPC.Center;
                // The complete fall is vertical, to the real ground below the locked X.
                attackAim = FindSurface(new Vector2(NPC.Center.X, player.Bottom.Y - 8)) - new Vector2(0, 29);
                if (attackAim.Y < NPC.Center.Y + 60) attackAim.Y = NPC.Center.Y + 360;
                dashDirection = Vector2.UnitY;
                NPC.netUpdate = true;
            }
            if (t < 120) { NPC.velocity = Vector2.Zero; return true; }
            if (t < 168) {
                float speed = Math.Min(FrostNovaCombatRules.SlamSpeed, 12 + (t - 120) * .5f);
                NPC.velocity = Vector2.UnitY * speed;
                NPC.damage = FrostNovaCombatRules.HeavyDamage;
                if (NPC.Center.Y + speed >= attackAim.Y || t == 167) {
                    NPC.Center = new Vector2(attackAnchor.X, Math.Min(NPC.Center.Y + speed, attackAim.Y));
                    NPC.velocity = Vector2.Zero; NPC.damage = 0;
                    FrostNovaVisuals.Cast(NPC.Center + new Vector2(0, 29), 2, 1.4f);
                    for (int side = -1; side <= 1; side += 2)
                        FireFan(NPC.Center, new Vector2(side, -.2f).SafeNormalize(Vector2.UnitX), 3, .22f, 4.8f, 0, 42);
                    combatTimer = 168;
                    attackAnchor = NPC.Center - new Vector2(0, 160);
                    NPC.netUpdate = true;
                }
                return true;
            }
            MoveTo(attackAnchor, 5.5f, .05f);
            return true;
        }
        bool second = AirbornePhase && t >= 150;
        int local = second ? t - 120 : t;
        if (t == 30 || (AirbornePhase && t == 150)) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                attackAnchor = NPC.Center;
                attackAim = player.Center;
                dashDirection = AirbornePhase ? (attackAim - NPC.Center).SafeNormalize(Vector2.UnitX)
                    : new Vector2(player.Center.X >= NPC.Center.X ? 1 : -1, 0);
                NPC.netUpdate = true;
            }
            FrostNovaVisuals.Cast(NPC.Center, 1, .9f);
        }
        if (local >= 30 && local < 78) { NPC.velocity = Vector2.Zero; return true; }
        if (local >= 78 && local < 106) {
            NPC.velocity = dashDirection * (AirbornePhase ? FrostNovaCombatRules.AirDashSpeed : FrostNovaCombatRules.GroundDashSpeed);
            NPC.damage = FrostNovaCombatRules.ShotDamage(AirbornePhase ? 2 : 1, true);
            if (!AirbornePhase && Collision.SolidCollision(NPC.position + new Vector2(NPC.velocity.X, 0), NPC.width, NPC.height)) {
                NPC.velocity.X = 0; NPC.damage = 0;
            }
            if (local % 14 == 8) FrostNovaVisuals.Cast(NPC.Center, 3, .35f);
            return true;
        }
        if (local == 106) { NPC.velocity *= .15f; attackAnchor = NPC.Center; NPC.netUpdate = true; }
        if (t < 30) {
            if (AirbornePhase) MoveTo(attackAnchor, FrostNovaCombatRules.FlightSpeed, .08f);
            else NPC.velocity.X *= .8f;
        }
        else NPC.velocity *= .88f;
        return true;
    }
    private void DrawMotionWarning(SpriteBatch batch, Vector2 screenPos) {
        if (!CombatHazardsActive) return;
        int t = combatTimer, pattern = CombatPattern;
        bool slam = pattern == 10 && t >= 60 && t < 120;
        bool dash = pattern == 9 && ((t >= 30 && t < 78) || (AirbornePhase && t >= 150 && t < 198));
        if (!slam && !dash) return;
        Vector2 end = slam ? attackAim : attackAnchor + dashDirection * (AirbornePhase ? FrostNovaCombatRules.AirDashSpeed : FrostNovaCombatRules.GroundDashSpeed) * 28;
        for (int i = 0; i < 14; i++) {
            Vector2 at = Vector2.Lerp(attackAnchor, end, i / 13f);
            FrostNovaHaloVisuals.Marker(batch, at, screenPos, slam ? 17 : 12, .7f);
        }
        if (slam) FrostNovaHaloVisuals.Marker(batch, end + new Vector2(0, 29), screenPos, 80, 1);
    }
}
