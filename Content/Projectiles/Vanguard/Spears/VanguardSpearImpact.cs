using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

public sealed class VanguardSpearImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private float Radius => Math.Clamp(Projectile.ai[1], 16, 80);
    public static void Spawn(Projectile source, Vector2 point, VanguardSpearKind kind, int damage, float radius)
    {
        if (source.owner != Main.myPlayer) return;
        Projectile p = Projectile.NewProjectileDirect(source.GetSource_FromThis(), point, Vector2.Zero,
            ModContent.ProjectileType<VanguardSpearImpact>(), damage, source.knockBack * .6f, source.owner, (int)kind, radius);
        p.CritChance = source.CritChance;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 20;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.damage > 0 && Projectile.ai[2] <= 2 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.friendly || !Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) <= Radius * Radius;
    public override void AI()
    {
        if (Projectile.ai[2]++ == 0)
        {
            VanguardSpearVfx.Impact(Projectile.Center, Vector2.UnitX, (VanguardSpearKind)(int)Projectile.ai[0], true);
            if (!Main.dedServ && Projectile.damage > 0)
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .45f, Pitch = .35f }, Projectile.Center);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float progress = Projectile.ai[2] / 20f;
        Color color = VanguardSpearRules.Tint((VanguardSpearKind)(int)Projectile.ai[0]);
        // Diameter matches the collision radius. No full-screen white flash.
        using (new ChargedOperatorVfx.Pass(false))
            ChargedOperatorVfx.Sprite("ImpactPetals", Projectile.Center, new Vector2(Radius * 1.6f) * (.6f + progress * .4f), color * (.6f * (1 - progress)));
        using (new ChargedOperatorVfx.Pass(true))
        {
            ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(Radius * 2) * (.75f + .25f * progress), color * (1 - progress));
            ChargedOperatorVfx.Sprite("StarCore", Projectile.Center, new Vector2(Radius * .7f) * (1 - progress), Color.Lerp(color, Color.White, .2f) * (1 - progress));
        }
        return false;
    }
}

// Bagpipe's S2/S3 extra strikes are bound to the hit enemy and delayed 4/8 ticks.
// They neither spread across an area nor regenerate the throwing reserve.
public sealed class VanguardFollowupStrike : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private bool played;
    private Vector2 origin;
    private bool initialized;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = 1; Projectile.timeLeft = 18;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[1] <= 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0] && !target.friendly ? null : false;
    public override void AI()
    {
        int index = (int)Projectile.ai[0];
        if (!initialized) { initialized = true; origin = Projectile.Center; }
        if (index < 0 || index >= Main.maxNPCs) { Projectile.Kill(); return; }
        NPC target = Main.npc[index];
        if (!target.active || target.type != (int)Projectile.ai[2] || target.friendly || target.dontTakeDamage
            || Vector2.DistanceSquared(target.Center, origin) > 320 * 320)
        { Projectile.Kill(); return; }
        Projectile.Center = target.Center; Projectile.ai[1]--;
        if (Projectile.ai[1] <= 0 && !played)
        {
            played = true;
            VanguardSpearVfx.Impact(Projectile.Center, Vector2.UnitX, VanguardSpearKind.Bagpipe, false);
        }
        if (Projectile.ai[1] < -2) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
