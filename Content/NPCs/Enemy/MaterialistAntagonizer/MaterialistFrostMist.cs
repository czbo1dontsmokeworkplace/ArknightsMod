using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// The damaging volume belongs to one living, stationary Frost drone in one encounter phase.
public sealed class MaterialistFrostMist : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)(FrostDrone.MistRadius * 2);
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = MaterialistWing.FrostSprayTicks + 1;
        Projectile.netImportant = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    internal NPC Source
    {
        get
        {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs) return null;
            NPC npc = Main.npc[index];
            if (!npc.active || npc.life <= 0 || npc.ModNPC is not FrostDrone ||
                !npc.TryGetGlobalNPC<MaterialistWing>(out var wing) || !wing.Bound || wing.Retreating ||
                wing.Encounter != (int)Projectile.ai[1] || !wing.FrostEmitting) return null;
            var boss = MaterialistAntagonizer.Owner(wing.OwnerIndex, wing.Encounter);
            return boss != null && boss.Phase == (int)Projectile.ai[2] && boss.EscortsReleased && !boss.Paused &&
                boss.NPC.HasValidTarget && wing.Age >= MaterialistRules.EscortGrace ? npc : null;
        }
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Source is NPC npc && npc.GetGlobalNPC<MaterialistWing>().FrostDamaging ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Source is NPC npc && FrostDrone.MistTouches(((FrostDrone)npc.ModNPC).MistCenter, targetHitbox) &&
        Collision.CanHitLine(((FrostDrone)npc.ModNPC).MistCenter, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);

    public override void AI()
    {
        if (Source is not NPC npc)
        {
            if (MaterialistAntagonizer.Authority) Projectile.Kill();
            return;
        }
        Projectile.Center = ((FrostDrone)npc.ModNPC).MistCenter;
    }

    public override bool PreDraw(ref Color lightColor) => false;
}
