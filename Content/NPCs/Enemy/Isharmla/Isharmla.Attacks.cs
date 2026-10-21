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
        int i = Projectile.NewProjectile(NPC.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<IsharmlaHazard>(), damage, 0, Main.myPlayer);
        if (i >= Main.maxProjectiles) return;
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
        int duration = fierce ? 210 : 285;
        Cast = Timer % duration;
        if (Cast == 0 && Authority)
        {
            int previous = Attack; int next;
            do next = Main.rand.Next(fierce ? 5 : 4); while (next == previous);
            NPC.ai[2] = next; Aim = p.Center; NPC.netUpdate = true;
        }
        Vector2 muzzle = NPC.Center + new Vector2(30 * NPC.spriteDirection, -26);
        if (fierce)
        {
            if (Attack == 3 && Cast < 75) Hover(p.Center + new Vector2(Side(p) * 530, -100), .09f, 22);
            else if (Attack == 3 && Cast == 85)
            {
                if (Authority) { Aim = p.Center; NPC.velocity = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX) * 30; NPC.netUpdate = true; }
            }
            else if (Attack == 3 && Cast is >= 85 and < 115) { }
            else Hover(p.Center + new Vector2(Side(p) * 480, -230), .04f, 16);
        }
        if (Cast < 35) return;
        switch (Attack)
        {
            case 0:
                if (Cast % (fierce ? 27 : 50) == 0) Fan(muzzle, p.Center, fierce ? 11 : 7, fierce ? 1.8f : 1.4f, fierce ? 13 : 8);
                break;
            case 1:
                if (Cast == 40 || fierce && Cast == 125) Columns(p, fierce);
                if (Cast % 70 == 0) Fan(muzzle, p.Center, fierce ? 3 : 1, .7f, 5, true);
                break;
            case 2:
                if (Cast == 40 || fierce && Cast == 135)
                    for (int i = -1; i <= 1; i += 2)
                        SpawnSeaborn(0, new Vector2(p.Center.X + i * 410, Anchor.Y + 80), new Vector2(i, 0));
                if (Cast % (fierce ? 30 : 60) == 0) Fan(muzzle, p.Center, fierce ? 7 : 3, 1.1f, 9);
                break;
            case 3:
                if (!fierce && Cast == 45 || fierce && Cast == 140)
                    for (int i = -1; i <= 1; i += 2)
                        SpawnShot(TideShot.RainCloud, p.Center + new Vector2(i * 310, -420), Vector2.Zero, 90, 45, 0, 150, fierce ? 130 : 180);
                if (fierce && Cast == 120) Fan(muzzle, p.Center, 13, 2.2f, 10);
                break;
            case 4:
                if (Cast % 42 == 0) Lattice(p, Cast / 42, false);
                break;
        }
    }
    private void CastTogether(Player p, IsharmlaDragon dragon)
    {
        if (dragon.LocalTimer < 65 || dragon.LocalTimer > 190) return;
        Vector2 origin = NPC.Center;
        switch (dragon.Pattern % 3)
        {
            case 0:
                if (Timer % 60 == 0) Fan(origin, p.Center, 7, 1.5f, 10);
                break;
            case 1:
                if (dragon.LocalTimer == 80) Columns(p, false);
                if (Timer % 100 == 0) Fan(origin, p.Center, 2, .65f, 5, true);
                break;
            case 2:
                if (dragon.LocalTimer == 75)
                    SpawnShot(TideShot.RainCloud, p.Center + new Vector2(Side(p) * -300, -420), Vector2.Zero, 85, 45, 0, 165, 160);
                if (Timer % 65 == 0) Fan(origin, p.Center, 5, 1.2f, 10);
                break;
        }
    }
    private void Lattice(Player p, int cycle, bool final)
    {
        bool horizontal = cycle % 2 == 1;
        float spacing = final ? 150 : 190;
        int gap = cycle % 3 - 1;
        for (int i = -6; i <= 6; i++)
        {
            if (i == gap || i == gap + 1) continue;
            Vector2 origin = p.Center + (horizontal ? new Vector2(-1250, i * spacing) : new Vector2(i * spacing, -1000));
            SpawnShot(TideShot.Lattice, origin, horizontal ? Vector2.UnitX : Vector2.UnitY,
                final ? 125 : 105, final ? 65 : 85, horizontal ? 2500 : 2000, final ? 27 : 24, 20);
        }
    }
    private void RunFinale(Player p)
    {
        int pattern = IsharmlaRules.FinalePattern(Timer), age = Timer % 540;
        if (age == 470 && Authority) Cleanup(false);
        if (age < 65 || age > 460) return;
        switch (pattern)
        {
            case 0:
                if (age % 95 == 0) Lattice(p, age / 95, true);
                break;
            case 1:
                if (age % 80 == 0) Columns(p, true);
                if (age % 40 == 0) Fan(NPC.Center, p.Center, 13, 2.7f, 12);
                break;
            case 2:
                if (age % 100 == 0) Lattice(p, age / 100 + 1, true);
                if (age % 90 == 45) Fan(NPC.Center, p.Center, 3, .6f, 5, true);
                break;
            case 3:
                if (age % 84 == 0) Lattice(p, age / 84, true);
                if (age % 42 == 21) Fan(NPC.Center, p.Center, 17, 3.5f, 11);
                break;
        }
    }
}
