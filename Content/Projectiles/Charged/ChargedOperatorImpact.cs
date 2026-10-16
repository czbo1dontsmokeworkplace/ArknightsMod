using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

public sealed class ChargedOperatorImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private float Radius => Math.Clamp(Projectile.ai[1], 16, 150);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.friendly = true; Projectile.tileCollide = false;
        Projectile.penetrate = -1; Projectile.timeLeft = 20; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 17 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) <= Radius * Radius;
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
    public override void AI()
    {
        Projectile.DamageType = ChargedOperatorRules.IsMelee(Kind) ? DamageClass.Melee : DamageClass.Ranged;
        if (Projectile.timeLeft == 20)
        {
            ChargedOperatorVfx.Burst(Projectile.Center, Kind, 20, Radius);
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item14 with { Volume = .32f, Pitch = .2f }, Projectile.Center);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float life = 1 - Projectile.timeLeft / 20f;
        Color color = ChargedOperatorRules.ColorFor(Kind);
        string body = Kind switch { ChargedOperatorKind.Melantha => "ImpactPetals", ChargedOperatorKind.Matoimaru => "CrystalVortex",
            ChargedOperatorKind.Franka => "ForwardFlame", ChargedOperatorKind.Andreana => "CrystalVortex", _ => "RuptureRing" };
        using (var bodyPass = new ChargedOperatorVfx.Pass(false))
            ChargedOperatorVfx.Sprite(body, Projectile.Center, new Vector2(Radius * 2 * (.35f + .65f * life)),
                Color.Lerp(new Color(18, 12, 28), color, .25f) * (1 - life) * .55f, Projectile.identity * .7f);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite(body, Projectile.Center, new Vector2(Radius * 2 * (.35f + .65f * life)), color * (1 - life) * .6f, Projectile.identity * .7f);
        ChargedOperatorVfx.Sprite("StarCore", Projectile.Center, new Vector2(Radius * .7f), Color.Lerp(color, Color.White, .25f) * (1 - life) * .5f);
        return false;
    }
}

// Firewatch: a visible 45-tick designation before the blast. Nearl: persistent local Blazing Sun.
public sealed class ChargedOperatorField : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int age;
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.friendly = true; Projectile.tileCollide = false;
        Projectile.penetrate = -1; Projectile.timeLeft = 3600; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 60;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[1] == 3 && age % 60 < 3 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) < 120 * 120;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DefenseEffectiveness *= 0;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => OperatorStunNPC.TryApply(target, 30);
    public override void AI()
    {
        age++; Player owner = Main.player[Projectile.owner];
        Projectile.DamageType = Projectile.ai[1] == 3 ? DamageClass.Melee : DamageClass.Ranged;
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Kind != Kind
            || (Projectile.owner == Main.myPlayer && Projectile.ai[1] == 3 && ChargedOperatorPlayer.ActiveSkill(owner) != 3) || age > Projectile.ai[2])
        { Projectile.Kill(); return; }
        if (Projectile.ai[1] == 2 && age == 45 && Projectile.owner == Main.myPlayer)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ChargedOperatorImpact>(), Projectile.damage, 6, Projectile.owner, (int)Kind, 130);
            Projectile.Kill(); return;
        }
        if (Projectile.ai[1] == 3 && age % 30 == 1) ChargedOperatorVfx.Burst(Projectile.Center, Kind, 8, 100);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        Color color = ChargedOperatorRules.ColorFor(Kind);
        if (Projectile.ai[1] == 2)
        {
            float progress = Math.Clamp(age / 45f, 0, 1);
            ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(260), color * (.2f + .35f * progress));
            ChargedOperatorVfx.Line(Projectile.Center - Vector2.UnitY * (160 * (1 - progress)), Projectile.Center, color * .65f, 2);
        }
        else
        {
            ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(240, 80), color * .3f);
            ChargedOperatorVfx.Sprite("StarCore", Projectile.Center - new Vector2(0, 42), new Vector2(48, 120), color * .65f, MathHelper.PiOver2);
        }
        return false;
    }
}
