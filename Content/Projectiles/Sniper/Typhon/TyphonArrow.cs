using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon
{
    public class TyphonArrow : ModProjectile
    {
        public bool Vanity;
        public bool Vanity2;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
        }

        public override bool PreAI()
        {
            if (Vanity)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                if (Projectile.alpha < 127)
                    Projectile.alpha += 7;

                if (Projectile.alpha > 127)
                    Projectile.alpha = 127;

                if (Projectile.timeLeft <= 2)
                {
                    Vanity = false;
                    Vanity2 = true;
                    Projectile.timeLeft = 60;
                }
                return false;
            }

            if (Vanity2)
            {
                if (Projectile.timeLeft % 12 == 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), new Vector2(Projectile.ai[0], Projectile.ai[1]) - new Vector2(Main.rand.NextFloat(-16, 16), 1000), Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2, Type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                return false;
            }
            return base.PreAI();
        }

        public override bool ShouldUpdatePosition()
        {
            return !Vanity2;
        }

        public override bool? CanDamage()
        {
            if (Vanity)
                return false;
            return base.CanDamage();
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_ItemUse_WithAmmo)
            {
                Vanity = true;
                Projectile.timeLeft = 120;
                Projectile.alpha = 255;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Vanity || Main.myPlayer != Projectile.owner)
                return;
            Projectile.NewProjectile(Projectile.GetSource_Death(), new Vector2(Projectile.ai[0], Projectile.ai[1]) - new Vector2(Main.rand.NextFloat(-16, 16), 1000), Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2, Type, Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.MediumPurple;
            color.A = 250;
            if (!Vanity)
                color.A = lightColor.A;
            return color;
        }
    }
}
