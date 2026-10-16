using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowBranchSkills
{
    internal static float DamageMultiplier(CrossbowKind kind, int skill) => (kind, skill) switch {
        (CrossbowKind.BluePoison, 1) => 1.8f, (CrossbowKind.BluePoison, 2) => 1.5f,
        (CrossbowKind.Provence, 2) => 2.4f,
        (CrossbowKind.GreyThroat, 1) => 1.4f, (CrossbowKind.GreyThroat, 2) => 1.4f,
        (CrossbowKind.Archetto, 1) => 2f, (CrossbowKind.Archetto, 2) => 1.4f,
        (CrossbowKind.Archetto, 3) => 1.3f, _ => 1
    };
    internal static Color ColorFor(CrossbowKind kind) => kind switch {
        CrossbowKind.BluePoison => new Color(98, 151, 239), CrossbowKind.Provence => new Color(177, 102, 221),
        CrossbowKind.GreyThroat => new Color(188, 216, 227), _ => new Color(245, 205, 112)
    };
    internal static void ExtraShots(Player player, Item item, Projectile main, Vector2 muzzle, Vector2 direction, float speed, CrossbowKind kind, int skill)
    {
        if (kind < CrossbowKind.BluePoison || skill == 0) return;
        int extras = (kind, skill) switch { (CrossbowKind.BluePoison, 1) => 1, (CrossbowKind.BluePoison, 2) => 2,
            (CrossbowKind.GreyThroat, 1) => 1, (CrossbowKind.Archetto, 1) => 3, (CrossbowKind.Archetto, 3) => 1, _ => 0 };
        List<NPC> targets = new();
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy(main) && npc.DistanceSQ(muzzle) < 800 * 800
                && Vector2.Dot((npc.Center - muzzle).SafeNormalize(direction), direction) > .65f
                && Collision.CanHitLine(muzzle, 1, 1, npc.position, npc.width, npc.height)) targets.Add(npc);
        targets.Sort((a, b) => a.DistanceSQ(muzzle).CompareTo(b.DistanceSQ(muzzle)));
        for (int i = 0; i < extras; i++)
        {
            Vector2 aim = i + 1 < targets.Count && kind != CrossbowKind.GreyThroat
                ? (targets[i + 1].Center - muzzle).SafeNormalize(direction)
                : direction.RotatedBy((i % 2 == 0 ? 1 : -1) * (.04f + i * .02f));
            var extra = Projectile.NewProjectileDirect(player.GetSource_ItemUse(item), muzzle, aim * speed,
                ModContent.ProjectileType<CrossbowBolt>(), main.damage, main.knockBack, player.whoAmI, (int)kind, skill);
            extra.CritChance = main.CritChance;
        }
    }
    internal static void Ricochet(Projectile shot, NPC hit)
    {
        if (shot.owner != Main.myPlayer || shot.ai[2] != 0) return;
        shot.ai[2] = 1; shot.netUpdate = true;
        NPC best = null; float range = 360 * 360;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = npc.DistanceSQ(hit.Center);
            if (npc.whoAmI == hit.whoAmI || !npc.CanBeChasedBy(shot) || distance >= range
                || !Collision.CanHitLine(hit.Center, 1, 1, npc.position, npc.width, npc.height)) continue;
            best = npc; range = distance;
        }
        if (best == null) return;
        Projectile.NewProjectile(shot.GetSource_OnHit(hit), hit.Center, (best.Center - hit.Center).SafeNormalize(Vector2.UnitX) * 20,
            ModContent.ProjectileType<CrossbowBolt>(), (int)(shot.damage * .8f), shot.knockBack, shot.owner, (int)CrossbowKind.Archetto, 0, 1);
    }
    internal static void Draw(Projectile shot, CrossbowKind kind)
    {
        Color color = ColorFor(kind);
        ChargedOperatorVfx.Trail(shot, color, CrossbowBallistics.IsPiercing(kind) ? 11 : 6);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite(kind == CrossbowKind.Provence ? "CrystalVortex" : "StarCore", shot.Center,
            new Vector2(CrossbowBallistics.IsPiercing(kind) ? 46 : 26, 14), color * .8f, shot.velocity.ToRotation());
    }
}
