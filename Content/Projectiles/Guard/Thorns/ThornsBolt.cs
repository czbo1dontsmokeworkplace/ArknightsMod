using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Thorns
{
    public class ThornsBolt : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/Thorns/ThornsWeaponProj";
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.scale = 1f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.penetrate = 1;
            Projectile.alpha = 255;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 60;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<sjds2>(), 180);
            for (int i = 0; i < 5; i++)
            {
                Dust v = Dust.NewDustDirect(target.Center, 0, 0, 1, 0f, 0f, 0, new Color(255, 200, 20, 200), 1.7f + Projectile.ai[0] / 2f);
                v.velocity = -(Projectile.velocity + new Vector2(Main.rand.Next(-5, 6), Main.rand.Next(-5, 6))).SafeNormalize(Vector2.Zero) * (2f + Main.rand.Next(3));
                v.noGravity = true;
                Dust s = Dust.NewDustDirect(target.Center, 0, 0, 1, 0f, 0f, 0, new Color(255, 200, 20, 200), 1.7f + Projectile.ai[0] / 2f);
                s.velocity = -(Projectile.velocity + new Vector2(Main.rand.Next(-5, 6), Main.rand.Next(-5, 6))).SafeNormalize(Vector2.Zero) * (2f + Main.rand.Next(3));
                s.noGravity = true;
            }
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft += ((int)Projectile.ai[0] * 90);
            base.OnSpawn(source);
        }
        public override void AI()
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height
            , 1, 0f, 0f, 0, new Color(255, 200, 0, 200), 1.7f);
            dust.noGravity = true;
            dust.velocity = Projectile.velocity / 2f;
            dust.position = Projectile.Center;
            Projectile.rotation = (Projectile.velocity).ToRotation() + MathHelper.PiOver2 * 2f;
            if (Projectile.ai[0] >= 1)
            {
                NPC target = null;
                float distanceMax = 300f;
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.type != NPCID.TargetDummy && !npc.dontTakeDamage)
                    {
                        float currentDistance = Vector2.Distance(npc.Center, Projectile.Center) + Vector2.Distance(npc.Size, new Vector2(0)) / 2f;
                        if (currentDistance < distanceMax)
                        {
                            distanceMax = currentDistance;
                            target = npc;
                        }
                    }
                }

                if (target != null)
                {
                    Vector2 targetVec = target.Center - Projectile.Center;
                    targetVec.Normalize();
                    targetVec *= 30f;
                    Projectile.velocity = (Projectile.velocity * 20f + targetVec) / 21f;
                }
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, 0), null, new Color(255, 200, 20, 222),
                Projectile.velocity.ToRotation() + 3.141f, texture.Size() / 2f, 1.3f + Projectile.ai[0] / 2f, SpriteEffects.None, 0);
            return false;
        }
    }
}
