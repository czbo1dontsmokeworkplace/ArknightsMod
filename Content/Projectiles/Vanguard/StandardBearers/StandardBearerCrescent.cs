using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

// A short-lived filled sheet, not a persistent ranged attack. ai: palette, source identity, age.
public sealed class StandardBearerCrescent : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = StandardBearerRules.WaveSize;
        Projectile.timeLeft = StandardBearerRules.WaveLife;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.tileCollide = false; // bounded manual steps below also work in the offline harness
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    private StandardBearerHoldout Source()
    {
        foreach (Projectile source in Main.ActiveProjectiles)
            if (source.owner == Projectile.owner && source.identity == (int)Projectile.ai[1]
                && source.ModProjectile is StandardBearerHoldout held && source.ai[0] == Projectile.ai[0]) return held;
        return null;
    }
    private bool ValidOwner() => (uint)Projectile.owner < Main.maxPlayers
        && Main.player[Projectile.owner] is { active: true, dead: false, CCed: false, noItems: false } owner
        && owner.HeldItem.ModItem is StandardBearerWeapon weapon && (int)weapon.Kind == (int)Projectile.ai[0]
        && !StandardBearerPlayer.IsPhased(owner);
    public override bool? CanDamage() => Projectile.ai[2] <= StandardBearerRules.WaveTravelFrames
        && Projectile.localAI[0] == 0 && ValidOwner() ? null : false;
    public override bool? CanHitNPC(NPC target)
    {
        // Share the spear's per-thrust hit ledger: sheet + shaft never double-hit the same enemy.
        StandardBearerHoldout source = Source();
        return source != null && !source.Idle && source.Projectile.localNPCImmunity[target.whoAmI] == 0
            && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                target.position, target.width, target.height) ? null : false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        StandardBearerHoldout source = Source();
        if (source != null) source.Projectile.localNPCImmunity[target.whoAmI] = -1;
    }
    public override void AI()
    {
        if (!ValidOwner() || Projectile.ai[0] < 0 || Projectile.ai[0] > 3)
        { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float age = Projectile.ai[2];
        float step = StandardBearerRules.WaveDistance(age + 1) - StandardBearerRules.WaveDistance(age);
        Vector2 next = Projectile.Center + direction * step;
        if (step > 0 && Collision.SolidCollision(next - Projectile.Size * .5f, Projectile.width, Projectile.height))
        {
            Projectile.localAI[0] = 1;
            age = StandardBearerRules.WaveTravelFrames;
        }
        else Projectile.Center = next;
        Projectile.rotation = direction.ToRotation();
        Projectile.ai[2] = age + 1;
        if (Projectile.ai[2] >= StandardBearerRules.WaveLife) Projectile.Kill();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center,
            StandardBearerRules.Tint((StandardBearerKind)(int)Projectile.ai[0]).ToVector3() * .28f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        StandardBearerCrescentMaterial.Draw(Projectile.Center, Projectile.rotation,
            (StandardBearerKind)(int)Projectile.ai[0], Projectile.ai[2]);
        return false;
    }
}
