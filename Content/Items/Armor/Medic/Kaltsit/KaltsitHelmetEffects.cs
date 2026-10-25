using System.Collections.Generic;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Kaltsit;

internal static class KaltsitHelmetEffects
{
    internal static void OnMon3trDepleted(Player owner, Vector2 center)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !owner.active || owner.dead
            || owner.armor[0].type != NeoArmorReforgeSetLoader.GetSetType<KaltsitHead>()) return;

        HashSet<int> damaged = [];
        foreach (NPC target in Main.ActiveNPCs)
        {
            if (!target.CanBeChasedBy() || target.DistanceSQ(center) > 800f * 800f) continue;
            target.AddBuff(ModContent.BuffType<StunDebuff>(), 3 * 60);
            target.netUpdate = true;
            // Segments sharing one health pool receive only one 1200-damage hit.
            int root = target.realLife >= 0 ? target.realLife : target.whoAmI;
            if (!damaged.Add(root)) continue;
            NPC.HitInfo hit = new()
            {
                Damage = 1200,
                Knockback = 0f,
                HitDirection = target.Center.X >= center.X ? 1 : -1,
                Crit = false,
                DamageType = DamageClass.Generic
            };
            // HitInfo.Damage is final damage: no defense, variance or player scaling.
            target.PlayerInteraction(owner.whoAmI);
            target.StrikeNPC(hit);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(target, hit);
        }
    }
}
