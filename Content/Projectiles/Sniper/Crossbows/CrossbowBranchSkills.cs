using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowBranchSkills
{
    internal static float DamageMultiplier(CrossbowKind kind, int skill) => (kind, skill) switch {
        (CrossbowKind.Melanite, 1) => 2.2f, (CrossbowKind.Melanite, 2) => 1.65f,
        (CrossbowKind.Provence, 2) => 2.4f,
        (CrossbowKind.GreyThroat, 1) => 1.4f, (CrossbowKind.GreyThroat, 2) => 1.4f,
        (CrossbowKind.BluePoison, 1) => 1.8f, (CrossbowKind.BluePoison, 2) => 1.5f,
        _ => 1f
    };
    internal static Color ColorFor(CrossbowKind kind) => kind switch {
        CrossbowKind.Melanite => new Color(171, 99, 240), CrossbowKind.Provence => new Color(177, 102, 221),
        CrossbowKind.GreyThroat => new Color(188, 216, 227), _ => new Color(71, 207, 191)
    };
    internal static void ExtraShots(Player player, Item item, Projectile main, Vector2 muzzle,
        Vector2 direction, float speed, CrossbowKind kind, int skill)
    {
        int extras = (kind, skill) switch {
            (CrossbowKind.BluePoison, 1) => 1, (CrossbowKind.BluePoison, 2) => 2,
            (CrossbowKind.GreyThroat, 1) => 1, _ => 0 };
        // Melanite's two light strands belong to a single damaging arrow, including both skills.
        if (extras == 0) return;
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
    internal static void Draw(Projectile shot, CrossbowKind kind) => CrossbowFlightVfx.Draw(shot, kind);
}
