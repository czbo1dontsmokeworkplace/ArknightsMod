using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles
{
    public class MostimaRingAndWingProj : ModProjectile
    {
        Player player => Main.player[Projectile.owner];
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(226);
            Projectile.aiStyle = -1;
            Projectile.width = Projectile.height = 64;
        }
        public override void OnSpawn(IEntitySource source)
        {
            for (int num506 = 0; num506 < 15; num506++)
            {
                int num507 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Firework_Blue);
                Main.dust[num507].noGravity = true;
                Dust dust2 = Main.dust[num507];
                dust2.velocity += Projectile.oldVelocity * Main.rand.NextFloat();
                Main.dust[num507].scale = 1.5f;
            }
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.6f, 1f));
            if (player.HasBuff<Buffs.MostimaRingAndWingBuff>())
                Projectile.timeLeft = 3;
            Projectile.position.X = Main.player[Projectile.owner].Center.X - Projectile.width / 2;
            Projectile.position.Y = Main.player[Projectile.owner].Center.Y - Projectile.height / 2 + Main.player[Projectile.owner].gfxOffY - 7f;
            if (Main.player[Projectile.owner].gravDir == -1f)
            {
                Projectile.position.Y += 120f;
                Projectile.rotation = 3.14f;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (int)Projectile.position.X;
            Projectile.position.Y = (int)Projectile.position.Y;
            if (Main.rand.NextBool(120))
            {
                int num507 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Firework_Blue);
                Main.dust[num507].noGravity = true;
                Dust dust2 = Main.dust[num507];
                dust2.velocity += Projectile.oldVelocity * Main.rand.NextFloat();
                Main.dust[num507].scale = 1.5f;
            }
        }
    }
}
