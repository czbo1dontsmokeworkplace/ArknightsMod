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
public sealed class MaterialistWing : GlobalNPC
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
        if (boss == null || boss.Order == FlightOrder.Death || Age > MaterialistRules.EscortLifetime) Retreating = true;
        if (Retreating)
        {
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(Slot % 2 == 0 ? -7 : 7, -12), .06f);
            npc.alpha = Math.Min(255, ++retreatTime * 6);
            if (retreatTime >= 45) MaterialistAntagonizer.Remove(npc);
            return false;
        }
        npc.dontTakeDamage = Age < MaterialistRules.EscortGrace;
        if (!boss.NPC.HasValidTarget) return false;
        Player target = Main.player[boss.NPC.target];
        npc.target = target.whoAmI;
        WingRole role = Role(npc);
        int beat = boss.Timer % 96;
        bool ownBeat = boss.Timer / 96 % MaterialistRules.WingCap(boss.Phase) == Slot;
        bool firing = boss.EscortFire && ownBeat && Age >= MaterialistRules.EscortGrace &&
            (boss.Order != FlightOrder.Bombard || role is WingRole.Drone or WingRole.MkII);
        float side = Slot % 2 == 0 ? -1 : 1;
        Vector2 station = target.Center + new Vector2(side * (250 + Slot / 2 * 125), -140 - Slot % 3 * 75);
        if (role == WingRole.Raptor) station = target.Center + new Vector2(-460, -170);
        if (boss.Paused) station = boss.NPC.Center + new Vector2(side * (160 + Slot * 28), 80);
        Vector2 delta = station - npc.Center;
        Vector2 motion = delta.SafeNormalize(Vector2.UnitY) * Math.Min(10, delta.Length() * .035f);
        npc.velocity = Vector2.Lerp(npc.velocity, firing ? Vector2.Zero : motion, .10f);
        npc.direction = npc.spriteDirection = target.Center.X < npc.Center.X ? -1 : 1;
        npc.rotation = MathHelper.Lerp(npc.rotation, npc.velocity.X * .012f, .1f);
        if (firing)
        {
            if (beat <= 38) aimAngle = (target.Center - Muzzle(npc)).ToRotation();
            if (MaterialistAntagonizer.Authority)
            {
                if (role == WingRole.ArtsA2 && beat == 1)
                    boss.Shoot(DroneShot.Beam, Muzzle(npc), (target.Center - Muzzle(npc)).SafeNormalize(Vector2.UnitY),
                        MaterialistRules.LaserWarning, 32, npc.whoAmI, side * .20f);
                else if (role == WingRole.Raptor && beat >= 48 && beat <= 88 && beat % 8 == 0)
                    boss.Shoot(DroneShot.Bolt, Muzzle(npc), (aimAngle + MathHelper.Lerp(-.42f, .42f, (beat - 48) / 40f)).ToRotationVector2() * 10, shooter: npc.whoAmI);
                else if (role == WingRole.ArtsA1 && beat == 52)
                    boss.Shoot(DroneShot.Bolt, Muzzle(npc), aimAngle.ToRotationVector2() * 17, shooter: npc.whoAmI);
                else if (role == WingRole.MkII && beat is 48 or 62 or 76)
                    boss.Shoot(DroneShot.Bolt, Muzzle(npc), aimAngle.ToRotationVector2() * 11, shooter: npc.whoAmI);
                else if (role == WingRole.Drone && beat == 54)
                    boss.Shoot(DroneShot.Missile, Muzzle(npc), aimAngle.ToRotationVector2() * 7, shooter: npc.whoAmI);
            }
        }
        if (MaterialistAntagonizer.Authority && (Age % 90 == 0 || firing && beat == 38)) npc.netUpdate = true;
        if (!Main.dedServ && !boundVisual)
        {
            boundVisual = true;
            MaterialistVisuals.Burst(npc.Center, MaterialistVisuals.Cyan, .65f);
            MaterialistEffects.Pulse(npc.Center, 100, MaterialistVisuals.Cyan);
        }
        return false;
    }
    public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !Bound;
    public override bool CheckActive(NPC npc) => !Bound;
    public override bool PreKill(NPC npc)
    {
        if (!Bound) return true;
        if (!defeatReported)
        {
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
        float alpha = (1 - npc.alpha / 255f) * MathHelper.Clamp(Age / 35f, 0, 1);
        var role = Role(npc);
        Texture2D texture;
        Rectangle frame;
        if (role == WingRole.Raptor)
        {
            texture = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/RaptorDrone/RaptorDrone_" + (1 + Age / 6 % 13)).Value;
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
        bool firing = boss != null && boss.EscortFire && boss.Timer / 96 % MaterialistRules.WingCap(boss.Phase) == Slot && Age >= MaterialistRules.EscortGrace;
        int beat = boss?.Timer % 96 ?? 0;
        Color accent = role is WingRole.ArtsA1 or WingRole.ArtsA2 ? MaterialistVisuals.Violet : MaterialistVisuals.Amber;
        MaterialistVisuals.Glow(npc.Center, accent * (.24f * alpha), new Vector2(frame.Width * 1.8f, frame.Height * 1.3f));
        batch.Draw(texture, npc.Center - screenPos, frame, Color.Lerp(drawColor, Color.White, .45f) * alpha,
            npc.rotation, frame.Size() * .5f, npc.scale, npc.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        if (Age < MaterialistRules.EscortGrace) MaterialistVisuals.Ring(npc.Center, 55 - Age * .3f, MaterialistVisuals.Cyan * alpha, Age * .04f, 2);
        if (firing && beat is >= 8 and < 48 && role != WingRole.ArtsA2)
        {
            Vector2 start = Muzzle(npc);
            MaterialistVisuals.Line(start, start + aimAngle.ToRotationVector2() * 650, accent * .5f, 1.5f);
            MaterialistVisuals.Glow(start, accent * .75f, new Vector2(16 + beat * .55f));
        }
        return false;
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter w)
    {
        w.Write(Encounter);
        if (!Bound) return;
        w.Write(OwnerIndex); w.Write(Slot); w.Write(Age); w.Write(ScaledLife); w.Write(Retreating); w.Write(aimAngle);
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader r)
    {
        Encounter = r.ReadInt32();
        if (!Bound) return;
        OwnerIndex = r.ReadInt32(); Slot = r.ReadInt32(); Age = r.ReadInt32(); ScaledLife = r.ReadInt32(); Retreating = r.ReadBoolean(); aimAngle = r.ReadSingle();
        npc.lifeMax = ScaledLife;
    }
}
