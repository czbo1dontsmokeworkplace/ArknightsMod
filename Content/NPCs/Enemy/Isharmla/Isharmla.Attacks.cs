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
            SpawnShot(TideShot.Canticle, origin, direction.RotatedBy(angle) * speed, 88, delay, duration: 210, style: style);
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
        switch (Attack)
        {
            case 0: ProcessionalSong(p, fierce); break;
            case 1: EchoChoir(p, fierce); break;
            case 2: TideStep(p, fierce); break;
            case 3: ConductTide(p, fierce); break;
        }
    }
    private void ProcessionalSong(Player p, bool fierce)
    {
        int end = fierce ? 252 : 312;
        float wave = MathF.Sin(Math.Max(0, Cast - 60) * .038f);
        Hover(PhraseAnchor + new Vector2(PhraseSide * 610, -140 + wave * 205), .12f, fierce ? 22 : 18);
        if (Cast == 36) IsharmlaPresentation.Cue(0, NPC.Center);
        if (Cast < 72 || Cast > end || Cast % (fierce ? 12 : 16) != 0) return;
        Vector2 muzzle = NPC.Center + new Vector2(-PhraseSide * 34, -23);
        if (Cast % 64 == 0) IsharmlaPresentation.Cue(1, muzzle);
        Score(muzzle, new Vector2(-PhraseSide, 0), fierce ? 3 : 2, .13f, 9.5f, 0);
        if (Cast % 48 == 0) Score(muzzle, new Vector2(-PhraseSide, 0), 2, .20f, 6.4f, 1, 20);
    }
    private void EchoChoir(Player p, bool fierce)
    {
        Hover(PhraseAnchor + new Vector2(PhraseSide * 520, -220), .10f, 18);
        if (Cast >= 216 && Cast <= (fierce ? 276 : 264) && (Cast - 216) % 12 == 0)
            SpawnDrifter(NPC.Center + new Vector2(-PhraseSide * 30, -20), PhraseAnchor, PhraseSide);
        if (Cast == 40) IsharmlaPresentation.Cue(0, NPC.Center);
        if (Cast == 60)
            for (int i = 0; i < 3; i++)
            {
                Vector2 at = PhraseAnchor + new Vector2(-PhraseSide * (480 - i * 230), -380 - (i % 2) * 90);
                SpawnShot(TideShot.EchoSinger, at, (PhraseAnchor - at).SafeNormalize(Vector2.UnitY) * 8.5f,
                    85, 78 + i * 28, duration: fierce ? 130 : 150, style: i, target: PhraseAnchor);
            }
        if (Cast is 96 or 144 or 192)
        {
            IsharmlaPresentation.Cue(1, NPC.Center);
            Score(NPC.Center + new Vector2(0, -25), (PhraseAnchor - NPC.Center).SafeNormalize(Vector2.UnitY),
                fierce ? 9 : 7, 1.32f, fierce ? 11 : 9, 0);
        }
    }
    private void TideStep(Player p, bool fierce)
    {
        bool dash = Cast is >= 90 and < 113 || fierce && Cast is >= 174 and < 197;
        if (Cast < 76) Hover(PhraseAnchor + new Vector2(PhraseSide * 620, -190), .13f, 22);
        else if (Cast is >= 130 and < 160 && fierce) Hover(PhraseAnchor + new Vector2(-PhraseSide * 620, 120), .15f, 25);
        else if (!dash) NPC.velocity = IsharmlaMotion.Brake(NPC.velocity, .13f, 1.65f);
        if ((Cast == 90 || fierce && Cast == 174) && Authority)
        {
            Aim = IsharmlaRules.BoundedAim(p.Center + p.velocity * 9);
            DashDirection = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX) * (fierce ? 32 : 27);
            NPC.netUpdate = true;
        }
        if (Cast is 68 or 152) IsharmlaPresentation.Cue(2, NPC.Center);
        if (dash)
        {
            int age = Cast - (Cast >= 174 ? 174 : 90);
            NPC.velocity = IsharmlaMotion.Rush(NPC.velocity, DashDirection, fierce ? 32 : 27, age, 23, .24f);
            Lean = NPC.velocity.X * .012f;
            if (Cast % 5 == 0) SpawnShot(TideShot.WakeMark, NPC.Center, DashDirection.SafeNormalize(Vector2.UnitX),
                90, 64, length: 80, duration: 50, style: fierce ? 1 : 0);
        }
        if (Cast == (fierce ? 230 : 190))
            Score(NPC.Center, (PhraseAnchor - NPC.Center).SafeNormalize(Vector2.UnitY), fierce ? 11 : 9, 1.6f, 8);
        if (!fierce && Cast > 240) Hover(PhraseAnchor + new Vector2(PhraseSide * 450, -180), .06f, 12);
    }
    private void ConductTide(Player p, bool fierce)
    {
        Hover(PhraseAnchor + new Vector2(0, -370), .10f, 18);
        if (Cast == 44) IsharmlaPresentation.Cue(0, NPC.Center);
        if (Cast is 80 or 152 or 224)
        {
            int beat = (Cast - 80) / 72;
            float gap = PhraseAnchor.X + MathF.Sin(beat * 1.5f) * 170;
            for (int i = -7; i <= 7; i++)
            {
                float x = PhraseAnchor.X + i * 110;
                if (Math.Abs(x - gap) < 135) continue;
                SpawnShot(TideShot.Current, new Vector2(x, PhraseAnchor.Y + 420), new Vector2(0, fierce ? -10 : -8),
                    95, 56, duration: 150, style: 0);
            }
            Score(NPC.Center, Vector2.UnitY, fierce ? 9 : 7, 1.9f, 6.2f, 1, 18);
        }
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
        if (Timer is 450 or 570 or 690)
        {
            float rotation = (Timer - 450) / 120 * .20f;
            for (int i = 0; i < 40; i++)
            {
                float angle = MathHelper.TwoPi * i / 40 + rotation;
                if (Math.Abs(MathHelper.WrapAngle(angle - MathHelper.PiOver2)) < .30f) continue;
                Vector2 direction = angle.ToRotationVector2();
                SpawnShot(TideShot.FoamArc, Aim + direction * 340, direction * 6.5f, 95, 55, duration: 170, style: 1);
            }
            IsharmlaPresentation.Impact(Aim, 1.5f);
        }
        if (Timer == 820 && Authority) Cleanup(false);
        if (Timer == 860) IsharmlaPresentation.Cue(3, NPC.Center);
    }
}
