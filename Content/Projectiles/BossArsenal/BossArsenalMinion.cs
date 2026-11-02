using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public abstract class BossArsenalMinionBuff : ModBuff
{
    protected abstract int Kind { get; }
    public override string Texture => "Terraria/Images/Buff_" + (Kind == 7 ? BuffID.Frostburn : Kind == 11 ? BuffID.Inferno : BuffID.BabySlime);
    internal static int ForKind(int kind) => kind switch {
        3 => ModContent.BuffType<UrsusArtilleryBuff>(), 7 => ModContent.BuffType<YetiChimeBuff>(),
        11 => ModContent.BuffType<BasaltBroodBuff>(), _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
    public override void SetStaticDefaults() { Main.buffNoTimeDisplay[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is BossArsenalMinion && (int)p.ai[0] == Kind) { player.buffTime[buffIndex] = 18000; return; }
        player.DelBuff(buffIndex--);
    }
}
public sealed class UrsusArtilleryBuff : BossArsenalMinionBuff { protected override int Kind => 3; }
public sealed class YetiChimeBuff : BossArsenalMinionBuff { protected override int Kind => 7; }
public sealed class BasaltBroodBuff : BossArsenalMinionBuff { protected override int Kind => 11; }

public sealed class BossArsenalMinion : ModProjectile
{
    private int Kind => (int)Projectile.ai[0];
    private int Clock => (int)Projectile.ai[1];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BabySlime;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 12; ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.MinionSacrificable[Type] = true; ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 28; Projectile.minion = true; Projectile.minionSlots = 1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.timeLeft = 2;
        Projectile.netImportant = true; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 24;
    }
    public override bool MinionContactDamage() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || !owner.HasBuff(BossArsenalMinionBuff.ForKind(Kind))) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (Kind == 7) {
            // Stable tie-break also removes duplicate crowns restored by old saves/network packets.
            foreach (Projectile other in Main.ActiveProjectiles)
                if (other.owner == Projectile.owner && other.ModProjectile is BossArsenalMinion && other.ai[0] == 7
                    && other.whoAmI < Projectile.whoAmI) { Projectile.Kill(); return; }
            RefreshCrown(Projectile, owner);
        }
        NPC target = null;
        if (owner.HasMinionAttackTargetNPC) {
            NPC manual = Main.npc[owner.MinionAttackTargetNPC];
            if (manual.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(owner.Center, manual.Center) < 1400 * 1400) target = manual;
        }
        if (target == null) foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(owner.Center, npc.Center) < 1000 * 1000
                && (target == null || Vector2.DistanceSquared(Projectile.Center, npc.Center) < Vector2.DistanceSquared(Projectile.Center, target.Center))) target = npc;
        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2000 * 2000) {
            Projectile.Center = owner.Center; Projectile.velocity = Vector2.Zero; Projectile.ai[1] = 0; Projectile.netUpdate = true;
        }
        Projectile.ai[1]++;
        Vector2 home = owner.MountedCenter + new Vector2((Projectile.minionPos % 5 - 2) * 38, -85 - Projectile.minionPos / 5 * 35);
        if (Kind == 7) {
            // Frost Blossom: player-relative perch, not an orbit around enemies.
            Projectile.Center = owner.MountedCenter + new Vector2(0, -85 + MathF.Sin(Clock / 40f) * 4);
            Projectile.velocity = Vector2.Zero; Projectile.scale = 1 + MathF.Sin(Clock / 40f) * .085f;
            int phase = Clock % 180;
            if (target != null && phase is > 0 and <= 48 && phase % 8 == 0 && Visible(target)) {
                for (int side = -1; side <= 1; side += 2) Fire(target, 7, new Vector2(side * 6, -4).RotatedBy(phase * .015f));
            }
            if (target != null && phase == 72 && Projectile.owner == Main.myPlayer && Visible(target)) {
                var ray = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center,
                    (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<YetiCrownBeam>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI, Projectile.whoAmI, Projectile.identity);
                ray.CritChance = Projectile.CritChance;
            }
        }
        else if (Kind == 3) {
            // Black Hawk: inertia 40, approach beyond 200px, back away inside it.
            Vector2 delta = target == null ? home - Projectile.Center : target.Center - Projectile.Center;
            Vector2 desired = target == null ? delta.SafeNormalize(Vector2.Zero) * Math.Min(16, delta.Length() * .1f)
                : delta.SafeNormalize(Vector2.UnitX) * (delta.Length() > 200 ? 18 : -9);
            Projectile.velocity = (Projectile.velocity * 40 + desired) / 41;
            if (target != null && Clock % 96 is 30 or 38 or 46 && Visible(target)) {
                Vector2 predicted = target.Center + target.velocity * Math.Min(18, delta.Length() / 14);
                Fire(target, 3, (predicted - Projectile.Center).SafeNormalize(Vector2.UnitX) * 14);
            }
        }
        else if (Kind == 11) {
            // Belladonna Spirit: gently follow, rise, then throw a ballistic glob.
            Vector2 delta = home - Projectile.Center;
            if (delta.Length() > 300) Projectile.velocity = delta / 30;
            else if (delta.Length() > 100) Projectile.velocity = (Projectile.velocity * 37 + delta.SafeNormalize(Vector2.Zero) * 17) / 40;
            else Projectile.velocity *= .98f;
            if (target != null) {
                Projectile.velocity.Y -= .005f * (Clock % 75);
                if (Clock % 75 == 74 && Visible(target)) Fire(target, 11,
                    new Vector2(Math.Clamp((target.Center.X - Projectile.Center.X) / 48, -12, 12), -8) + Projectile.velocity * .25f);
            }
        }
        if (Kind != 7) {
            Projectile.rotation = Projectile.velocity.X * .025f;
            foreach (Projectile other in Main.ActiveProjectiles)
                if (other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner && other.type == Type
                    && Vector2.DistanceSquared(other.Center, Projectile.Center) < 26 * 26)
                    Projectile.velocity += (Projectile.Center - other.Center).SafeNormalize(Vector2.UnitX) * .08f;
        }
        bool Visible(NPC npc) => Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1);

    }
    internal static void RefreshCrown(Projectile crown, Player owner)
    {
        int slots = Math.Max(1, owner.maxMinions);
        if (crown.ai[2] <= 0) crown.ai[2] = Math.Max(1, crown.originalDamage);
        crown.minionSlots = slots;
        crown.originalDamage = Math.Max(1, (int)(crown.ai[2] * slots));
        crown.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(crown.originalDamage));
    }
    private void Fire(NPC target, int kind, Vector2 velocity)
    {
        if (Projectile.owner != Main.myPlayer) return;
        var p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
            ModContent.ProjectileType<BossArsenalShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, kind, kind == 7 ? target.whoAmI + 1 : 0);
        p.DamageType = DamageClass.Summon;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == 7) { BossArsenalPolishVisuals.Crown(Projectile); return false; }
        int item = Kind switch { 3 => ItemID.MechanicalEye, _ => ItemID.MagmaStone };
        Main.instance.LoadItem(item); Texture2D body = TextureAssets.Item[item].Value;
        Rectangle frame = Main.itemAnimations[item]?.GetFrame(body) ?? body.Frame();
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() * .5f, 1, SpriteEffects.None);
        return false;
    }
}
