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
        NPC.damage = boss.Stage == TideStage.LastTide ? 260 : 220;
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
            { Navigate(boss.Portal + new Vector2(MathF.Sin(boss.Transition * .025f) * 450, 580), 16, .055f); }
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
            case TideMotion.Breach: HuntAssault(boss, p, final); break;
            case TideMotion.Broadside: BroadsideAssault(boss, p, final); break;
            case TideMotion.Spiral: CoilAssault(boss, p, final); break;
            case TideMotion.Gate: GateAssault(boss, p, final); break;
            case TideMotion.Undertow: DiveAssault(boss, p, final); break;
            case TideMotion.AtomicBreath: BreathAssault(boss, p); break;
            case TideMotion.LaserWall: LaserWallAssault(boss,p); break;
        }
        ContainMovement(); Face(); WaterEvents(boss);
        PassPressure(p);
        if (!Main.dedServ)
        {
            IsharmlaVisuals.DragonWake(NPC.Center, NPC.velocity, 85, 0, final);
            IsharmlaPresentation.DragonParticles(this);
        }
        if (Authority && LocalTimer % 60 == 0) NPC.netUpdate = true;
    }

    private void SelectPattern(bool final, bool reset = false)
    {
        if (reset) { bagPosition = 7; motionSequence = 0; }
        int count = final ? 7 : 5;
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
            int opener = final ? 6 : Boss.Stage == TideStage.Leviathan ? 0 : 1;
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
        bool breathing = Pattern == 5 && LocalTimer is >= 60 and <= 306 && Dying == 0 && Boss?.Stage != TideStage.Finale;
        if (NPC.velocity.LengthSquared() > 1 && !breathing) Direction = NPC.velocity.SafeNormalize(Direction);
        NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle;
    }
    private void Lock(Player p)
    { if (Authority) { Aim = IsharmlaRules.BoundedAim(p.Center + p.velocity * 10); NPC.netUpdate = true; } }

    public float SegmentCharge(int order)
    {
        if (Dying > 0 || Boss is not { Transition: 0 }) return 0;
        if (Pattern == 1 && order < 16 && order % 3 == 1)
        {
            int shot = 65 + order * 4;
            return Math.Max(IsharmlaRules.Envelope(LocalTimer, shot - 34, shot + 8, 20),
                IsharmlaRules.Envelope(LocalTimer, shot + 126, shot + 168, 20));
        }
        return Pattern == 5 ? IsharmlaRules.Envelope(LocalTimer - order * 4, 92, 205, 45) : 0;
    }
    internal void FireSegment(IsharmlaSegment part)
    {
        if (!Authority || Dying > 0 || Boss is not { Transition: 0 } boss || boss.Stage == TideStage.LastSong ||
            Pattern != 1 || part.Order >= 16 || part.Order % 3 != 1) return;
        int time = 65 + part.Order * 4;
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
