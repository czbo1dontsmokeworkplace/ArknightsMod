using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Projectiles.Caster.Passenger;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Leizi;

public class LeiziFlyingDischarge : ModProjectile
{
    internal const float SpeedPerUpdate = 13.8f;
    private bool impactHandled;
    internal virtual LightningPalette Palette => LightningPalette.Leizi;
    internal virtual float VisualSize => 0.85f;
    internal virtual int ChainProjectileType => ModContent.ProjectileType<LeiziChainDischarge>();
    internal virtual int ChainTargetLimit => ChainArtsVfx.TargetCount;
    // 与布丁、异客共用完整雷束结构；两个技能保持普攻视觉强度的 120% / 140%。
    internal static float Intensity(int mode) => 0.85f * (mode == 2 ? 1.4f : mode == 1 ? 1.2f : 1f);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 24;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 2;
        Projectile.timeLeft = 180; // 12 像素 × 180 次更新 = 2160 像素，持续 60 帧。
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => !impactHandled && target.CanBeChasedBy(Projectile) ? null : false;
    public override bool PreDraw(ref Color lightColor) { ChainArtsVfx.Head(Projectile, Palette, 3); return false; }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead)
        {
            Projectile.Kill();
            return;
        }
        if (Main.dedServ || Projectile.numUpdates != 0)
            return;
        int phase = (int)Projectile.localAI[0]++;
        Vector2 end = Projectile.Center + Projectile.velocity;
        Vector2 start = end - Projectile.velocity * 3f;
        float scale = Intensity((int)Projectile.ai[0]) * VisualSize / 0.85f;
        EmitFlightVisuals(start, end, scale, phase);
    }

    protected virtual void EmitFlightVisuals(Vector2 start, Vector2 end, float scale, int phase)
    {
        ChainArtsVfx.Flight(start, end, scale, phase, Projectile.identity * 397 + phase * 13, Palette, 3);
        Lighting.AddLight(Projectile.Center, Palette.Light * (scale * 0.7f));
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (impactHandled)
            return;
        impactHandled = true;
        if (!target.boss && target.knockBackResist > 0f && target.realLife < 0)
            target.AddBuff(ModContent.BuffType<PassengerCurrentSlow>(), 30);
        if (Projectile.owner != Main.myPlayer)
            return;
        int mode = (int)Projectile.ai[0];
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
            ChainProjectileType, Projectile.damage, Projectile.knockBack,
            Projectile.owner, mode == 2 ? 1.4f : mode == 1 ? 1.2f : 1f, 30f, ChainTargetLimit);
        if (!Main.projectile.IndexInRange(index))
            return;
        Projectile chain = Main.projectile[index];
        chain.CritChance = Projectile.CritChance;
        ((PassengerChainDischarge)chain.ModProjectile).InitializeRoute(Main.player[Projectile.owner], target.Center, target);
    }
}

// 共用确定性选敌与同步；惊蛰保留较小的紫白节点与初雷规则。
public sealed class LeiziChainDischarge : PassengerChainDischarge
{
    internal override LightningPalette Palette => LightningPalette.Leizi;
    protected override float VisualSize => 0.85f;
    protected override void BeginHopVisuals(Vector2 start, Vector2 end, float scale, float nodeIntensity, int seed, int hop, bool targetNode)
        => ChainArtsVfx.Hop(start, end, scale, nodeIntensity, seed, 3, Palette, targetNode);
    protected override void ImpactHopVisuals(Vector2 end, float intensity, int seed, bool firstHop, bool collided)
    {
        ChainArtsVfx.Impact(end, intensity, 3, Palette);
        PassengerLightningVisuals.ElectricImpactDust(end, intensity * .7f, firstHop, Palette);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // ai[0]=1.4 只由初雷生成，随原版弹幕消息同步；其余攻击保留 15% 递减。
        if (Projectile.ai[0] < 1.39f)
            base.ModifyHitNPC(target, ref modifiers);
    }
}
