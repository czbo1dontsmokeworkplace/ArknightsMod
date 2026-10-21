using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4.RaptorDrone;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// Only instances explicitly bound by the commander receive formation AI. Wild NPCs keep their own AI, loot and art.
public sealed partial class MaterialistWing : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is Drone or DroneII or ArtsMasterDrone or RaptorDrone;
    internal int OwnerIndex = -1, Encounter, Slot, Age, ScaledLife;
    internal bool Retreating;
    private int retreatTime;
    private float aimAngle;
    private bool defeatReported;
    private bool boundVisual;
    internal bool Bound => Encounter > 0;
    internal bool BelongsTo(MaterialistAntagonizer boss) => Bound && OwnerIndex == boss.NPC.whoAmI && Encounter == boss.Encounter;
    internal static WingRole Role(NPC npc) => npc.ModNPC switch {
        RaptorDrone => WingRole.Raptor, ArtsMasterA2 => WingRole.ArtsA2, ArtsMasterA1 => WingRole.ArtsA1,
        DroneII => WingRole.MkII, _ => WingRole.Drone
    };
    internal static Vector2 Muzzle(NPC npc) => npc.ModNPC is ArtsMasterDrone arts ? arts.Muzzle :
        npc.Center + new Vector2((npc.ModNPC is RaptorDrone ? 27 : 12) * npc.spriteDirection, 12).RotatedBy(npc.rotation);
    internal static (Texture2D Texture, Rectangle Frame) Sprite(NPC npc, int age)
    {
        if (Role(npc) == WingRole.Raptor)
        {
            var texture = MaterialistVisuals.Asset("ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/RaptorDrone/RaptorDrone_" + (1 + age / 6 % 5));
            return (texture, texture.Bounds);
        }
        var body = TextureAssets.Npc[npc.type].Value;
        return (body, body.Frame(1, npc.ModNPC is ArtsMasterDrone ? 8 : 2, 0, age / 6 % (npc.ModNPC is ArtsMasterDrone ? 8 : 2)));
    }
    internal void Bind(NPC npc, MaterialistAntagonizer boss, int slot)
    {
        OwnerIndex = boss.NPC.whoAmI; Encounter = boss.Encounter; Slot = slot;
        ScaledLife = (int)(MaterialistRules.WingLife(Role(npc)) * MathF.Sqrt(boss.NPC.lifeMax / (float)MaterialistRules.Life));
        npc.life = npc.lifeMax = ScaledLife;
        npc.damage = 0; npc.defense = 22; npc.value = 0;
        npc.knockBackResist = 0; npc.noGravity = npc.noTileCollide = true;
        npc.dontTakeDamage = true; npc.netAlways = true;
        npc.ai[0] = npc.ai[1] = npc.ai[2] = npc.ai[3] = 0;
    }
    public override bool PreAI(NPC npc)
    {
        if (!Bound) return true;
        npc.damage = 0; npc.noGravity = npc.noTileCollide = true;
        npc.value = 0; npc.defense = 22; npc.knockBackResist = 0;
        npc.lifeMax = ScaledLife; npc.timeLeft = 1800;
        Age++;
        var boss = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
        if (boss == null && !MaterialistAntagonizer.Authority)
        {
            // A late join can receive escorts before their commander. Do not locally commit to a retreat.
            npc.dontTakeDamage = true;
            npc.velocity *= .9f;
            return false;
        }
        if (boss == null || boss.Order == FlightOrder.Death ||
            !boss.FinalStand && Role(npc) != WingRole.Raptor && Age > MaterialistRules.EscortLifetime) Retreating = true;
        if (Retreating)
        {
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(Slot % 2 == 0 ? -7 : 7, -12), .06f);
            npc.alpha = Math.Min(255, ++retreatTime * 6);
            if (retreatTime >= 45) MaterialistAntagonizer.Remove(npc);
            return false;
        }
        npc.dontTakeDamage = Age < MaterialistRules.EscortGrace || !boss.EscortsReleased || boss.Order == FlightOrder.Deploy;
        if (!boss.NPC.HasValidTarget) return false;
        Player target = Main.player[boss.NPC.target];
        npc.target = target.whoAmI;
        FlyFormation(npc, boss);
        if (MaterialistAntagonizer.Authority && Age % 45 == 0) npc.netUpdate = true;
        if (!Main.dedServ && !boundVisual)
        {
            boundVisual = true;
            MaterialistVisuals.Burst(npc.Center, MaterialistVisuals.Cyan, .65f);
        }
        return false;
    }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (!Bound || !npc.dontTakeDamage) return;
        npc.lifeRegen = Math.Max(0, npc.lifeRegen);
        npc.lifeRegenCount = Math.Max(0, npc.lifeRegenCount);
        damage = 0;
    }
    public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
    {
        if (!Bound) return true;
        cooldownSlot = ImmunityCooldownID.Bosses;
        var boss = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
        return Role(npc) != WingRole.Raptor && !Retreating && Age >= MaterialistRules.EscortGrace && boss != null && boss.EscortsReleased && !boss.ArtilleryActive && boss.EscortFire && boss.Order != FlightOrder.Deploy &&
            (boss.ScoutReady ? MaterialistScoutPlanner.Diving(boss.ScoutAttack(Slot), MaterialistScoutPlanner.LocalBeat(boss, Slot)) :
                MaterialistTactics.Diving(boss.Pattern, boss.Timer, Slot, MaterialistRules.WingCap(boss.Phase)));
    }
    public override bool CheckActive(NPC npc) => !Bound;
    public override bool PreKill(NPC npc)
    {
        if (!Bound) return true;
        if (!defeatReported)
        {
            if (!Retreating && npc.life <= 0 && Role(npc) == WingRole.Raptor && MaterialistAntagonizer.Authority)
                MaterialistRaptorDeathBomb.Spawn(npc);
            defeatReported = true; Retreating = true;
            MaterialistAntagonizer.Owner(OwnerIndex, Encounter)?.EscortDestroyed();
        }
        return false; // No ordinary loot, banner farming or native OnKill effects for summoned units.
    }
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Bound && npc.life <= 0 && !Main.dedServ)
        {
            MaterialistVisuals.Burst(npc.Center, MaterialistVisuals.Amber, Role(npc) == WingRole.Raptor ? 1.8f : .8f);
            MaterialistEffects.Pulse(npc.Center, 120, MaterialistVisuals.Amber);
        }
    }
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (!Bound) return true;
        float alpha = 1 - npc.alpha / 255f;
        var role = Role(npc);
        Texture2D texture;
        Rectangle frame;
        if (role == WingRole.Raptor)
        {
            var owner = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
            int animation = owner != null && Age >= MaterialistRules.EscortGrace && RaptorWindow(owner) ?
                RaptorShot(owner) ? 12 : 13 : 1 + Age / 6 % 5;
            texture = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/RaptorDrone/RaptorDrone_" + animation).Value;
            frame = texture.Bounds;
        }
        else
        {
            texture = TextureAssets.Npc[npc.type].Value;
            int count = role is WingRole.ArtsA1 or WingRole.ArtsA2 ? 8 : 2;
            frame = texture.Frame(1, count, 0, Age / 6 % count);
            npc.frame = frame;
        }
        var boss = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
        Color accent = boss != null ? DutyColor(MaterialistRules.Duty(boss.Phase, Slot)) : MaterialistVisuals.Cyan;
        if (role == WingRole.Raptor)
        {
            SpriteEffects flip = npc.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rimScale = npc.scale * MathHelper.Lerp(.3f, 1, MaterialistFormation.Smooth(Age / 38f));
            if (!MaterialistTechRenderer.ChassisMaterial(texture, npc.Center, frame, npc.rotation, new Vector2(rimScale * 1.025f),
                flip, accent * (.95f * alpha), 4))
                for (int i = 0; i < 8; i++)
                    batch.Draw(texture, npc.Center - screenPos + (i * MathHelper.TwoPi / 8).ToRotationVector2() * 2,
                        frame, (accent with { A = 0 }) * (.85f * alpha), npc.rotation, frame.Size() * .5f, rimScale, flip, 0);
        }
        else MaterialistVisuals.Glow(npc.Center, accent * (.24f * alpha), new Vector2(frame.Width * 1.8f, frame.Height * 1.3f));
        if (role != WingRole.Raptor && !MaterialistVisuals.Reduced && npc.velocity.LengthSquared() > 100)
            for (int i = 1; i <= 3; i++) batch.Draw(texture, npc.Center - npc.velocity * i * 1.4f - screenPos, frame,
                (accent with { A = 0 }) * ((1 - i / 4f) * .28f * alpha), npc.rotation, frame.Size() * .5f, npc.scale,
                npc.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        if (role == WingRole.Raptor || Age >= 42 || !MaterialistTechRenderer.HullActive)
            batch.Draw(texture, npc.Center - screenPos, frame, Color.Lerp(drawColor, Color.White, .45f) * alpha,
            npc.rotation, frame.Size() * .5f, npc.scale * MathHelper.Lerp(.3f, 1, MaterialistFormation.Smooth(Age / 38f)),
            npc.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        DrawFlightWarning(npc, boss, accent);
        return false;
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter w)
    {
        w.Write(Encounter);
        if (!Bound) return;
        w.Write(OwnerIndex); w.Write(Slot); w.Write(Age); w.Write(ScaledLife); w.Write(Retreating); w.Write(aimAngle);
        w.Write(planRevision); w.WriteVector2(planStart); w.WriteVector2(launchOrigin);
        if (Role(npc) == WingRole.Raptor) WriteRaptorAI(w);
        w.Write(scoutSeenRevision); w.WriteVector2(scoutAim);
        w.Write(flightRoute); w.Write(blendAge); w.WriteVector2(blendStart); w.WriteVector2(lastFlightGoal); w.WriteVector2(goalVelocity);
        w.Write(goalReady); w.Write(maneuverLocked); w.WriteVector2(maneuverCenter); w.Write(maneuverExit);
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader r)
    {
        Encounter = r.ReadInt32();
        if (!Bound) return;
        OwnerIndex = r.ReadInt32(); Slot = r.ReadInt32(); Age = r.ReadInt32(); ScaledLife = r.ReadInt32(); Retreating = r.ReadBoolean(); aimAngle = r.ReadSingle();
        planRevision = r.ReadInt32(); planStart = r.ReadVector2(); launchOrigin = r.ReadVector2();
        if (Role(npc) == WingRole.Raptor) ReadRaptorAI(r);
        scoutSeenRevision = r.ReadInt32(); scoutAim = r.ReadVector2();
        flightRoute = r.ReadInt32(); blendAge = r.ReadInt32(); blendStart = r.ReadVector2(); lastFlightGoal = r.ReadVector2(); goalVelocity = r.ReadVector2();
        goalReady = r.ReadBoolean(); maneuverLocked = r.ReadBoolean(); maneuverCenter = r.ReadVector2(); maneuverExit = r.ReadSingle();
        npc.lifeMax = ScaledLife;
    }
}
