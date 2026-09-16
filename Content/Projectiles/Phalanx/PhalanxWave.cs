using System;
using ArknightsMod.Content.Items.Weapons.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

public sealed class PhalanxWave : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int Tier => (int)Projectile.ai[0];
    private bool Charged => Projectile.ai[2] == 1;
    private bool Powerful => Broken || Charged;
    private bool Broken => Projectile.ai[1] == 10;
    private float MaxRadius => (180 + Tier * 36) * (Broken ? 1.4f : Projectile.ai[1] == 1 && Tier == 0 ? 1.45f : 1);
    private float Progress => 1 - Projectile.timeLeft / 24f;
    private float Radius => MaxRadius * MathF.Sqrt(Math.Max(0, Progress));
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 24;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0)
        {
            PhalanxVisuals.Burst(Projectile.Center, Tier, Powerful ? 90 : 24, Powerful ? 13 : 6);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = Powerful ? .85f : .28f,
                Pitch = Powerful ? -.45f : .1f + Tier * .15f, MaxInstances = 4 }, Projectile.Center);
        }
        if (!Main.dedServ)
        {
            for (int i = 0; i < (Powerful ? 12 : 5); i++)
            {
                Vector2 outward = Main.rand.NextVector2Unit();
                Dust dust = Dust.NewDustPerfect(Projectile.Center + outward * Radius, DustID.TintableDustLighted,
                    outward * 2, 100, PhalanxVisuals.Palette(Tier), Powerful ? 1.8f : 1.1f);
                dust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, PhalanxVisuals.Palette(Tier).ToVector3() * (1 - Progress));
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(nearest, Projectile.Center) <= Radius * Radius
            && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        PhalanxVisuals.Burst(target.Center, Tier, Powerful ? 24 : 10, Powerful ? 8 : 4);
        if (Tier == 1 && Projectile.ai[1] == 1) target.AddBuff(ModContent.BuffType<CarnelianSandBind>(), 90);
        if (Tier == 2) target.AddBuff(BuffID.Confused, 45);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Projectile.Center - Main.screenPosition;
        float fade = 1 - Progress;
        Color color = PhalanxVisuals.Palette(Tier);
        color.A = 0;
        // A textured shock front at the real damage radius, without radial spokes or polygon outlines.
        PhalanxVisuals.Shockwave(center, new Vector2(Radius * 2), color * fade * (Powerful ? .8f : .55f),
            Progress * (Tier == 1 ? .18f : -.12f));
        PhalanxVisuals.Glow(center, new Vector2((Powerful ? 140 : 64) * fade), color * fade * .25f);
        return false;
    }
}

public sealed class BeeswaxObelisk : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = 30; Projectile.height = 80;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 600;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || Projectile.owner == Main.myPlayer
            && (player.GetModPlayer<PhalanxPlayer>().Staff?.Tier != 0 || player.GetModPlayer<PhalanxPlayer>().Mode != 2))
        {
            if (Projectile.owner == Main.myPlayer) Projectile.Kill();
            return;
        }
        // Keep remote copies alive as well; the owner sends the despawn when the skill ends.
        Projectile.timeLeft = 30;
        if (Projectile.ai[0]++ % 60 == 0 && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<PhalanxWave>(), Projectile.damage / 2, 6f, Projectile.owner, 0, 0);
        if (Projectile.ai[0] == 1) PhalanxVisuals.Burst(Projectile.Center, 0, 50, 7);
        Lighting.AddLight(Projectile.Center, 1f, .6f, .12f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Projectile.ai[0] / 20f, 0, 1);
        Vector2 c = Projectile.Center - Main.screenPosition;
        Color color = PhalanxVisuals.Palette(0) * fade;
        Main.instance.LoadItem(ItemID.SandstoneColumn);
        PhalanxVisuals.Sprite(TextureAssets.Item[ItemID.SandstoneColumn].Value,
            c + new Vector2(0, 40 * (1 - fade)), new Vector2(32, 80 * fade), Color.White * fade);
        return false;
    }
}

public sealed class CarnelianSandBind : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Slow;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex)
    {
        if (!npc.boss && npc.realLife < 0 && npc.knockBackResist > 0) npc.velocity *= .9f;
    }
}

public sealed class LinGlassShard : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic; Projectile.timeLeft = 70;
        Projectile.penetrate = 1; Projectile.extraUpdates = 1;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, .4f, .12f, .4f);
    }
    public override void OnKill(int timeLeft) => PhalanxVisuals.Burst(Projectile.Center, 2, 8, 3);
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = PhalanxVisuals.Palette(2); color.A = 0;
        Vector2 c = Projectile.Center - Main.screenPosition;
        Main.instance.LoadItem(ItemID.CrystalShard);
        PhalanxVisuals.Glow(c, new Vector2(28, 14), color * .25f, Projectile.rotation);
        PhalanxVisuals.Sprite(TextureAssets.Item[ItemID.CrystalShard].Value, c,
            new Vector2(10, 22), Color.White, Projectile.rotation + MathHelper.PiOver2);
        return false;
    }
}
