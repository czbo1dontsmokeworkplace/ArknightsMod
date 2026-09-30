using System;
using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

public sealed class ScatterPellet : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int Tier => (int)Projectile.ai[0];
    private int Mode => (int)Projectile.ai[1];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 100; Projectile.extraUpdates = 1;
        Projectile.penetrate = 2; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    private bool configured;
    public override void OnSpawn(IEntitySource source) => ConfigureFlight();
    private void ConfigureFlight()
    {
        if (configured) return;
        configured = true;
        if (Tier == 1)
        {
            // Was 2 updates/frame. Now 3; compensate velocity by 4/3 for exactly twice the old travel.
            Projectile.extraUpdates = 2;
            Projectile.velocity *= 4f / 3f;
        }
        Projectile.penetrate = Tier == 2 ? 5 : 2;
        if (Tier == 2) Projectile.timeLeft = 150;
        Projectile.ArmorPenetration = Tier == 1 ? (Mode == 1 ? 35 : 15) : 0;
    }
    public override void AI()
    {
        ConfigureFlight();
        Projectile.ai[2] += Projectile.velocity.Length();
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Tier == 2) Projectile.velocity.Y += .025f;
        if (Projectile.ai[2] > (Tier == 2 ? 1230 : 820)) Projectile.Kill();
        Lighting.AddLight(Projectile.Center, ScatterVisuals.ColorFor(Tier).ToVector3() * .18f);
        if (Tier == 1 && Projectile.numUpdates == 0) ScatterVisuals.ExecutorFlight(Projectile);
        if (Tier == 2 && Projectile.numUpdates == 0)
        {
            ScatterVisuals.WaterTrail(Projectile);
        }
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Shotguns reward being close without losing their identity to long-range sniping.
        float range = MathHelper.Clamp((Projectile.ai[2] - 120) / 480, 0, 1);
        modifiers.SourceDamage *= MathHelper.Lerp(Tier == 1 ? 1.5f : 1.2f, .65f, range);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ScatterVisuals.Spray(Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY), Tier, Tier == 2 ? 12 : 7, 5);
        if (Tier == 2)
        {
            target.AddBuff(BuffID.Wet, 240);
            if (Mode > 0) target.AddBuff(ModContent.BuffType<HighPressureSoak>(), Mode == 2 ? 180 : 90);
            if (Mode == 2 && Projectile.owner == Main.myPlayer
                && Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<ChalterWaterField>()] < 3)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<ChalterWaterField>(), Math.Max(1, Projectile.damage / 5), 0,
                    Projectile.owner);
        }
        Projectile.damage = Math.Max(1, (int)(Projectile.damage * .8f));
    }
    public override void OnKill(int timeLeft) => ScatterVisuals.Spray(Projectile.Center,
        -Projectile.velocity.SafeNormalize(Vector2.UnitY), Tier, Tier == 2 ? 10 : 4, 3);
    public override bool PreDraw(ref Color lightColor)
    {
        if (Tier == 2) return false; // Drawn together by ChalterWaterRenderer, with one shader batch.
        Main.instance.LoadProjectile(ProjectileID.BulletHighVelocity);
        Texture2D bullet = TextureAssets.Projectile[ProjectileID.BulletHighVelocity].Value;
        PhalanxVisuals.Sprite(bullet, Projectile.Center - Main.screenPosition, bullet.Size(),
            Color.White, Projectile.rotation + MathHelper.PiOver2);
        return false;
    }

}

public sealed class HighPressureSoak : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Wet;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex)
    {
        if (!npc.boss && npc.realLife < 0 && npc.knockBackResist > 0) npc.velocity *= .94f;
    }
}

public sealed class ChalterWaterField : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = 160; Projectile.height = 80; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.timeLeft = 180;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        // All fields share the same hit cooldown, preventing three overlapping fields from triple-ticking.
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 30;
    }
    public override void AI()
    {
        Projectile.ai[0]++;
        if (!Main.dedServ && Main.rand.NextBool(3))
            ScatterVisuals.Spray(Projectile.Center + Main.rand.NextVector2Circular(65, 28), -Vector2.UnitY, 2, 2, 3);
    }
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Projectile.Center, 1, 1, target.Center, 1, 1) ? null : false;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { target.AddBuff(ModContent.BuffType<HighPressureSoak>(), 90); target.AddBuff(BuffID.Wet, 180); }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Math.Min(Projectile.ai[0], Projectile.timeLeft) / 20f, 0, 1);
        Color color = ScatterVisuals.ColorFor(2); color.A = 0;
        Vector2 center = Projectile.Center - Main.screenPosition;
        PhalanxVisuals.Glow(center, new Vector2(160, 72), color * fade * .22f);
        for (int j = 0; j < 2; j++)
        {
            float phase = (Projectile.ai[0] / 60f + j * .5f) % 1f;
            PhalanxVisuals.Shockwave(center, new Vector2(160, 72) * (.5f + phase * .5f),
                color * fade * (1 - phase) * .28f);
        }
        return false;
    }
}
