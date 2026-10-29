using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaDragon
{
    private void EncounterAI()
    {
        Isharmla boss = Boss;
        if (boss == null) { if (Authority) IsharmlaWorld.Deactivate(NPC); return; }
        UpdateSpine(); NPC.target = boss.NPC.target; NPC.timeLeft = 1800;
        // Last-resort containment; regular swimming and charges bend away before reaching this boundary.
        float worldRight = Main.maxTilesX * 16 - 160;
        if (NPC.Center.X < 160 || NPC.Center.X > worldRight)
        {
            bool left = NPC.Center.X < 160;
            NPC.Center = new Vector2(MathHelper.Clamp(NPC.Center.X, 160, worldRight), NPC.Center.Y);
            NPC.velocity.X = 0;
            if (SweepDirection.LengthSquared() > 0) SweepDirection.X = Math.Abs(SweepDirection.X) * (left ? 1 : -1);
        }
        Player p = Main.player[NPC.target];
        if (PortalOpen && PortalCrossed && Spine.Travel - PortalCrossTravel > IsharmlaRules.SegmentDistance(18) + 180)
        { PortalOpen = false; if (Authority) NPC.netUpdate = true; }
        if (previousStage != boss.Stage)
        {
            previousStage = boss.Stage; LocalTimer = 0; ContactActive = false;
            if (!PortalCrossed) PortalOpen = false;
            if (Authority && boss.Stage != TideStage.Finale) SelectPattern(boss.Stage == TideStage.LastTide, true);
        }
        if (Dying > 0 || boss.Stage == TideStage.Finale)
        { FinaleMovement(boss); return; }
        int floor = IsharmlaRules.DragonFloor(NPC.lifeMax, boss.Stage);
        NPC.life = Math.Max(Math.Max(1, floor), NPC.life);
        NPC.dontTakeDamage = boss.Transition > 0 || !IsharmlaRules.DragonVulnerable(boss.Stage) ||
            floor > 0 && NPC.life <= floor || Visibility < .65f;
        NPC.damage = boss.Stage == TideStage.LastTide ? 190 : 155;
        ContactActive = false; Charge = 0;
        if (boss.Stage == TideStage.LastTide && NPC.life <= 1)
        {
            NPC.dontTakeDamage = true;
            if (Authority && boss.Timer >= 600) { CheckDead(); return; }
        }
        if (boss.Transition > 0)
        {
            NPC.damage = 0; NPC.velocity *= .96f;
            if (boss.Stage == TideStage.Leviathan)
            { NPC.Center = boss.Portal + new Vector2(0, 580); Direction = -Vector2.UnitY; NPC.velocity = Vector2.Zero; }
            else Navigate(boss.Aim + new Vector2(AttackSide * 850, -430), 17, .05f);
            Face(); return;
        }
        if (boss.Stage == TideStage.LastSong)
        {
            LocalTimer++;
            Navigate(boss.Anchor + new Vector2(MathF.Sin(LocalTimer * .004f) * 1200, 500), 13, .026f);
            Face(); NPC.damage = 0; Jaw = MathHelper.Lerp(Jaw, 0, .08f); return;
        }
        LocalTimer++;
        bool final = boss.Stage == TideStage.LastTide;
        int duration = IsharmlaRules.MotionLength(Pattern, final);
        // Finish a live portal traversal before selecting a new movement.
        bool busyPortal = Pattern == 3 && PortalOpen && (!PortalCrossed || Spine.Travel - PortalCrossTravel < IsharmlaRules.SegmentDistance(18) + 180);
        if (LocalTimer >= duration && !busyPortal && Authority) SelectPattern(final);
        if (busyPortal && LocalTimer > 840 && Authority)
        { PortalOpen = false; SelectPattern(final); }
        switch ((TideMotion)Pattern)
        {
            case TideMotion.Breach: BreachScore(boss, p, final); break;
            case TideMotion.Broadside: BroadsideScore(boss, p, final); break;
            case TideMotion.Spiral: SpiralScore(boss, p, final); break;
            case TideMotion.Gate: GateScore(boss, p, final); break;
            case TideMotion.Undertow: UndertowScore(boss, p, final); break;
            case TideMotion.AtomicBreath: AtomicScore(boss, p); break;
        }
        ContainMovement(); Face(); WaterEvents(boss);
        if (!Main.dedServ)
        {
            IsharmlaVisuals.DragonWake(NPC.Center, NPC.velocity, 85, 0, final);
            IsharmlaPresentation.DragonParticles(this);
        }
        if (Authority && LocalTimer % 60 == 0) NPC.netUpdate = true;
    }

    private void SelectPattern(bool final, bool reset = false)
    {
        if (reset) { bagPosition = 6; motionSequence = 0; }
        int count = final ? 6 : 5;
        if (bagPosition >= count || bagSize != count)
        {
            for (int i = 0; i < count; i++) patternBag[i] = i;
            for (int i = count - 1; i > 0; i--)
            { int j = Main.rand.Next(i + 1); (patternBag[i], patternBag[j]) = (patternBag[j], patternBag[i]); }
            if (patternBag[0] == previousPattern) (patternBag[0], patternBag[1]) = (patternBag[1], patternBag[0]);
            bagPosition = 0; bagSize = count;
        }
        // Put the prescribed opener into the bag rather than skipping its randomly selected entry.
        if (reset)
        {
            int opener = final ? 5 : Boss.Stage == TideStage.Leviathan ? 0 : 1;
            int index = Array.IndexOf(patternBag, opener, 0, count);
            (patternBag[0], patternBag[index]) = (patternBag[index], patternBag[0]);
        }
        Pattern = patternBag[bagPosition++];
        previousPattern = Pattern; motionSequence++; LocalTimer = 0; Serial++;
        Aim = IsharmlaRules.BoundedAim(Main.player[NPC.target].Center + Main.player[NPC.target].velocity * 12);
        Start = NPC.Center; AttackSide = NPC.Center.X > Aim.X ? 1 : -1;
        SweepDirection = Vector2.Zero; Charge = 0; ContactActive = false; NPC.netUpdate = true;
    }

    private void Navigate(Vector2 destination, float speed, float turn)
    {
        destination = new Vector2(MathHelper.Clamp(destination.X, 200, Main.maxTilesX * 16 - 200),
            MathHelper.Clamp(destination.Y, 250, Main.maxTilesY * 16 - 300));
        NPC.velocity = IsharmlaMotion.Swim(NPC.velocity, Direction, destination - NPC.Center, speed, turn);
    }
    private void LineUp(Vector2 direction, float speed)
        => NPC.velocity = IsharmlaMotion.LineUp(NPC.velocity, OceanHeading(direction), speed);
    private void Rush(Vector2 direction, float speed, int age, int duration)
        => NPC.velocity = IsharmlaMotion.Rush(NPC.velocity, OceanHeading(direction), speed, age, duration);
    private Vector2 OceanHeading(Vector2 direction)
    {
        direction = direction.SafeNormalize(Direction);
        float room = direction.X < 0 ? NPC.Center.X - 180 : Main.maxTilesX * 16 - 180 - NPC.Center.X;
        if (Math.Abs(direction.X) > .05f && room < 1000)
        {
            float bend = 1 - IsharmlaRules.Smooth(Math.Max(0, room) / 1000f);
            Vector2 inward = new(-Math.Sign(direction.X) * .65f, direction.Y > .15f ? 1 : -1);
            direction = Vector2.Lerp(direction, inward.SafeNormalize(-Vector2.UnitY), bend).SafeNormalize(Direction);
        }
        return direction;
    }
    private void ContainMovement()
    {
        // Limit outward travel by stopping distance instead of reflecting a high-speed body in one frame.
        float left = Math.Max(0, NPC.Center.X - 180), right = Math.Max(0, Main.maxTilesX * 16 - 180 - NPC.Center.X);
        float limit = MathF.Sqrt(2 * 1.4f * (NPC.velocity.X < 0 ? left : right));
        NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -limit, limit);
    }
    private void Face()
    {
        bool breathing = Pattern == 5 && LocalTimer is >= 85 and <= 455 && Dying == 0 && Boss?.Stage != TideStage.Finale;
        if (NPC.velocity.LengthSquared() > 1 && !breathing) Direction = NPC.velocity.SafeNormalize(Direction);
        NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle;
    }
    private void Lock(Player p)
    { if (Authority) { Aim = IsharmlaRules.BoundedAim(p.Center + p.velocity * 10); NPC.netUpdate = true; } }

    private void BreachScore(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t < 90)
        {
            if (t < 62) Navigate(new Vector2(Aim.X - AttackSide * 170, Math.Max(boss.Anchor.Y + 500, Aim.Y + 650)), 29, .09f);
            Charge = IsharmlaRules.Smooth((t - 35) / 55f);
            if (t >= 62) LineUp((Aim - NPC.Center).SafeNormalize(-Vector2.UnitY).Y < -.35f ? Aim - NPC.Center : new Vector2(-AttackSide * .22f, -1), 8);
        }
        if (t == 90)
        {
            Lock(p); SweepDirection = (Aim - NPC.Center).SafeNormalize(-Vector2.UnitY);
            if (SweepDirection.Y > -.35f) SweepDirection = new Vector2(-AttackSide * .22f, -1).SafeNormalize(-Vector2.UnitY);
            NPC.netUpdate = Authority; IsharmlaPresentation.Cue(4, NPC.Center);
        }
        if (t is >= 90 and < 155) { Rush(SweepDirection, final ? 38 : 32, t - 90, 65); ContactActive = true; }
        if (t is >= 155 and < 265) { Navigate(Aim + new Vector2(AttackSide * 900, -430), 25, .035f); ContactActive = true; }
        if (t == 175)
            for (int layer = 0; layer < 3; layer++)
                for (int i = -6; i <= 6; i++)
                    if (i != 0) boss.SpawnShot(TideShot.FoamArc, Muzzle,
                        new Vector2(i * .17f, 1).SafeNormalize(Vector2.UnitY) * (5 + layer * 2.2f),
                        95, 35 + layer * 5, duration: 190, style: 2);
        if (t >= 265) Navigate(Aim + new Vector2(-AttackSide * 700, 520), 23, .04f);
        Jaw = MathHelper.Lerp(Jaw, t is >= 90 and < 200 ? .25f : 0, .08f);
    }

    private void BroadsideScore(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t < 110)
        {
            if (t < 76) Navigate(Aim + new Vector2(AttackSide * 1050, -80), 29, .07f);
            else LineUp(new Vector2(-AttackSide, 0), 8);
        }
        if (t == 110 || t == 270)
        {
            Lock(p); SweepDirection = new Vector2(t == 110 ? -AttackSide : AttackSide, 0);
            // Commit to a line after the curtain has been shown.
            NPC.netUpdate = Authority;
            IsharmlaPresentation.Cue(4, Muzzle);
        }
        if (t is >= 110 and < 170 || t is >= 270 and < 330)
        { Rush(SweepDirection, final ? 38 : 32, t - (t < 170 ? 110 : 270), 60); ContactActive = true; }
        if (t is >= 170 and < 270)
        {
            if (t < 236) Navigate(Aim + new Vector2(-AttackSide * 1100, 100), 29, .07f);
            else LineUp(new Vector2(AttackSide, 0), 8);
        }
        if (t >= 330) { Navigate(Aim + new Vector2(AttackSide * 1000, -430), 23, .035f); ContactActive = false; }
        Charge = Math.Max(IsharmlaRules.Envelope(t, 55, 110), IsharmlaRules.Envelope(t, 220, 270));
        Jaw = MathHelper.Lerp(Jaw, ContactActive ? .16f : 0, .1f);
    }

    private void SpiralScore(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t >= 132 && t <= (final ? 192 : 180) && (t - 132) % 12 == 0)
            boss.SpawnDrifter(Muzzle, Aim, AttackSide);
        if (t < 90) Navigate(Aim + new Vector2(AttackSide * 720, -120), 28, .06f);
        else if (t < 315)
        {
            float angle = (t - 90) * .013f * AttackSide;
            // Aim ahead on the ellipse, so the head follows its tangent instead of chasing its own wake.
            float lead = angle + .013f * AttackSide * 18;
            Vector2 destination = Aim + new Vector2(MathF.Cos(lead) * AttackSide * 680, MathF.Sin(lead) * 480 - 80);
            Navigate(destination, 30, .06f);
        }
        else { Navigate(Aim + new Vector2(-AttackSide * 1200, -400), 30, .035f); ContactActive = t < 390; }
        if (t == 100)
        {
            boss.SpawnShot(TideShot.TideHeart, Aim + new Vector2(0, -80), Vector2.Zero, 115, 210, width: 80, duration: 150,
                style: final ? 1 : 0, target: p.Center);
            IsharmlaPresentation.Cue(0, Aim);
        }
        Charge = IsharmlaRules.Envelope(t, 100, 315, 60); Jaw = MathHelper.Lerp(Jaw, Charge * .2f, .05f);
    }

    private void GateScore(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t == 1 && Authority)
        {
            PortalIncoming = NPC.velocity.SafeNormalize(Direction);
            PortalEntrance = NPC.Center + PortalIncoming * 1250;
            PortalEntrance.X = MathHelper.Clamp(PortalEntrance.X, 220, Main.maxTilesX * 16 - 220);
            PortalEntrance.Y = MathHelper.Clamp(PortalEntrance.Y, 350, Main.maxTilesY * 16 - 400);
            PortalIncoming = (PortalEntrance - NPC.Center).SafeNormalize(Direction);
            PortalExit = IsharmlaRules.BoundedAim(Aim + new Vector2(-AttackSide * 950, -140));
            PortalOutgoing = (Aim - PortalExit).SafeNormalize(Vector2.UnitX);
            PortalOpen = true; PortalCrossed = false; NPC.netUpdate = true;
        }
        if (!PortalOpen) { Navigate(Aim + new Vector2(AttackSide * 950, -450), 23, .04f); return; }
        if (!PortalCrossed)
        {
            Navigate(PortalEntrance, 30, .065f);
            float remaining = Vector2.Distance(NPC.Center, PortalEntrance);
            if (t > 65 && remaining < 450)
                NPC.velocity = IsharmlaMotion.Hover(NPC.velocity, PortalEntrance - NPC.Center, .16f, 30);
            if (t > 115 && remaining < NPC.velocity.Length() + 50)
            {
                Spine.CrossPortal(NeckAt(PortalEntrance, Direction), NeckAt(PortalExit, PortalOutgoing), Direction, PortalOutgoing);
                float entrySpeed = MathHelper.Clamp(NPC.velocity.Length(), 10, 18);
                NPC.Center = PortalExit; Direction = PortalOutgoing; NPC.velocity = Direction * entrySpeed;
                PortalCrossed = true; PortalExitTimer = t; PortalCrossTravel = Spine.Travel; NPC.netUpdate = Authority;
                IsharmlaPresentation.Impact(PortalExit, 1.1f);
            }
        }
        else if (t < PortalExitTimer + 78) { Rush(PortalOutgoing, final ? 43 : 35, t - PortalExitTimer, 78); ContactActive = true; }
        else Navigate(Aim + new Vector2(AttackSide * 1100, -430), 26, .035f);
        if (PortalCrossed && t == PortalExitTimer + 32)
            boss.Score(PortalExit, PortalOutgoing, final ? 13 : 9, 1.5f, 7.5f, 1, 30);
        Charge = !PortalCrossed ? IsharmlaRules.Smooth((t - 45) / 80f) : 0;
    }

    private void UndertowScore(Isharmla boss, Player p, bool final)
    {
        int t = LocalTimer;
        if (t < 100)
        {
            if (t < 72) Navigate(Aim + new Vector2(AttackSide * 650, -650), 25, .06f);
            else LineUp(Aim + new Vector2(-AttackSide * 450, 420) - NPC.Center, 8);
        }
        if (t == 100) { Lock(p); SweepDirection = (Aim + new Vector2(-AttackSide * 450, 420) - NPC.Center).SafeNormalize(Vector2.UnitY); NPC.netUpdate = Authority; }
        if (t is >= 100 and < 185) { Rush(SweepDirection, final ? 34 : 28, t - 100, 85); ContactActive = true; }
        if (t is >= 112 and <= 172 && t % 12 == 4)
            boss.SpawnShot(TideShot.WakeMark, NPC.Center - Direction * 130, Direction, 95, 85, length: 115, duration: 65, style: 1);
        if (t >= 185) Navigate(Aim + new Vector2(-AttackSide * 1050, -300), 24, .04f);
        if (t == 245)
            for (int i = -6; i <= 6; i++)
                if (Math.Abs(i) > 1) boss.SpawnShot(TideShot.Current, Aim + new Vector2(i * 120, 450), new Vector2(0, -8),
                    95, 60, duration: 160, style: 1);
        Charge = IsharmlaRules.Envelope(t, 45, 100); Jaw = MathHelper.Lerp(Jaw, ContactActive ? .18f : 0, .1f);
    }

    private void AtomicScore(Isharmla boss, Player p)
    {
        int t = LocalTimer;
        if (t < 115)
        {
            if (t < 65) Navigate(Aim + new Vector2(AttackSide * 800, -120), 24, .065f);
            else if (t < 85) LineUp(p.Center - NPC.Center, 5);
            if (t >= 85)
                Direction = FacingAngle.AngleTowards((p.Center - Muzzle).ToRotation(), .025f).ToRotationVector2();
        }
        if (t is >= 85 and < 205) NPC.velocity = IsharmlaMotion.Brake(NPC.velocity, .10f, .85f);
        if (t is > 205 and <= 455) NPC.velocity = IsharmlaMotion.Brake(NPC.velocity, .085f, .48f);
        Charge = IsharmlaRules.Smooth((t - 100) / 90f) * (1 - IsharmlaRules.Smooth((t - 400) / 65f));
        Jaw = MathHelper.Lerp(Jaw, t < 425 ? Charge * .32f : 0, .08f);
        if (t == 115)
        {
            IsharmlaPresentation.Cue(5, Muzzle);
            boss.SpawnShot(TideShot.Breath, Muzzle, Direction, 145, 90, 2600, 76, 210);
        }
        if (t is >= 115 and < 205)
            Direction = FacingAngle.AngleTowards((p.Center - Muzzle).ToRotation(), .012f).ToRotationVector2();
        if (t == 205)
        {
            IsharmlaPresentation.Impact(Muzzle, 2.2f); NPC.velocity = -Direction * 8;
        }
        if (t is >= 205 and < 415)
            Direction = FacingAngle.AngleTowards((p.Center - Muzzle).ToRotation(), IsharmlaRules.BreathTurnRate(Vector2.Distance(p.Center, Muzzle)) * .65f).ToRotationVector2();
        if (t == 420) IsharmlaPresentation.Cue(3, Muzzle);
        if (t > 455) Navigate(Aim + new Vector2(AttackSide * 1100, -450), 17, .03f);
    }

    public float SegmentCharge(int order)
    {
        if (Dying > 0 || Boss is not { Transition: 0 }) return 0;
        if (Pattern == 1 && order < 16 && order % 3 == 1)
        {
            int shot = 115 + order * 5;
            return Math.Max(IsharmlaRules.Envelope(LocalTimer, shot - 34, shot + 8, 20),
                IsharmlaRules.Envelope(LocalTimer, shot + 126, shot + 168, 20));
        }
        return Pattern == 5 ? IsharmlaRules.Envelope(LocalTimer - order * 4, 92, 205, 45) : 0;
    }
    internal void FireSegment(IsharmlaSegment part)
    {
        if (!Authority || Dying > 0 || Boss is not { Transition: 0 } boss || boss.Stage == TideStage.LastSong ||
            Pattern != 1 || part.Order >= 16 || part.Order % 3 != 1) return;
        int time = 115 + part.Order * 5;
        if (LocalTimer != time && LocalTimer != time + 160) return;
        Vector2 target = Main.player[NPC.target].Center;
        if (Vector2.DistanceSquared(target, part.NPC.Center) < 300 * 300) return;
        Vector2 normal = (part.NPC.rotation + IsharmlaRules.BodyArtAngle + MathHelper.PiOver2).ToRotationVector2();
        if (Vector2.Dot(normal, target - part.NPC.Center) < 0) normal = -normal;
        for (int layer = 0; layer < (boss.Stage == TideStage.LastTide ? 3 : 2); layer++)
            for (int i = -3; i <= 3; i++)
                boss.SpawnShot(TideShot.FoamArc, part.NPC.Center + normal * 75,
                    normal.RotatedBy(i * .14f + layer * .06f) * (6 + layer * 2.2f), 95, 24, duration: 190, style: layer);
        IsharmlaPresentation.Cue(6, part.NPC.Center);
    }
    private void WaterEvents(Isharmla boss)
    {
        bool above = NPC.Center.Y < boss.Anchor.Y;
        if (waterCooldown > 0) waterCooldown--;
        if (above != wasAbove && waterCooldown == 0)
        {
            waterCooldown = 75;
            Vector2 point = new(NPC.Center.X, boss.Anchor.Y);
            if (!Main.dedServ) { TidalParticles.Splash(point, !above, 1.5f); IsharmlaPresentation.Impact(point, 1.1f); }
            if (Pattern == 0 && LocalTimer > 90 && LocalTimer < 230)
                for (int i = -6; i <= 6; i++)
                    if (i != 0) boss.SpawnShot(TideShot.FoamArc, point, new Vector2(i * 1.5f, -8 - Math.Abs(i) * .4f),
                        95, 38, duration: 170, style: 2);
        }
        wasAbove = above;
    }
    private void FinaleMovement(Isharmla boss)
    {
        Dying++; NPC.life = 1; NPC.damage = 0; NPC.dontTakeDamage = true; ContactActive = false; Charge = 0;
        int t = boss.Timer;
        if (t < 360) Navigate(boss.Aim + new Vector2(MathF.Sin(t * .011f) * 800, -240 + MathF.Cos(t * .011f) * 340), 22, .04f);
        else if (t < 570) Navigate(boss.Aim + new Vector2(650, -900), 18, .032f);
        else { NPC.velocity *= .97f; NPC.velocity.Y -= .025f; }
        Face(); Jaw = MathHelper.Lerp(Jaw, 0, .04f);
        if (t == 540) IsharmlaPresentation.Impact(NPC.Center, 1.8f);
        if (t >= 810 && Authority)
        { NPC.life = 0; NPC.checkDead(); NPC.netUpdate = true; }
    }
}
