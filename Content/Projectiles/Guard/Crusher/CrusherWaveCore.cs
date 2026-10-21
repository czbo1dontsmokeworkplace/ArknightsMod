using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherWaveCore : ModProjectile
{
    private int parentIndex = -1, parentIdentity = -1, age, searchTicks, samples;
    private readonly Vector2[] history = new Vector2[18];
    internal CrusherKind Kind => (CrusherKind)Math.Clamp((int)Projectile.ai[0], 0, 3);
    internal void LinkToWave(Projectile wave) { parentIndex = wave.whoAmI; parentIdentity = wave.identity; }
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 350;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true; Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Melee; Projectile.ignoreWater = true; Projectile.tileCollide = false;
        Projectile.timeLeft = 100; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.extraUpdates = 1;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => CrusherWaveVisuals.Emission(Projectile.Center, Projectile.velocity, Kind);
    public override void AI()
    {
        age++;
        // The brief backwards peel is deliberate; guidance takes over after the release flash.
        NPC target = ResolveTarget();
        if (age <= 6) Projectile.velocity = Projectile.velocity.RotatedBy(.045f * Projectile.ai[2]) * .96f;
        else if (target != null) {
            Vector2 delta = target.Center - Projectile.Center;
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 32, .12f);
            float leadTime = Math.Min(6, delta.Length() / (Math.Max(1, speed) * Projectile.MaxUpdates) * .35f);
            Vector2 lead = target.velocity * leadTime;
            if (lead.LengthSquared() > 48 * 48) lead = lead.SafeNormalize(Vector2.Zero) * 48;
            Vector2 desired = (delta + lead).SafeNormalize(Vector2.UnitX);
            float turn = MathHelper.Lerp(.15f, .36f, Math.Clamp((age - 6) / 12f, 0, 1));
            float heading = Projectile.velocity.SafeNormalize(desired).ToRotation().AngleTowards(desired.ToRotation(), turn);
            Projectile.velocity = heading.ToRotationVector2() * speed;
        }
        else Projectile.velocity *= .985f;
        Projectile.rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        Projectile.Opacity = Math.Clamp(age / 3f, 0, 1) * Math.Clamp(Projectile.timeLeft / 12f, 0, 1);
        for (int i = Math.Min(samples, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center; samples = Math.Min(samples + 1, history.Length);
        if (!Main.dedServ && CrusherVisuals.Visible(Projectile.Center)) {
            Lighting.AddLight(Projectile.Center, CrusherRules.Tint(Kind).ToVector3() * .35f * Projectile.Opacity);
            if (Projectile.numUpdates == 0 && age % 4 == 0) CrusherWaveVisuals.CoreMote(Projectile, Kind);
        }
    }
    private NPC ResolveTarget()
    {
        bool Valid(NPC npc) => npc.active && npc.CanBeChasedBy(Projectile) &&
            Vector2.DistanceSquared(npc.Hitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) <= 1600 * 1600;
        int currentIndex = (int)Projectile.ai[1] - 1;
        NPC current = (uint)currentIndex < Main.maxNPCs && Valid(Main.npc[currentIndex]) ? Main.npc[currentIndex] : null;
        if (Projectile.owner != Main.myPlayer) return current;
        if (current == null && currentIndex >= 0) searchTicks = 0;
        if (searchTicks-- > 0) return current;
        searchTicks = 5;
        CrusherWave parent = null;
        if ((uint)parentIndex < Main.maxProjectiles) {
            Projectile wave = Main.projectile[parentIndex];
            if (wave.active && wave.owner == Projectile.owner && wave.identity == parentIdentity) parent = wave.ModProjectile as CrusherWave;
        }
        NPC selected = current; int bestPriority = -1; float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (!Valid(npc)) continue;
            int priority = (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type] ? 2 : 0) + (parent?.HasHit(npc.whoAmI) == true ? 1 : 0);
            float distance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
            // A small preference retains the current target among equivalent candidates.
            if (npc == current) distance *= .8f;
            if (priority < bestPriority || priority == bestPriority && distance >= bestDistance) continue;
            selected = npc; bestPriority = priority; bestDistance = distance;
        }
        int index = selected == null ? 0 : selected.whoAmI + 1;
        if (Projectile.ai[1] != index) { Projectile.ai[1] = index; Projectile.netUpdate = true; }
        return selected;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => CrusherWaveVisuals.Impact(Projectile.Center, Kind, .4f);
    public override void OnKill(int timeLeft) => CrusherWaveVisuals.Impact(Projectile.Center, Kind, .28f);
    public override bool PreDraw(ref Color lightColor)
    {
        CrusherWaveMaterial.DrawCore(Projectile, history, samples, Kind); return false;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(parentIndex); writer.Write(parentIdentity); writer.Write((byte)Math.Min(age, 255)); }
    public override void ReceiveExtraAI(BinaryReader reader) { parentIndex = reader.ReadInt32(); parentIdentity = reader.ReadInt32(); age = reader.ReadByte(); }
}
