using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon
{
    /// <summary>
    /// 提丰 S3 持续视觉：在鼠标附近 500px 内自动锁定最近敌人，并在该 NPC 上播放瞄准框动画。
    /// 由 TyphonBow.HoldItem 在 S3 active 时确保每个玩家有且仅有一个 reticle。
    /// </summary>
    public class TyphonAimReticle : ModProjectile
    {
        public const float MouseSearchRadius = 500f;   // 鼠标半径内寻找敌人
        public const float ReticleAreaRadius = 120f;   // 瞄准框"区域内"半径，用于下落箭随机挑目标
        private const int  FrameCount        = 8;

        public override string Texture => "ArknightsMod/Content/Items/Weapons/Sniper/Typhon/skill3_aim";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width        = 128;
            Projectile.height       = 128;
            Projectile.aiStyle      = -1;
            Projectile.tileCollide  = false;
            Projectile.friendly     = false;
            Projectile.hostile      = false;
            Projectile.penetrate    = -1;
            Projectile.timeLeft     = 99999;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            var modPlayer = owner.GetModPlayer<WeaponPlayer>();
            bool s3Active = !owner.dead && owner.active
                && owner.HeldItem.ModItem is TyphonBow
                && modPlayer.Skill == 2 && modPlayer.SkillActive;

            if (!s3Active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = FindNearestEnemy(Main.MouseWorld, MouseSearchRadius);
            if (target != null)
            {
                Projectile.Center = target.Center;
                Projectile.alpha  = 0;
            }
            else
            {
                Projectile.Center = Main.MouseWorld;
                Projectile.alpha  = 140; // 找不到目标时半透明跟随鼠标
            }

            // 帧动画：每 5 帧推进一帧
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = TextureAssets.Projectile[Type].Value;
            int frameH      = tex.Height / FrameCount;
            Rectangle src   = new Rectangle(0, frameH * Projectile.frame, tex.Width, frameH);
            Vector2 origin  = new Vector2(tex.Width / 2f, frameH / 2f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float alphaMult = 1f - Projectile.alpha / 255f;
            Color color = Color.White * alphaMult;
            Main.spriteBatch.Draw(tex, drawPos, src, color, 0f, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }

        public static Vector2? GetCurrentPos(int playerWho)
        {
            int reticleType = ModContent.ProjectileType<TyphonAimReticle>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == playerWho && p.type == reticleType)
                    return p.Center;
            }
            return null;
        }

        private static NPC FindNearestEnemy(Vector2 origin, float range)
        {
            NPC best = null;
            float bestDistSq = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.life <= 0 || npc.dontTakeDamage) continue;
                float d = Vector2.DistanceSquared(npc.Center, origin);
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = npc;
                }
            }
            return best;
        }
    }
}
