using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaDragon
{
    // Every approach keeps forward travel. A tell locks its aim before the damaging pass.
    private void Commit(Player player, float lead)
    {
        if (!Authority) return;
        Vector2 prediction = player.velocity * lead;
        if (prediction.LengthSquared() > 420 * 420) prediction = prediction.SafeNormalize(Vector2.Zero) * 420;
        Aim = IsharmlaRules.BoundedAim(player.Center + prediction);
        SweepDirection = (Aim - NPC.Center).SafeNormalize(Direction);
        NPC.netUpdate = true;
    }
    private void HuntAssault(Isharmla boss, Player p, bool final)
    {
        int beat = LocalTimer % 110;
        if (beat == 1) Lock(p);
        if (beat < 36)
        {
            Navigate(Aim + new Vector2(AttackSide * (LocalTimer / 110 % 2 == 0 ? 850 : -850), 360), 30, .065f);
            Charge = IsharmlaRules.Smooth(beat / 36f);
        }
        if (beat == 12) Commit(p, final ? 22 : 15);
        if (beat == 36) { SweepDirection = (Aim - NPC.Center).SafeNormalize(Direction); IsharmlaPresentation.Cue(4, Muzzle); }
        if (beat is >= 36 and < 88)
        {
            Rush(SweepDirection, final ? 48 : 41, beat - 36, 52); ContactActive = true;
            if (beat == 42) IsharmlaPresentation.Impact(Muzzle, 2.3f);
        }
        if (beat >= 88) Navigate(Aim + new Vector2(-AttackSide * 1050, -520), 29, .045f);
        Jaw = MathHelper.Lerp(Jaw, ContactActive ? .3f : 0, .12f);
    }
    private void BroadsideAssault(Isharmla boss, Player p, bool final)
    {
        int beat = LocalTimer % 160;
        int side = LocalTimer < 160 ? AttackSide : -AttackSide;
        if (beat == 1) Lock(p);
        if (beat < 55)
        {
            Navigate(Aim + new Vector2(side * 1000, -230), 31, .065f);
            Charge = IsharmlaRules.Smooth((beat - 15) / 40f);
        }
        if (beat == 23) Commit(p, 12);
        if (beat == 55) { SweepDirection = (Aim - NPC.Center).SafeNormalize(Direction); IsharmlaPresentation.Cue(4, Muzzle); }
        if (beat is >= 55 and < 125)
        {
            Rush(SweepDirection, final ? 43 : 36, beat - 55, 70); ContactActive = true;
            if (beat is 65 or 95) boss.Score(Muzzle, SweepDirection, 5, .65f, 13, 0, 24);
        }
        if (beat >= 125) Navigate(Aim + new Vector2(-side * 1000, -400), 31, .05f);
    }
    private void CoilAssault(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t < 70) Navigate(Aim + new Vector2(AttackSide * 900, -100), 33, .065f);
        else if (t < 242)
        {
            float a = (t - 70) * .018f * AttackSide;
            float lead = a + .42f * AttackSide;
            Navigate(Aim + new Vector2(MathF.Cos(lead) * AttackSide * 850, MathF.Sin(lead) * 650), 34, .065f);
            ContactActive = true;
            if (t % 32 == 0) boss.Score(Muzzle, (Aim - Muzzle).SafeNormalize(Direction), 5, .6f, 12, 1, 32);
            if (t is 108 or 156) boss.SpawnDrifter(Muzzle, Aim, AttackSide);
        }
        else if (t < 280)
        { Navigate(Aim + new Vector2(-AttackSide * 900, -380), 29, .055f); Charge = (t - 242) / 38f; }
        if (t == 242) Commit(p, 24);
        if (t == 280) { SweepDirection = (Aim - NPC.Center).SafeNormalize(Direction); IsharmlaPresentation.Cue(4, Muzzle); }
        if (t >= 280) { Rush(SweepDirection, final ? 50 : 43, t - 280, 70); ContactActive = true; }
    }
    private void GateAssault(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t == 1 && Authority)
        {
            PortalIncoming = NPC.velocity.SafeNormalize(Direction);
            float room = PortalIncoming.X > 0 ? Main.maxTilesX * 16 - NPC.Center.X : NPC.Center.X;
            if (Math.Abs(PortalIncoming.X) > .2f && room < 1900)
                PortalIncoming = new Vector2(-Math.Sign(PortalIncoming.X) * .65f, PortalIncoming.Y > .1f ? .76f : -.76f);
            PortalEntrance = IsharmlaRules.BoundedAim(NPC.Center + PortalIncoming * 1600);
            PortalIncoming = (PortalEntrance - NPC.Center).SafeNormalize(Direction);
            PortalExit = IsharmlaRules.BoundedAim(Aim + new Vector2(-AttackSide * 1100, -320));
            PortalOutgoing = (Aim - PortalExit).SafeNormalize(Vector2.UnitX);
            PortalOpen = true; PortalCrossed = false; NPC.netUpdate = true;
        }
        if (!PortalOpen) { Navigate(p.Center + new Vector2(AttackSide * 1100, -400), 32, .06f); return; }
        if (!PortalCrossed)
        {
            Charge = IsharmlaRules.Smooth(t / 70f);
            Navigate(PortalEntrance, t < 75 ? 20 : 32, .085f);
            float remaining = Vector2.Distance(NPC.Center, PortalEntrance);
            if (t >= 75 && remaining < Math.Max(85, NPC.velocity.Length() * 2))
            {
                Spine.CrossPortal(NeckAt(PortalEntrance, Direction), NeckAt(PortalExit, PortalOutgoing), Direction, PortalOutgoing);
                NPC.Center = PortalExit; Direction = PortalOutgoing; NPC.velocity = Direction * 24;
                PortalCrossed = true; PortalExitTimer = t; PortalCrossTravel = Spine.Travel; NPC.netUpdate = Authority;
                IsharmlaPresentation.Impact(PortalExit, 2.6f);
            }
        }
        else if (t < PortalExitTimer + 88)
        { Rush(PortalOutgoing, final ? 49 : 42, t - PortalExitTimer, 88); ContactActive = t >= PortalExitTimer + 12; }
        else Navigate(p.Center + new Vector2(AttackSide * 1100, -550), 32, .05f);
        if (PortalCrossed && t == PortalExitTimer + 34)
            boss.Score(PortalExit, PortalOutgoing, 9, 1.2f, 13, 1, 30);
    }
    private void DiveAssault(Isharmla boss, Player p, bool final)
    {
        int beat = LocalTimer % 160;
        if (beat == 1) Lock(p);
        if (beat < 55)
        { Navigate(Aim + new Vector2(AttackSide * 750, -750), 34, .065f); Charge = IsharmlaRules.Smooth(beat / 55f); }
        if (beat == 23) Commit(p, 20);
        if (beat == 55) { SweepDirection = (Aim - NPC.Center).SafeNormalize(Direction); IsharmlaPresentation.Cue(4, Muzzle); }
        if (beat is >= 55 and < 115)
        {
            Rush(SweepDirection, final ? 47 : 40, beat - 55, 60); ContactActive = true;
            if (beat % 18 == 0) boss.SpawnShot(TideShot.WakeMark, NPC.Center - Direction * 180, Direction, 110, 60, duration: 55);
        }
        if (beat >= 115) Navigate(Aim + new Vector2(-AttackSide * 1100, -500), 34, .05f);
        if (beat == 120) { AttackSide *= -1; if (Authority) NPC.netUpdate = true; }
    }
    private void BreathAssault(Isharmla boss, Player p)
    {
        int t = LocalTimer;
        // A moving firing platform: strafe across the side of the arena, never park for the beam.
        Vector2 destination = Aim + new Vector2(AttackSide * 1000, -520 + MathF.Sin(t * .012f) * 650);
        Navigate(destination, t < 70 ? 30 : 19, .038f);
        if (t is >= 60 and <= 306)
            Direction = FacingAngle.AngleTowards((p.Center - Muzzle).ToRotation(), t < 156 ? .018f : .005f).ToRotationVector2();
        Charge = IsharmlaRules.Envelope(t, 55, 306, 55);
        Jaw = MathHelper.Lerp(Jaw, Charge * .35f, .08f);
        if (t == 84)
        { boss.SpawnShot(TideShot.Breath, Muzzle, Direction, 160, 72, 2600, 70, 150); IsharmlaPresentation.Cue(5, Muzzle); }
        if (t == 156) IsharmlaPresentation.Impact(Muzzle, 2.6f);
        if (t > 306) { Navigate(p.Center + new Vector2(-AttackSide * 1200, -500), 35, .05f); ContactActive = true; }
    }
    private void PassPressure(Player player)
    {
        if (Main.dedServ || !ContactActive || NPC.velocity.LengthSquared() < 30 * 30) return;
        float proximity = 1 - MathHelper.Clamp(Vector2.Distance(player.Center, NPC.Center) / 950, 0, 1);
        if (LocalTimer % 8 == 0 && proximity > 0) IsharmlaVisuals.Shake(proximity * 7);
        if (LocalTimer % 42 == 0 && proximity > .25f) IsharmlaPresentation.Cue(4, NPC.Center);
    }
}
