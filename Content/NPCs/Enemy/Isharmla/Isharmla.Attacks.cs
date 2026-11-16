using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class Isharmla
{
    internal IsharmlaHazard SpawnShot(TideShot kind, Vector2 position, Vector2 velocity, int damage = 85, int delay = 24,
        float length = 900, float width = 36, int duration = 60, int style = 0, Vector2 target = default)
    {
        if (!Authority || Cleaning) return null;
        int active = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is IsharmlaHazard h && h.OwnerIndex == NPC.whoAmI && h.Encounter == Encounter) active++;
        if (active >= IsharmlaRules.HazardBudget) return null;
        bool line = IsharmlaRules.IsLineShot(kind);
        // Signature attacks must not disappear because of a historical projectile ratio.
        if (kind == TideShot.Breath)
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is IsharmlaHazard h && h.OwnerIndex == NPC.whoAmI && h.Encounter == Encounter && h.Kind == TideShot.Breath) return null;
        int i = Projectile.NewProjectile(NPC.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<IsharmlaHazard>(), damage, 0, Main.myPlayer);
        if (i >= Main.maxProjectiles) return null;
        SpawnedShots++; if (line) SpawnedLines++;
        var shot = (IsharmlaHazard)Main.projectile[i].ModProjectile;
        shot.Bind(this, kind, velocity, delay, length, width, duration);
        shot.Style = style; shot.Target = target; Main.projectile[i].netUpdate = true;
        return shot;
    }
    internal void Fan(Vector2 origin, Vector2 aim, int count, float spread, float speed, bool seekers = false)
        => Score(origin, (aim - origin).SafeNormalize(Vector2.UnitY), count, spread, speed, seekers ? 1 : 0);
    internal void Chord(Vector2 origin, Vector2 aim, int count, float spread, float speed, bool charm = false)
        => Score(origin, (aim - origin).SafeNormalize(Vector2.UnitY), count, spread, speed, charm ? 1 : 0);
    internal void Score(Vector2 origin, Vector2 direction, int count, float spread, float speed, int style = 0, int delay = 12)
    {
        if (!Authority) return;
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0 : MathHelper.Lerp(-spread * .5f, spread * .5f, i / (float)(count - 1));
            SpawnShot(TideShot.Harpoon, origin, direction.RotatedBy(angle) * speed, 105, Math.Max(18, delay), duration: 180, style: style,
                target: Main.player[NPC.target].Center + Main.player[NPC.target].velocity * 12);
        }
    }
    internal void BubbleRing(Vector2 origin, int count, float speed, float rotation)
    {
        for (int i = 0; i < count; i++)
            SpawnShot(TideShot.FoamArc, origin, (rotation + i * MathHelper.TwoPi / count).ToRotationVector2() * speed,
                95, 22, duration: 190, style: i % 2);
    }
    internal void SpawnSeaborn(int kind, Vector2 origin, Vector2 velocity)
    {
        if (!Authority) return;
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is IsharmlaSeaborn s && s.OwnerIndex == NPC.whoAmI && s.Encounter == Encounter) count++;
        if (count >= 4) return;
        int slot = Terraria.NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, ModContent.NPCType<IsharmlaSeaborn>());
        if (slot >= Main.maxNPCs) return;
        ((IsharmlaSeaborn)Main.npc[slot].ModNPC).Bind(this, kind, origin, velocity); Main.npc[slot].netUpdate = true;
    }
    private void BeginPhrase(Player p, int cycle)
    {
        if (!Authority || PhraseSerial == cycle) return;
        PhraseSerial = cycle; NPC.ai[2] = cycle % 4;
        PhraseAnchor = Aim = IsharmlaRules.BoundedAim(p.Center + p.velocity * 12);
        PhraseSide = NPC.Center.X > p.Center.X ? 1 : -1;
        DashDirection = Vector2.Zero; NPC.netUpdate = true;
    }
    private void CastSpells(Player p, bool fierce)
    {
        int duration = IsharmlaRules.HumanPhraseLength(fierce);
        Cast = Math.Max(0, Timer) % duration;
        BeginPhrase(p, Math.Max(0, Timer) / duration);
        Lean = MathHelper.Lerp(Lean, MathHelper.Clamp(NPC.velocity.X * .014f, -.30f, .30f), .12f);
        Gesture = IsharmlaRules.Envelope(Cast, 24, fierce ? 260 : 330);
        RunTrialScore(p,Cast,Attack,fierce,PhraseAnchor,NPC.Center);
        switch (Attack)
        {
            case 0: ProcessionalSong(p, fierce); break;
            case 1: EchoChoir(p, fierce); break;
            case 2: TideStep(p, fierce); break;
            case 3: ConductTide(p, fierce); break;
        }
    }
    private Vector2 Intercept(Player p, float lead = 14)
        => IsharmlaRules.BoundedAim(p.Center + Vector2.Clamp(p.velocity * lead, new Vector2(-340), new Vector2(340)));
    private void ProcessionalSong(Player p,bool fierce)
    {
        float phase=Cast/(float)IsharmlaRules.HumanPhraseLength(fierce);
        Hover(Intercept(p,8)+new Vector2(PhraseSide*(620-phase*1000),-260+MathF.Sin(phase*MathHelper.TwoPi)*180),.16f,fierce?29:25);
        if(Cast==24)IsharmlaPresentation.Cue(0,NPC.Center);
    }
    private void EchoChoir(Player p,bool fierce)
    {
        float angle=Cast*.018f*PhraseSide;
        Hover(Intercept(p,7)+new Vector2(MathF.Cos(angle)*660,MathF.Sin(angle)*290-200),.15f,28);
        if(Cast is 48 or 172)IsharmlaPresentation.Cue(1,NPC.Center);
    }
    private void TideStep(Player p, bool fierce)
    {
        int beat = Cast % (fierce ? 96 : 120);
        if (beat < 42)
        {
            Hover(Intercept(p, 12) + new Vector2(PhraseSide * 650, beat < 24 ? -250 : -80), .18f, 30);
            if (beat == 18)
            {
                if (Authority) { Aim = Intercept(p, 18); NPC.netUpdate = true; }
                IsharmlaPresentation.Cue(2, NPC.Center);
            }
        }
        if (beat == 42 && Authority)
        { DashDirection = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX); NPC.netUpdate = true; }
        if (beat is >= 42 and < 72)
        {
            NPC.velocity = IsharmlaMotion.Rush(NPC.velocity, DashDirection, fierce ? 39 : 33, beat - 42, 30, .22f);
            if (beat % 10 == 2) SpawnShot(TideShot.WakeMark, NPC.Center, DashDirection, 110, 50, duration: 48);
        }
        if (beat == 73)
        {
            PhraseSide *= -1; if (Authority) NPC.netUpdate = true;
            Score(NPC.Center, (Intercept(p, 10) - NPC.Center).SafeNormalize(Vector2.UnitY), 7, 1.1f, 13, 0, 30);
        }
        if (beat >= 72) Hover(Intercept(p, 8) + new Vector2(PhraseSide * 580, -280), .16f, 28);
        Lean = MathHelper.Clamp(NPC.velocity.X * .012f, -.45f, .45f);
    }
    private void ConductTide(Player p,bool fierce)
    {
        Hover(Intercept(p,8)+new Vector2(MathF.Sin(Cast*.023f)*620,-420),.15f,27);
        if(Cast is 54 or 210)IsharmlaPresentation.Cue(1,NPC.Center);
    }
    private void CastTogether(Player p, IsharmlaDragon dragon)
    {
        int t = dragon.LocalTimer; Cast = t; NPC.ai[2] = dragon.Pattern;
        bool final = Stage == TideStage.LastTide;
        Gesture = IsharmlaRules.Envelope(t, 40, IsharmlaRules.MotionLength(dragon.Pattern, final) - 60);
        if (!Mounted)
        {
            Vector2 destination = dragon.Aim + new Vector2(-dragon.AttackSide * 510, -310 + MathF.Sin(t * .025f) * 60);
            if (dragon.Pattern == 1) destination = dragon.Aim + new Vector2(-dragon.AttackSide * 300, -420);
            if (dragon.Pattern == 2) destination = dragon.Aim + new Vector2(0, -350);
            Hover(destination, .13f, 24);
        }
        if(dragon.Pattern==6)return; // The laser-wall sequence owns this entire window.
        RunTrialScore(p,t,dragon.Pattern%4,final,dragon.Aim,NPC.Center);
        if (dragon.Visibility < .7f || t < 70) return;
        Vector2 origin = NPC.Center + new Vector2(0, -30);
        switch (dragon.Pattern)
        {
            case 0:
                if (t is 170 or 218 or 266)
                {
                    for (int i = -5; i <= 5; i++)
                        if (Math.Abs(i) > 1) SpawnShot(TideShot.Canticle, dragon.Aim + new Vector2(i * 125, -540),
                            new Vector2(0, final ? 10 : 8), 90, 42, duration: 180, style: 0);
                    IsharmlaPresentation.Cue(1, origin);
                }
                break;
            case 1:
                if (t is 84 or 244)
                {
                    int side = t < 200 ? dragon.AttackSide : -dragon.AttackSide;
                    for (int i = -5; i <= 5; i++)
                        if (Math.Abs(i) > 1) SpawnShot(TideShot.Canticle, dragon.Aim + new Vector2(-side * 680, i * 95),
                            new Vector2(side * 9, 0), 90, 38, duration: 190, style: 1);
                }
                break;
            case 2:
                if (t is 120 or 180 or 240) BubbleRing(origin, final ? 30 : 22, 5.8f, t * .006f);
                break;
            case 3:
                if (dragon.PortalCrossed && t >= dragon.PortalExitTimer + 20 && t <= dragon.PortalExitTimer + 80 && t % 12 == 0)
                    Score(origin, -Vector2.UnitY, 3, .38f, 8, 0, 20);
                break;
            case 4:
                if (t is 100 or 168 or 236)
                    Score(origin, (dragon.Aim - origin).SafeNormalize(Vector2.UnitY), final ? 11 : 7, 1.8f, 8, 1, 24);
                break;
            case 5:
                if (t is 220 or 292 or 364 && Vector2.DistanceSquared(origin, p.Center) > 300 * 300)
                {
                    Vector2 side = dragon.Direction.RotatedBy(MathHelper.PiOver2);
                    for (int sign = -1; sign <= 1; sign += 2)
                        Score(origin - dragon.Direction * 120, (-dragon.Direction + side * sign * .75f).SafeNormalize(Vector2.UnitY), 5, .55f, 7, 1, 20);
                }
                break;
        }
    }
    private void RunFinale(Player p)
    {
        Cast = Timer; Gesture = IsharmlaRules.Envelope(Timer, 0, 810, 60);
        if (Dragon is { } dragon && Timer < 540) Ride(dragon);
        else Hover(Aim + new Vector2(0, -180), .08f, 14);
        if (Timer is 90 or 210 or 330)
        {
            for (int side = -1; side <= 1; side += 2)
                for (int lane = -4; lane <= 4; lane++)
                    if (Math.Abs(lane) > 1) SpawnShot(TideShot.Canticle, Aim + new Vector2(side * 850, lane * 105),
                        new Vector2(-side * 8, 0), 90, 60, duration: 215, style: side > 0 ? 1 : 0);
            IsharmlaPresentation.Cue(1, NPC.Center);
        }
        if(Timer>=420&&Timer<780)RunTrialScore(p,Timer-372,1,true,Aim,NPC.Center);
        if (Timer == 820 && Authority) Cleanup(false);
        if (Timer == 860) IsharmlaPresentation.Cue(3, NPC.Center);
    }
}
