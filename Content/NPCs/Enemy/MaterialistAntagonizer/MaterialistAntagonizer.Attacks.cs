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
                NPC.velocity *= .90f;
                if (Timer >= MaterialistRules.Duration(Order)) Enter(FlightOrder.Deploy);
                return;
            case FlightOrder.Transition:
                MoveTo(FormationCenter + new Vector2(0, -290), 10);
                if (Timer >= MaterialistRules.Duration(Order)) Enter(FlightOrder.Deploy);
                return;
            case FlightOrder.Deploy:
                MoveTo(player.Center + new Vector2(0, -300));
                if (Authority && Timer == 1) Wave++;
                if (Authority && Timer >= 30 && Timer <= 210 && (Timer - 30) % 20 == 0)
                    DeploySlot((Timer - 30) / 20);
                // If the NPC pool temporarily filled, finish the missing slots before releasing the wing.
                if (Authority && Timer > 210 && Timer % 20 == 10)
                    for (int slot = 0; slot < MaterialistRules.WingCap(Phase); slot++) DeploySlot(slot);
                if (Authority && Timer == 280 && Phase >= 2 && WingCount() > 0)
                {
                    ShieldMax = (int)(NPC.lifeMax * (Phase == 2 ? .045f : .060f));
                    Shield = ShieldMax; NPC.netUpdate = true;
                }
                break;
            case FlightOrder.Command:
                RunFormationBody();
                if (Authority && Timer > 120 && WingCount() == 0) { NPC.ai[3] = -1; Enter(FlightOrder.Recover); return; }
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
                if (Timer < 114) RunCoreBarrage();
                if (Timer < 110) MoveTo(FormationCenter + new Vector2(side * 470, -110), 16);
                else if (Timer < 170)
                {
                    NPC.velocity *= .85f;
                    if (Authority && Timer == 114)
                    {
                        ClearHazards();
                        Aim = player.Center + Vector2.Clamp(player.velocity * 12, new Vector2(-100), new Vector2(100));
                        DashDirection = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.netUpdate = true;
                    }
                }
                else if (Timer < 200) NPC.velocity = DashDirection * 25;
                else NPC.velocity *= .92f;
                break;
            case FlightOrder.Overload:
                if (Timer < 35) MoveTo(player.Center + new Vector2(0, -240));
                else NPC.velocity *= .90f;
                if (Authority && Timer == 40) { Aim = player.Center; NPC.netUpdate = true; }
                if (Authority && Timer is 112 or 140 or 168)
                {
                    float gap = (Aim - NPC.Center).ToRotation();
                    for (int i = 0; i < MaterialistRules.SpreadRingCount(20); i++)
                    {
                        float angle = i * MathHelper.TwoPi / MaterialistRules.SpreadRingCount(20) + (Timer == 140 ? .08f : -.08f);
                        if (Math.Abs(MathHelper.WrapAngle(angle - gap)) < .56f) continue;
                        Shoot(DroneShot.Bolt, NPC.Center, angle.ToRotationVector2() * (Timer == 168 ? 9 : 7));
                    }
                }
                break;
            case FlightOrder.Recover:
                MoveTo(player.Center + new Vector2(side * 100, -120), 6);
                break;
        }
        if (Timer >= OrderDuration && (Order != FlightOrder.Deploy || DeploymentReady())) NextOrder();
    }

    private void RunFormationBody()
    {
        if (Authority && Timer is 60 or 240)
        {
            ClearHazards(preservePincer: true);
            if (Phase == 1 && Timer == 240) MaterialistScoutPlanner.Roll(this);
            NPC.netUpdate = true;
        }
        MoveTo(FormationCenter + ArtilleryBodyOffset(), 9);
        RunCoreBarrage();
        if (!Authority) return;
        int beat = MaterialistTactics.Beat(Timer);
        // Strike the rear of the shared corridor; the exit stays available while escorts converge.
        if (!CoreSuppressed && !CorePincerRound && !CoreSaucerRound && !PincerInProgress && !ArtilleryActive && MaterialistTactics.Active(Timer) && beat == 36 && Pattern is WingPattern.ArcPincer or WingPattern.Gate or WingPattern.HeavyHunt)
            Shoot(DroneShot.Bombardment, FormationCenter - ExitAngle.ToRotationVector2() * 190, Vector2.Zero, 72, 18);
    }

    internal int WingCount()
    {
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && !wing.Retreating) count++;
        return count;
    }
    private bool DeploymentReady()
    {
        int ready = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && !wing.Retreating)
            {
                if (wing.Age < MaterialistRules.EscortGrace) return false;
                ready++;
            }
        return ready >= MaterialistRules.WingCap(Phase);
    }
    private void DeploySlot(int slot)
    {
        int cap = MaterialistRules.WingCap(Phase);
        if (slot >= cap || WingCount() >= cap) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this) && wing.Slot == slot && !wing.Retreating) return;
        WingDuty duty = MaterialistRules.Duty(Phase, slot);
        int type = duty switch {
            WingDuty.Containment => Phase == 1 ? ModContent.NPCType<Drone>() : ModContent.NPCType<ArtsMasterA2>(),
            WingDuty.Precision => Phase == 1 ? ModContent.NPCType<DroneII>() : ModContent.NPCType<ArtsMasterA1>(),
            WingDuty.Scatter => ModContent.NPCType<DroneII>(),
            WingDuty.Heavy => ModContent.NPCType<RaptorDrone>(),
            _ => ModContent.NPCType<Drone>()
        };
        Vector2 origin = NPC.Center + new Vector2(slot % 2 == 0 ? -120 : 120, 25);
        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, type);
        if (index < 0 || index >= Main.maxNPCs) return;
        NPC escort = Main.npc[index];
        var data = escort.GetGlobalNPC<MaterialistWing>();
        data.Bind(escort, this, slot);
        escort.Center = origin;
        escort.netUpdate = true;
        NPC.netUpdate = true;
    }
    internal void Shoot(DroneShot kind, Vector2 origin, Vector2 velocity, int delay = 0, int duration = 240, int shooter = -1, float sweep = 0,
        float length = MaterialistRules.BeamLength, FireStyle style = FireStyle.Standard, float bend = 0, Vector2? mark = null)
    {
        if (!Authority) return;
        bool verticalSlide = false, denseBallistics = false;
        if (shooter >= 0)
        {
            if (shooter >= Main.maxNPCs || !Main.npc[shooter].active ||
                !Main.npc[shooter].TryGetGlobalNPC<MaterialistWing>(out var wing) || !wing.BelongsTo(this) || wing.Retreating ||
                !wing.PrepareShot(Main.npc[shooter], this, ref kind, ref velocity, ref delay, ref duration, ref bend)) return;
            if (Phase == 3)
            {
                WingRole role = MaterialistWing.Role(Main.npc[shooter]);
                verticalSlide = role == WingRole.ArtsA2 && kind is DroneShot.Beam or DroneShot.ChaseBeam;
                denseBallistics = role == WingRole.Raptor && kind == DroneShot.MachineRound;
                if (verticalSlide) { kind = DroneShot.Beam; velocity = Vector2.UnitY; sweep = 0; }
            }
        }
        else if (kind is DroneShot.Bolt or DroneShot.Prism or DroneShot.Seeker or DroneShot.ArcSpark or DroneShot.Petal or
            DroneShot.Lattice or DroneShot.CoreLance or DroneShot.ClusterOrb or DroneShot.OrbitSpark or DroneShot.Needle)
        {
            float speed = MathHelper.Clamp(velocity.Length() * .65f, 3, style == FireStyle.CoreFocus ? 6.2f : 5.2f);
            velocity = velocity.SafeNormalize(Vector2.UnitY) * speed;
            bend = MathHelper.Clamp(bend, -.006f, .006f);
        }
        int count = 0, ballisticCount = 0;
        int scoutCount = 0, seekerCount = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(this) && !h.Retiring)
            {
                count++;
                if (h.Kind == DroneShot.MachineRound) ballisticCount++;
                if (h.Kind == DroneShot.ScoutPulse) scoutCount++;
                if (h.Kind is DroneShot.ArtsOrb or DroneShot.Seeker) seekerCount++;
            }
        if (count >= MaterialistRules.HazardCap) return;
        if (kind == DroneShot.ScoutPulse && scoutCount >= 36 || kind is DroneShot.ArtsOrb or DroneShot.Seeker && seekerCount >= 2) return;
        // Keep space for the two sustained gunships during dense core/escort combinations.
        if (Phase == 3 && kind != DroneShot.MachineRound && count - ballisticCount >= MaterialistRules.HazardCap - 72) return;
        int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity,
            ModContent.ProjectileType<MaterialistHazard>(), style == FireStyle.Containment ? MaterialistRules.ContainmentDamage : MaterialistRules.Damage(kind), 0, Main.myPlayer,
            NPC.whoAmI + 1, Encounter, (int)kind);
        if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is MaterialistHazard hazard)
        {
            hazard.Delay = delay; hazard.Duration = duration; hazard.Target = NPC.target;
            hazard.Shooter = shooter; hazard.Sweep = sweep;
            hazard.Length = length; hazard.Style = style;
            hazard.Acceleration = 1;
            hazard.Bend = bend; hazard.Anchor = mark ?? origin;
            hazard.VerticalSlide = verticalSlide; hazard.DenseBallistics = denseBallistics;
            hazard.LaunchVelocity = velocity;
            if (kind == DroneShot.ArtsOrb)
            {
                hazard.Projectile.width = hazard.Projectile.height = 26;
                hazard.Projectile.Center = origin;
            }
            if (kind == DroneShot.SaucerRound)
            {
                hazard.Projectile.width = hazard.Projectile.height = 8;
                hazard.Projectile.Center = origin;
                hazard.Projectile.penetrate = 1;
                hazard.Projectile.tileCollide = true;
            }
            if (kind == DroneShot.MachineRound)
            {
                hazard.Projectile.width = hazard.Projectile.height = 6;
                hazard.Projectile.Center = origin;
                hazard.Projectile.penetrate = 1;
                hazard.Projectile.tileCollide = true;
                hazard.Projectile.extraUpdates = MaterialistRules.MachineUpdates - 1;
                hazard.Projectile.timeLeft = (duration + MaterialistHazard.RetirementTicks + 8) * MaterialistRules.MachineUpdates;
            }
            if (kind is DroneShot.GravityOrb or DroneShot.DropMissile)
            {
                hazard.Projectile.tileCollide = true;
                hazard.Projectile.penetrate = 1;
            }
            hazard.Projectile.netUpdate = true;
        }
    }
    private void ClearHazards(bool preservePincer = false)
    {
        if (!Authority) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(this) &&
                (!preservePincer || h.Kind != DroneShot.PincerBeam)) h.Retire();
    }
    private void RecallWing()
    {
        if (!Authority) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(this))
            { wing.Retreating = true; n.dontTakeDamage = true; n.damage = 0; n.netUpdate = true; }
    }
}
