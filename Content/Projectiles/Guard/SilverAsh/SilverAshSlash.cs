using ArknightsMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.SilverAsh
{
    public class SilverAshSlash : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/SilverAsh/SilverAshWeapon2";
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(49);
            AIType = 49;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.scale = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.idStaticNPCHitCooldown = 60;
        }
        bool jjj = false;
        int max = 0;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            max = player.itemTime;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (jjj == false)
            {
                jjj = true;
                Player player = Main.player[Projectile.owner];
                Projectile.NewProjectile(player.GetSource_Death(), Projectile.Center
                  , Projectile.velocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<TYTX>(), 0, 0, Main.myPlayer);
            }

        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (jjj == false && Projectile.ai[0] >= player.itemTime / 3.5f && Projectile.ai[1] == 1)
            {
                jjj = true;
                Projectile.NewProjectile(player.GetSource_Death(), Projectile.Center
                  , Projectile.velocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<SilverAshSlashEffect>(), 0, 0, Main.myPlayer);
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            return true;
        }
    }
}
