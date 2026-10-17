using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

/// <summary>固定鼠标快照的真实曲射。110次子更新落至目标；碰墙或碰敌提前爆炸。</summary>
public sealed class CatapultShell : ModProjectile
{
    internal const int FlightUpdates = 110;
    internal const float Gravity = .16f;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Grenade;
    internal static Vector2 LaunchVelocity(Vector2 start, Vector2 target, float gravityDirection) =>
        new((target.X - start.X) / FlightUpdates,
            (target.Y - start.Y) / FlightUpdates - Gravity * gravityDirection * (FlightUpdates + 1) * .5f);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 10;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.timeLeft = FlightUpdates + 6;
        Projectile.extraUpdates = 1;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        float gravityDirection = Projectile.ai[1] < 0f ? -1f : 1f;
        Projectile.velocity.Y += Gravity * gravityDirection;
        Vector2 movement = Collision.TileCollision(Projectile.position, Projectile.velocity,
            Projectile.width, Projectile.height, true, true);
        Projectile.position += movement;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        bool impact = Vector2.DistanceSquared(movement, Projectile.velocity) > .001f;
        foreach (NPC target in Main.ActiveNPCs)
            if (target.CanBeChasedBy(Projectile) && Projectile.Hitbox.Intersects(target.Hitbox)) { impact = true; break; }
        // 只有拥有者决定死亡及生成伤害弹，服务器通过原版Kill同步转发。
        if (Projectile.owner == Main.myPlayer && (impact || ++Projectile.localAI[0] >= Projectile.ai[0]))
            Projectile.Kill();
        if (!Main.dedServ && Projectile.numUpdates == 0 && Projectile.timeLeft % 6 == 0)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * .12f, 90,
                new Color(255, 155, 65), .65f);
            dust.noGravity = true;
        }
    }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner != Main.myPlayer) return;
        float gravityDirection = Projectile.ai[1] < 0f ? -1f : 1f;
        int damage = Math.Max(1, (int)MathF.Round(Projectile.damage * (Projectile.velocity.Y * gravityDirection > 0f ? 1.25f : 1f)));
        int blast = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<CatapultExplosion>(), damage, Projectile.knockBack, Projectile.owner,
            MathHelper.Clamp(Projectile.ai[2], 56f, 112f));
        if (Main.projectile.IndexInRange(blast)) Main.projectile[blast].CritChance = Projectile.CritChance;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        CatapultVisuals.FlightTrail(Projectile);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.Grenade].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, texture.Size() * .5f, 12f / Math.Max(texture.Width, texture.Height), SpriteEffects.None);
        return false;
    }
}
