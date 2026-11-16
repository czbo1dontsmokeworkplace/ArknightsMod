using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Developer;

// Unchanged 22x2 WulfrimBulletPROJ art from ReforgedReformed, with centered sprite afterimages.
public sealed class SR25Round : ModProjectile
{
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 2;
        Projectile.timeLeft = 540;
        Projectile.ignoreWater = true;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, .08f, .24f, .12f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
        {
            if (Projectile.oldPos[i] == Vector2.Zero) continue;
            float opacity = (1 - i / (float)Projectile.oldPos.Length) * .38f;
            Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition, null,
                Color.White * opacity, Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
            Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None);
        return false;
    }
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 4; i++)
        {
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch,
                -Projectile.velocity.RotatedByRandom(.65f) * Main.rand.NextFloat(.05f, .18f), 120, default, .55f);
            d.noGravity = true;
        }
    }
}

// Calamity OmniSniperCasing art; genuine Terraria gore gravity, collisions and fading.
public sealed class SR25EjectedCasing : ModGore
{
    public override void OnSpawn(Gore gore, IEntitySource source)
    {
        gore.timeLeft = 120;
        gore.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
}
