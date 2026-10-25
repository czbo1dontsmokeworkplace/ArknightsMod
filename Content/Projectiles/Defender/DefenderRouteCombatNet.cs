using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Projectiles.Defender;

// Owning-client melee contacts forward only bounded, deduplicated status events.
internal static class DefenderRouteCombatNet
{
    internal static void Hit(Projectile projectile, NPC target, int damage, int sequence)
    {
        Item item = Main.player[projectile.owner].HeldItem;
        if (!DefenderRules.IsPrimal(item) && item.ModItem?.Name is not ("AuroraAxe" or "AsbestosBlade" or "HoshigumaBlazingBlade")) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (projectile.owner != Main.myPlayer) return;
            var packet = ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.DefenderRouteHit);
            packet.Write(projectile.identity); packet.Write((short)target.whoAmI); packet.Write(damage); packet.Write(sequence); packet.Send();
        }
        else if (projectile.ModProjectile is DefenderHoldout held) held.ApplyRouteEffects(target, damage, sequence);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        int identity = reader.ReadInt32(), npc = reader.ReadInt16(), damage = reader.ReadInt32(), sequence = reader.ReadInt32();
        if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers || (uint)npc >= Main.maxNPCs) return;
        Player owner = Main.player[sender]; NPC target = Main.npc[npc];
        if (!owner.active || owner.dead || !DefenderRules.IsWeapon(owner.HeldItem) || !target.active || target.friendly || target.dontTakeDamage) return;
        foreach (var p in Main.ActiveProjectiles) {
            if (p.owner != sender || p.identity != identity || p.ModProjectile is not DefenderHoldout held || (int)p.ai[0] != owner.HeldItem.type) continue;
            if (held.CanDamage() == false) return;
            float reach = DefenderRules.WeaponLength(owner.HeldItem) + 64;
            if (Vector2.DistanceSquared(owner.Center, target.Hitbox.ClosestPointInRect(owner.Center)) > reach * reach
                || !Collision.CanHitLine(owner.Center, 1, 1, target.position, target.width, target.height)) return;
            held.ApplyRouteEffects(target, damage, sequence); return;
        }
    }
}
