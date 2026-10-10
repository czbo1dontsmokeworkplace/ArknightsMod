using System;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4.RaptorDrone;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    private void RunOrder(Player player)
    {
        float side = Cycle % 2 == 0 ? -1 : 1;
        switch (Order)
        {
            case FlightOrder.Arrival:
            case FlightOrder.Transition:
                NPC.velocity *= .90f;
                if (Timer >= MaterialistRules.Duration(Order)) Enter(FlightOrder.Deploy);
                return;
            case FlightOrder.Deploy:
                MoveTo(player.Center + new Vector2(0, -300));
                if (Authority && Timer == 1) Wave++;
                if (Authority && Timer >= 60 && Timer <= 156 && (Timer - 60) % 24 == 0)
                    DeploySlot((Timer - 60) / 24);
                if (Authority && Timer == 170 && Phase >= 2 && WingCount() > 0)
                {
                    ShieldMax = (int)(NPC.lifeMax * (Phase == 2 ? .045f : .060f));
                    Shield = ShieldMax; NPC.netUpdate = true;
                }
                break;
            case FlightOrder.Command:
                // The commander stays reachable while escorts execute staggered attack slots.
                MoveTo(player.Center + new Vector2(MathF.Sin(Timer * .010f) * 220, -240), 10);
                if (Authority && Timer > 120 && WingCount() == 0) { Enter(FlightOrder.Recover); return; }
                break;
            case FlightOrder.Bombard:
                MoveTo(player.Center + new Vector2(side * 320, -270));
                if (Authority && Timer is 45 or 115 or 185)
                {
                    // Fixed world-space marker: moving after the lock always escapes the strike.
                    Vector2 point = player.Center + (Timer == 115 ? Vector2.Clamp(player.velocity * 22, new Vector2(-160), new Vector2(160)) : Vector2.Zero);
                    Shoot(DroneShot.Bombardment, point, Vector2.Zero, MaterialistRules.BombWarning, 18);
                }
                break;
            case FlightOrder.Sweep:
                if (Timer < 40) MoveTo(player.Center + new Vector2(side * 420, -230));
                else NPC.velocity *= .88f;
                if (Authority && Timer == 48)
                {
                    Vector2 muzzle = NPC.Center + new Vector2(0, 48);
                    Aim = player.Center; NPC.netUpdate = true;
                    Shoot(DroneShot.Beam, muzzle, (Aim - muzzle).SafeNormalize(Vector2.UnitY), 72, 70, sweep: side * .50f);
                }
                break;
            case FlightOrder.Ram:
                if (Timer < 24) MoveTo(player.Center + new Vector2(side * 470, -110), 16);
                else if (Timer < 84)
                {
                    NPC.velocity *= .85f;
                    if (Authority && Timer == 28)
                    {
                        Aim = player.Center + Vector2.Clamp(player.velocity * 12, new Vector2(-100), new Vector2(100));
                        DashDirection = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.netUpdate = true;
                    }
                }
                else if (Timer < 114) NPC.velocity = DashDirection * 25;
                else NPC.velocity *= .92f;
                break;
            case FlightOrder.Overload:
                if (Timer < 35) MoveTo(player.Center + new Vector2(0, -240));
                else NPC.velocity *= .90f;
                if (Authority && Timer == 40) { Aim = player.Center; NPC.netUpdate = true; }
                if (Authority && Timer is 112 or 140 or 168)
                {
                    float gap = (Aim - NPC.Center).ToRotation();
                    for (int i = 0; i < 20; i++)
                    {
                        float angle = i * MathHelper.TwoPi / 20 + (Timer == 140 ? .08f : -.08f);
                        if (Math.Abs(MathHelper.WrapAngle(angle - gap)) < .56f) continue;
                        Shoot(DroneShot.Bolt, NPC.Center, angle.ToRotationVector2() * (Timer == 168 ? 9 : 7));
                    }
                }
                break;
            case FlightOrder.Recover:
                MoveTo(player.Center + new Vector2(side * 100, -120), 6);
                break;
        }
        if (Timer >= MaterialistRules.Duration(Order)) NextOrder();
    }

    internal int WingCount()
    {
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && !wing.Retreating) count++;
        return count;
    }
    private void DeploySlot(int slot)
    {
        int cap = MaterialistRules.WingCap(Phase);
        if (slot >= cap || WingCount() >= cap) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && wing.Slot == slot && !wing.Retreating) return;
        int type = Phase switch {
            1 => slot == 2 ? ModContent.NPCType<DroneII>() : ModContent.NPCType<Drone>(),
            2 => slot switch { 1 => ModContent.NPCType<ArtsMasterA1>(), 2 => ModContent.NPCType<ArtsMasterA2>(), _ => ModContent.NPCType<DroneII>() },
            _ => slot switch { 0 => raptorDeployed ? ModContent.NPCType<DroneII>() : ModContent.NPCType<RaptorDrone>(),
                1 => ModContent.NPCType<ArtsMasterA1>(), 3 => ModContent.NPCType<ArtsMasterA2>(), _ => ModContent.NPCType<DroneII>() }
        };
        Vector2 origin = NPC.Center + new Vector2(slot % 2 == 0 ? -120 : 120, 25);
        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, type);
        if (index < 0 || index >= Main.maxNPCs) return;
        NPC escort = Main.npc[index];
        var data = escort.GetGlobalNPC<MaterialistWing>();
        data.Bind(escort, this, slot);
        escort.Center = origin;
        escort.netUpdate = true;
        if (type == ModContent.NPCType<RaptorDrone>()) raptorDeployed = true;
        NPC.netUpdate = true;
    }
    internal void Shoot(DroneShot kind, Vector2 origin, Vector2 velocity, int delay = 0, int duration = 240, int shooter = -1, float sweep = 0)
    {
        if (!Authority) return;
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(this)) count++;
        if (count >= MaterialistRules.HazardCap) return;
        int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity,
            ModContent.ProjectileType<MaterialistHazard>(), MaterialistRules.Damage(kind), 0, Main.myPlayer,
            NPC.whoAmI + 1, Encounter, (int)kind);
        if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is MaterialistHazard hazard)
        {
            hazard.Delay = delay; hazard.Duration = duration; hazard.Target = NPC.target;
            hazard.Shooter = shooter; hazard.Sweep = sweep;
            hazard.Projectile.netUpdate = true;
        }
    }
    private void ClearHazards()
    {
        if (!Authority) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(this)) { p.hostile = false; p.Kill(); }
    }
    private void RecallWing()
    {
        if (!Authority) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this))
            { wing.Retreating = true; n.dontTakeDamage = true; n.damage = 0; n.netUpdate = true; }
    }
}
