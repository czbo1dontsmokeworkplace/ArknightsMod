using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// Player-owned projectile hit callbacks run on the owning client. Only the server
// may pull/stun NPCs or maintain a damage-over-time mark, so forward this bounded event.
internal static class CrusherCombatNet
{
    internal static void Contact(Player player, NPC target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (player.whoAmI != Main.myPlayer) return;
            ModPacket packet = ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.CrusherHit);
            packet.Write(-1); packet.Write((short)target.whoAmI); packet.Send();
        }
        else player.GetModPlayer<CrusherPlayer>().MarkAttacker(target);
    }
    internal static void Hit(Projectile projectile, NPC target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (projectile.owner != Main.myPlayer) return;
            ModPacket packet = ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.CrusherHit);
            packet.Write(projectile.identity); packet.Write((short)target.whoAmI); packet.Send();
        }
        else Apply(projectile, target);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        int identity = reader.ReadInt32(), npc = reader.ReadInt16();
        if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers || (uint)npc >= Main.maxNPCs
            || !Main.player[sender].active || Main.player[sender].dead || Main.player[sender].HeldItem.ModItem is not CrusherWeapon) return;
        NPC target = Main.npc[npc];
        if (!target.active || target.friendly || target.dontTakeDamage) return;
        if (identity == -1) {
            Player owner = Main.player[sender];
            if (target.damage > 0 && Vector2.DistanceSquared(target.Hitbox.ClosestPointInRect(owner.Center), owner.Center) <= 100 * 100)
                owner.GetModPlayer<CrusherPlayer>().MarkAttacker(target);
            return;
        }
        foreach (Projectile p in Main.ActiveProjectiles) {
            if (p.owner != sender || p.identity != identity) continue;
            float range = p.ModProjectile is CrusherHeldBlade ? 650 : p.ModProjectile is CrusherAnchorCrash ? 190 : 120;
            if (Vector2.DistanceSquared(target.Hitbox.ClosestPointInRect(p.Center), p.Center) > range * range) return;
            Apply(p, target); return;
        }
    }
    private static void Apply(Projectile projectile, NPC target)
    {
        switch (projectile.ModProjectile) {
            case CrusherHeldBlade blade: blade.ApplyHitEffects(target); break;
            case UlpianusAnchorThrow thrown: thrown.ApplyHitEffects(target); break;
            case CrusherAnchorCast anchor: anchor.ApplyPull(target); break;
            case CrusherAnchorCrash crash: crash.ApplyStun(target); break;
            case CrusherUpheaval upheaval: upheaval.ApplyPull(target); break;
        }
    }
}
