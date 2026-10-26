using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Projectiles.BasePROJ;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Gavial;

// Damaging hot metal ejected from Gavial's saw. Blood/mist contact VFX are a separate system.
public sealed class GavialSawSpark : ModProjectile
{
    private bool impactPlayed;
    private int Age => (int)Projectile.localAI[0];
    public override string Texture => "Terraria/Images/Extra_98";
    public override void SetStaticDefaults()
    {
        Terraria.ID.ProjectileID.Sets.TrailCacheLength[Type] = 14;
        Terraria.ID.ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1; Projectile.timeLeft = 150; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 10;
    }
    public override bool? CanDamage() => Age >= 10 ? null : false;
    public override void AI()
    {
        Projectile.localAI[0]++; Projectile.tileCollide = Age >= 12; Projectile.rotation = Projectile.velocity.ToRotation();
        if (Age >= 7) Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + .18f, 16);
        Projectile.velocity.X *= .996f;
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, new Vector3(.75f, .46f, .12f));
        if (Age > 2 && Age % 3 == 0) MetalSparks(1, 2);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Keep the existing cutting-shard gameplay; only its projectile identity/presentation changes.
        if (damageDone > 0) target.AddBuff(ModContent.BuffType<WeaponBleedingDebuff>(), 180);
        Impact();
    }
    public override bool OnTileCollide(Vector2 oldVelocity) { Impact(); return true; }
    public override void OnKill(int timeLeft) => Impact();
    private void Impact()
    {
        if (impactPlayed) return; impactPlayed = true;
        if (!Main.dedServ) MetalSparks(9, 6.5f);
    }
    private void MetalSparks(int count, float speed)
    {
        Vector2 rebound = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
        for (int i = 0; i < count; i++)
        {
            Dust spark = Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.FireworksRGB,
                rebound.RotatedByRandom(1.15f) * Main.rand.NextFloat(speed * .45f, speed),
                0, new Color(255, 180, 62), Main.rand.NextFloat(.7f, 1.2f));
            spark.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.dedServ) return false;
        SpriteBatch batch = Main.spriteBatch;
        float fade = Math.Min(Math.Clamp(Age / 4f, 0, 1), Math.Clamp(Projectile.timeLeft / 10f, 0, 1));
        BaseHeldMeleeSupport.BeginAdditive(batch);
        for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
        {
            if (Projectile.oldPos[i] == Vector2.Zero) continue;
            float tail = 1 - i / (float)Projectile.oldPos.Length;
            BaseHeldMeleeSupport.DrawSharpTear(Projectile.oldPos[i] + Projectile.Size / 2, Projectile.rotation,
                new Color(255, 144, 36), 20 * Projectile.scale, 3.5f * Projectile.scale, fade * tail * .35f);
        }
        BaseHeldMeleeSupport.DrawSharpTear(Projectile.Center, Projectile.rotation, new Color(255, 158, 42), 31 * Projectile.scale, 6 * Projectile.scale, fade);
        BaseHeldMeleeSupport.DrawSharpTear(Projectile.Center, Projectile.rotation, new Color(255, 242, 174), 18 * Projectile.scale, 2 * Projectile.scale, fade);
        BaseHeldMeleeSupport.EndAdditive(batch);
        return false;
    }
    internal static void SpawnFromSaw(Projectile saw, Vector2 origin, Vector2 forward)
    {
        if (saw.owner != Main.myPlayer) return;
        Vector2 velocity = forward.SafeNormalize(Vector2.UnitX).RotatedByRandom(MathHelper.ToRadians(21) * 1.3f) * Main.rand.NextFloat(8, 12.5f);
        velocity.Y -= Main.rand.NextFloat(.35f, 1.5f);
        int index = Projectile.NewProjectile(saw.GetSource_FromThis(), origin + Main.rand.NextVector2Circular(4, 4), velocity,
            ModContent.ProjectileType<GavialSawSpark>(), Math.Max(1, (int)MathF.Round(saw.damage * .24f)), saw.knockBack * .22f, saw.owner, 1);
        if (Main.projectile.IndexInRange(index)) { Main.projectile[index].scale = Main.rand.NextFloat(.82f, 1.18f); Main.projectile[index].netUpdate = true; }
    }
}
