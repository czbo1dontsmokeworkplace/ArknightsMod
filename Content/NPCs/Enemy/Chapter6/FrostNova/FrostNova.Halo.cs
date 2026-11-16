using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

internal static class FrostNovaHaloRules
{
    internal const int Curves = 6, RelocationStart = 132, RelocationWarning = 60;
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
    private bool relocationArmed;
    private int relocationStart;
    private Vector2 relocationOrigin, relocationTarget;
    private Vector2 domainCenter,domainOrigin,domainDestination;
    private int domainTravel=22;
    private bool domainReady;
    internal Vector2 HaloCenter => !AirbornePhase && domainReady ? domainCenter : NPC.Center;
    private void BeginDomainTransit(Vector2 destination)
    {
        if(!domainReady){domainCenter=NPC.Center;domainReady=true;}
        domainOrigin=domainCenter;domainDestination=destination;domainTravel=0;
    }
    private void UpdateDomainCenter()
    {
        if(!domainReady){domainCenter=domainOrigin=domainDestination=NPC.Center;domainReady=true;domainTravel=22;}
        if(Vector2.DistanceSquared(NPC.Center,domainDestination)>80*80)BeginDomainTransit(NPC.Center);
        else domainDestination=NPC.Center;
        if(domainTravel<22){domainTravel++;float t=domainTravel/22f;domainCenter=Vector2.Lerp(domainOrigin,domainDestination,1-MathF.Pow(1-t,3));}
        else domainCenter=NPC.Center;
    }
    private bool TickPlannedRelocation(Player player, int pattern)
    {
        bool motion = !AirbornePhase && FrostNovaCombatRules.IsMotion(pattern);
        bool planned = motion || (AirbornePhase ? pattern == 7 : pattern == 5);
        int start = motion ? pattern == 9 && AirbornePhase && combatTimer >= 150 ? 150 : 30 : FrostNovaHaloRules.RelocationStart;
        int end = start + FrostNovaHaloRules.RelocationWarning;
        if (!planned || combatTimer < start || combatTimer > end) { relocationArmed = false; return false; }
        if (combatTimer == start && Main.netMode != NetmodeID.MultiplayerClient) {
            relocationOrigin = NPC.Center; relocationStart = start;
            float side = (attackSerial + start / 150) % 2 == 0 ? -1 : 1;
            Vector2 seed = NPC.Center + new Vector2(side * (motion ? 400 : 320), 0);
            relocationArmed = TryStandingCenter(seed, NPC.width, NPC.height, out relocationTarget)
                && Vector2.DistanceSquared(relocationTarget, relocationOrigin) < 720 * 720;
            NPC.netUpdate = true;
        }
        if (!relocationArmed) return false;
        NPC.damage = 0; NPC.velocity.X = 0;
        if (combatTimer == start) FrostNovaVisuals.Cast(NPC.Center, 0, .7f);
        if (combatTimer % 9 == 0) FrostNovaVisuals.Cast(NPC.Center, 1, .3f);
        if (combatTimer == end) {
            FrostNovaVisuals.Shatter(relocationOrigin, Vector2.Zero, 18);
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                // Revalidate the warned point in case a player has placed blocks there.
                if (!Collision.SolidCollision(relocationTarget - NPC.Size * .5f, NPC.width, NPC.height)) {
                    BeginDomainTransit(relocationTarget);
                    NPC.Center = relocationTarget; NPC.velocity = Vector2.Zero;
                    attackAnchor = NPC.Center;
                    if (motion) SpawnHazard(NPC.Center, (attackAim - NPC.Center).SafeNormalize(Vector2.UnitY),
                        Terraria.ModLoader.ModContent.ProjectileType<FrostNovaRitualPulse>(),
                        FrostNovaCombatRules.ShotDamage(AirbornePhase ? 2 : 1), NPC.whoAmI, AirbornePhase ? 1 : 0);
                }
                NPC.netUpdate = true;
            }
            FrostNovaVisuals.Cast(relocationTarget, 0, 1.4f);
            relocationArmed = false;
        }
        return true;
    }
    private void DrawOrbitHalo(SpriteBatch batch, Vector2 screenPos)
    {
        if (!AirbornePhase || !NPC.active) return;
        float opacity = 1;
        FrostNovaHaloVisuals.Draw(batch, HaloCenter + new Vector2(0, 8), screenPos,
            Main.GlobalTimeWrappedHourly, opacity);
    }
    private void DrawRelocationWarning(SpriteBatch batch, Vector2 screenPos)
    {
        if (!relocationArmed || !CombatHazardsActive) return;
        float progress = Math.Clamp((combatTimer - relocationStart) / (float)FrostNovaHaloRules.RelocationWarning, 0, 1);
        FrostNovaHaloVisuals.Marker(batch, relocationOrigin, screenPos, 45 + progress * 15, .35f);
        FrostNovaHaloVisuals.Marker(batch, relocationTarget, screenPos, 65 - progress * 24, .65f + progress * .35f);
        for (int i = 1; i <= 3; i++)
            FrostNovaProjectileArt.Crystal(Vector2.Lerp(relocationOrigin, relocationTarget, i / 4f), new Vector2(6, 12), 0, .5f);
    }
}

internal static class FrostNovaHaloVisuals
{
    private static readonly Color Gold = new(250, 224, 91);
    internal static void Draw(SpriteBatch batch, Vector2 center, Vector2 screenPos, float time, float opacity)
    {
        var glow = Terraria.GameContent.TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        for (int curve=0;curve<FrostNovaHaloRules.Curves;curve++) {
            Vector2 at=center+FrostNovaHaloRules.Orbit(curve,time,0)-screenPos;
            batch.Draw(glow,at,null,(Gold with { A=0 })*(opacity*.75f),0,glow.Size()/2,new Vector2(30,24)/glow.Size(),SpriteEffects.None,0);
            batch.Draw(glow,at,null,new Color(255,246,158,0)*opacity,0,glow.Size()/2,new Vector2(12,10)/glow.Size(),SpriteEffects.None,0);
        }
    }
    internal static void Marker(SpriteBatch batch, Vector2 center, Vector2 screenPos, float radius, float opacity)
    {
        var glow=Terraria.GameContent.TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        batch.Draw(glow,center-screenPos,null,new Color(150,220,255,0)*opacity,0,glow.Size()/2,new Vector2(20,12)/glow.Size(),SpriteEffects.None,0);
    }
}
