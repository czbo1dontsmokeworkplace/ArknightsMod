using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class Isharmla
{
    internal void SpawnShot(TideShot kind, Vector2 position, Vector2 velocity, int damage = 85, int delay = 24,
        float length = 900, float width = 36, int duration = 60)
    {
        if (!Authority) return;
        bool line = IsharmlaRules.IsLineShot(kind);
        if (line && !IsharmlaRules.AllowsLineShot(SpawnedShots, SpawnedLines)) return;
        int i = Projectile.NewProjectile(NPC.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<IsharmlaHazard>(), damage, 0, Main.myPlayer);
        if (i >= Main.maxProjectiles) return;
        SpawnedShots++; if (line) SpawnedLines++;
        var shot = (IsharmlaHazard)Main.projectile[i].ModProjectile;
        shot.Bind(this, kind, velocity, delay, length, width, duration); Main.projectile[i].netUpdate = true;
    }
    internal void Fan(Vector2 origin, Vector2 aim, int count, float spread, float speed, bool seekers = false)
    {
        if (!Authority) return;
        Vector2 direction = (aim - origin).SafeNormalize(Vector2.UnitY);
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0 : MathHelper.Lerp(-spread / 2, spread / 2, i / (float)(count - 1));
            SpawnShot(seekers ? TideShot.Seeker : TideShot.Pearl, origin, direction.RotatedBy(angle) * speed, 85, 22);
        }
    }
    internal void Columns(Player player, bool fierce)
    {
        // Snapshot positions once. The central gap is intentionally wide enough for a player.
        float spacing = fierce ? 176 : 210;
        for (int i = -3; i <= 3; i++)
        {
            if (i == 0) continue;
            SpawnShot(TideShot.Column, new Vector2(player.Center.X + i * spacing, Anchor.Y + 100), -Vector2.UnitY,
                fierce ? 115 : 90, fierce ? 65 : 85, 1150, fierce ? 48 : 38, 30);
        }
    }
    internal void SpawnSeaborn(int kind, Vector2 origin, Vector2 velocity)
    {
        if (!Authority) return;
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is IsharmlaSeaborn s && s.OwnerIndex == NPC.whoAmI && s.Encounter == Encounter) count++;
        if (count >= 8) return;
        int slot = Terraria.NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, ModContent.NPCType<IsharmlaSeaborn>());
        if (slot >= Main.maxNPCs) return;
        var seaborn = (IsharmlaSeaborn)Main.npc[slot].ModNPC;
        seaborn.Bind(this, kind, origin, velocity); Main.npc[slot].netUpdate = true;
    }
    private void CastSpells(Player p, bool fierce)
    {
        int duration = fierce ? 240 : 300;
        Cast = Timer % duration;
        if (Cast == 0 && Authority)
        {
            // A complete eight-phrase song: every phrase mixes a lead and an accompaniment.
            NPC.ai[2] = (Attack + 3) % 8; Aim = p.Center; NPC.netUpdate = true;
        }
        if (Cast == 1 && Authority) { Aim = p.Center + p.velocity * 10; NPC.netUpdate = true; }
        Vector2 muzzle = NPC.Center + new Vector2(30 * NPC.spriteDirection, -26);
        if (fierce)
        {
            if (Attack == 7 && Cast < 75) Hover(p.Center + new Vector2(Side(p) * 530, -100), .09f, 22);
            else if (Attack == 7 && Cast == 85)
            {
                if (Authority) { Aim = p.Center; NPC.velocity = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX) * 30; NPC.netUpdate = true; }
            }
            else if (Attack == 7 && Cast is >= 85 and < 111) { }
            else Hover(p.Center + new Vector2(Side(p) * 480, -230 - MathF.Sin(Timer * .014f) * 110), .04f, 16);
        }
        if (Cast < 35 || Cast > 190) return;
        int beat = fierce ? 24 : 40;
        switch (Attack)
        {
            case 0:
                if (Cast % beat == 0) Chord(muzzle, p.Center, fierce ? 7 : 5, 1.25f, fierce ? 12 : 8);
                if (Cast == 70 || fierce && Cast == 142) BubbleRing(muzzle, fierce ? 12 : 8, 5, Cast * .05f);
                if (Cast == 110) CharmPair(muzzle);
                break;
            case 1:
                // The sweep locks its centre before firing; it does not track back onto a dodge.
                if (Cast is >= 40 and <= 154 && Cast % (fierce ? 5 : 8) == 0)
                    SpawnShot(TideShot.Note, muzzle, (Aim - muzzle).SafeNormalize(Vector2.UnitY)
                        .RotatedBy(MathHelper.Lerp(-.95f, .95f, (Cast - 40) / 114f)) * (fierce ? 15 : 10), 90, 18);
                if (Cast == 60 || Cast == 130) Skyfall(p, fierce, TideShot.StormSpark);
                break;
            case 2:
                if (Cast == 40) CharmPair(muzzle);
                if (Cast % (fierce ? 36 : 60) == 0) CrossCurrent(p, fierce, Cast / 36);
                if (Cast == 110)
                    for (int side = -1; side <= 1; side += 2)
                        SpawnShot(TideShot.Typhoon, muzzle, new Vector2(side * 6, -3), 100, 30);
                break;
            case 3:
                if (Cast == 40)
                    for (int i = -1; i <= 1; i += 2)
                        SpawnShot(TideShot.RainCloud, p.Center + new Vector2(i * 310, -420), Vector2.Zero, 90, 45, 0, 150, fierce ? 130 : 180);
                if (Cast % (fierce ? 38 : 60) == 0) Skyfall(p, fierce, TideShot.Note);
                if (Cast == 100) Chord(muzzle, p.Center, 5, 1.6f, 9);
                break;
            case 4:
                if (Cast % (fierce ? 32 : 50) == 0)
                {
                    for (int i = -3; i <= 3; i++)
                        if (i != 0) SpawnShot(TideShot.StormSpark, p.Center + new Vector2(i * 130, 430),
                            new Vector2(MathF.Sin(i) * 1.5f, fierce ? -13 : -9), 100, 52);
                }
                if (Cast == 70 || Cast == 150) CrossCurrent(p, fierce, Cast / 70);
                break;
            case 5:
                if (Cast == 40 || Cast == 115) BubbleRing(muzzle, fierce ? 16 : 12, fierce ? 7 : 5, Cast * .045f);
                if (Cast % beat == 0)
                    Chord(muzzle, p.Center, 3, .65f, 10, Cast % (beat * 2) == 0);
                if (fierce && Cast == 160) Skyfall(p, true, TideShot.StormSpark);
                break;
            case 6:
                if (Cast == 40)
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 offset = new Vector2(i % 2 == 0 ? -440 : 440, i < 2 ? -270 : 270);
                        SpawnShot(TideShot.MistField, p.Center + offset, Vector2.Zero, 100, 65, 0, 75, 120);
                    }
                if (Cast == 85 || Cast == 150)
                    for (int i = -1; i <= 1; i += 2)
                        SpawnShot(TideShot.Bomb, p.Center + new Vector2(i * 350, i * (Cast == 85 ? 180 : -180)), Vector2.Zero, 115, 65, 0, 80, 24);
                if (Cast % beat == 0) Chord(muzzle, p.Center, 3, .85f, 10);
                break;
            case 7:
                if (Cast == 40 || Cast == 150) Skyfall(p, fierce, TideShot.Note);
                if (Cast is >= 90 and <= 140 && Cast % 10 == 0)
                    Chord(muzzle, p.Center, fierce ? 3 : 2, 1.4f, fierce ? 12 : 8);
                if (Cast == 160) BubbleRing(muzzle, 10, 6, .2f);
                break;
        }
    }
    private void CastTogether(Player p, IsharmlaDragon dragon)
    {
        bool final = Stage == TideStage.LastTide;
        int t = dragon.LocalTimer;
        if (t < 40 || t > (dragon.Pattern == 5 ? 270 : 215) || dragon.Visibility < .7f) return;
        Vector2 origin = NPC.Center + new Vector2(0, -28);
        // The rider shapes escape routes alongside the dragon's current movement.
        switch (dragon.Pattern)
        {
            case 0:
                if (t % (final ? 28 : 42) == 0) Chord(origin, p.Center, 5, 1.5f, final ? 13 : 10);
                if (t == 110) Skyfall(p, final, TideShot.Note);
                break;
            case 1:
                if (t == 70 || t == 150) CrossCurrent(p, final, t / 70);
                if (t == 110) CharmPair(origin);
                break;
            case 2:
                if (t == 70)
                    SpawnShot(TideShot.RainCloud, p.Center + new Vector2(-dragon.AttackSide * 360, -430), Vector2.Zero, 90, 55, 0, 165, 130);
                if (t % 36 == 0) Chord(origin, p.Center, final ? 5 : 3, 1.0f, 11);
                break;
            case 3:
                if (t % 48 == 0) CrossCurrent(p, final, t / 48);
                if (t == 160) CharmPair(origin);
                break;
            case 4:
                if (t == 65 || t == 140) Skyfall(p, final, TideShot.StormSpark);
                if (t % 36 == 0) Chord(origin, p.Center, 3, .9f, 10);
                break;
            case 5:
                if (t == 65 || t == 170)
                    for (int side = -1; side <= 1; side += 2)
                        SpawnShot(TideShot.MistField, p.Center + new Vector2(side * 440, 220), Vector2.Zero, 110, 65, 0, 80, 115);
                if (t % 40 == 0) Chord(origin, p.Center, 3, 1.3f, 9);
                break;
        }
        if (final && t % 60 == 30) BubbleRing(origin, 8, 5, t * .035f);
    }
    internal void Chord(Vector2 origin, Vector2 aim, int count, float spread, float speed, bool charm = false)
    {
        Vector2 forward = (aim - origin).SafeNormalize(Vector2.UnitY);
        for (int i = 0; i < count; i++)
            SpawnShot(charm ? TideShot.CharmNote : TideShot.Note, origin,
                forward.RotatedBy(count == 1 ? 0 : MathHelper.Lerp(-spread / 2, spread / 2, i / (float)(count - 1))) * speed, 90, 24);
    }
    internal void BubbleRing(Vector2 origin, int count, float speed, float rotation)
    {
        for (int i = 0; i < count; i++) SpawnShot(TideShot.Bubble, origin,
            (rotation + i * MathHelper.TwoPi / count).ToRotationVector2() * speed, 90, 26);
    }
    private void CharmPair(Vector2 origin)
    {
        for (int i = -1; i <= 1; i += 2)
            SpawnShot(TideShot.CharmNote, origin + new Vector2(i * 24, 0), new Vector2(i * 5, -4), 95, 30);
    }
    private void Skyfall(Player p, bool fierce, TideShot kind)
    {
        for (int i = -3; i <= 3; i++)
        {
            if (i == 0) continue;
            SpawnShot(kind, p.Center + new Vector2(i * 145, -490 - Math.Abs(i) * 25),
                new Vector2(-i * .35f, fierce ? 12 : 8), 95, 45 + Math.Abs(i) * 4);
        }
    }
    private void CrossCurrent(Player p, bool fierce, int beat)
    {
        int side = beat % 2 == 0 ? -1 : 1;
        for (int lane = -2; lane <= 2; lane++)
        {
            if (lane == 0) continue;
            SpawnShot(TideShot.Note, p.Center + new Vector2(side * 640, lane * 115),
                new Vector2(-side * (fierce ? 13 : 9), lane * -.4f), 95, 46);
        }
    }
    private void RunFinale(Player p)
    {
        int pattern = IsharmlaRules.FinalePattern(Timer), age = Timer % 540;
        if (age == 520 && Authority) Cleanup(false);
        if (age < 65 || age > 425 || (age - 65) % 90 != 0) return;
        int wave = (age - 65) / 90;
        Vector2 centre = p.Center;
        Vector2[] offsets = IsharmlaRules.FinaleOffsets(pattern, wave);
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 zone = centre + offsets[i];
            bool mist = (i + wave + pattern) % 3 == 0;
            SpawnShot(mist ? TideShot.MistField : TideShot.Bomb, zone, Vector2.Zero, 125, 75 + i * 5, 0, mist ? 82 : 90, mist ? 125 : 24);
            // Three illustrated notes emerge from each zone; the vanished singer is never a muzzle.
            Chord(zone, centre + offsets[(i + 1) % offsets.Length] * .3f, 3, .9f, 7);
        }
        if (pattern == 1 || pattern == 3) return;
        // Lasers are occasional punctuation. SpawnShot enforces the encounter's 20% ceiling.
        int orientation = (wave + pattern) % 2;
        for (int side = -1; side <= 1; side += 2)
            SpawnShot(TideShot.Lattice, centre + (orientation == 0 ? new Vector2(side * 360, -950) : new Vector2(-1100, side * 280)),
                orientation == 0 ? Vector2.UnitY : Vector2.UnitX, 125, 85, orientation == 0 ? 1900 : 2200, 26, 28);
    }
}
