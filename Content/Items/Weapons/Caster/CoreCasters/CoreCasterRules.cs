using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;

internal static class CoreCasterRules
{
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal static bool Clear(Vector2 a, Vector2 b) => Collision.CanHitLine(a, 1, 1, b, 1, 1);
    internal static int Rank(WeaponPlayer state) => Math.Clamp((state.CurrentSkill?.ForceReplaceLevel ?? state.CurrentSkill?.Level ?? 1) - 1, 0, 9);
    internal static float LifeFraction(NPC npc)
    {
        if (npc.realLife >= 0 && Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
            npc = Main.npc[npc.realLife];
        return npc.life / (float)Math.Max(1, npc.lifeMax);
    }
    internal static List<NPC> Targets(Vector2 origin, float range, Vector2 focus, bool weakest = false, float hpLimit = 1)
    {
        var targets = new List<NPC>();
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy() && npc.DistanceSQ(origin) <= range * range && LifeFraction(npc) <= hpLimit
                && Collision.CanHitLine(origin, 1, 1, npc.position, npc.width, npc.height)) targets.Add(npc);
        targets.Sort((a, b) => {
            int health = weakest ? LifeFraction(a).CompareTo(LifeFraction(b)) : 0;
            return health != 0 ? health : a.DistanceSQ(focus).CompareTo(b.DistanceSQ(focus));
        });
        // A segmented boss is one target for a multi-target cast, rather than six free targets.
        var roots = new HashSet<int>();
        targets.RemoveAll(npc => !roots.Add(npc.realLife >= 0 ? npc.realLife : npc.whoAmI));
        return targets;
    }
    internal static Projectile Spawn(IEntitySource source, Vector2 position, Vector2 velocity, int type,
        int damage, float knockback, int owner, float ai0 = 0, float ai1 = 0, float ai2 = 0, int crit = 0)
    {
        int index = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
        if (!Main.projectile.IndexInRange(index)) return null;
        Projectile p = Main.projectile[index];
        p.CritChance = crit;
        // Delayed spell children remain Arts damage even if their owner changes weapons.
        p.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        return p;
    }
}
