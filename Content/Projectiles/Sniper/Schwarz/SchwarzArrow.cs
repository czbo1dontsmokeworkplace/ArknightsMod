using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Players;

namespace ArknightsMod.Content.Projectiles.Sniper.Schwarz
{
    // ── 双 UV 顶点，专供 SchwarzDistortion 使用 ───────────────────────────────
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DistortionVertex : IVertexType
    {
        public Vector3 Position;   // 世界坐标
        public Color   Color;      // alpha 淡出
        public Vector2 ScreenUV;   // 对应截图中的采样 UV [0,1]
        public Vector2 TrailUV;    // x=crossU [0左~1右], y=progress [0头~1尾]

        public static readonly VertexDeclaration Declaration = new(
            new VertexElement( 0, VertexElementFormat.Vector3, VertexElementUsage.Position,          0),
            new VertexElement(12, VertexElementFormat.Color,   VertexElementUsage.Color,             0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1));

        public VertexDeclaration VertexDeclaration => Declaration;
    }
    // ─────────────────────────────────────────────────────────────────────────

    public class SchwarzArrow : ModProjectile
    {
        private static Asset<Effect> _distortEffect;
        Player player => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.friendly = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
            if (Main.myPlayer == player.whoAmI) {
                if (modPlayer.SkillActive == true){
                    if (modPlayer.Skill == 0 && Main.rand.NextFloat() < 0.8f) {
                        target.AddBuff(36, 300);
                    }
                    else if (modPlayer.Skill == 1 && Main.rand.NextFloat() < 0.5f) {
                        target.AddBuff(36, 300);
                    }
                    else if (modPlayer.Skill == 2 ) {
                        target.AddBuff(36, 300);
                    }
                    // 这里要黑命中时的纹理图暂时没有
                }
                else {
                    if (Main.rand.NextFloat() < 0.2f)
					    target.AddBuff(36, 300);
                }
			}

        }
        

        public override void AI()
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(3))
            {
                Vector2 spawnPos = Projectile.Center
                    - Projectile.velocity * Main.rand.NextFloat(0.1f, 0.5f);

                // 垂直于飞行方向的随机偏移，让碎片向两侧散开
                Vector2 perpendicular = Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                Vector2 scatterVel = perpendicular * Main.rand.NextFloat(-2f, 2f)
                    + Projectile.velocity * Main.rand.NextFloat(-0.1f, 0.3f);

                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    spawnPos,
                    scatterVel,
                    ModContent.ProjectileType<TrailShard>(),
                    0,
                    0,
                    Projectile.owner
                );

                if (idx >= 0 && idx < Main.maxProjectiles)
                {
                    Projectile p = Main.projectile[idx];
                    p.ai[0] = Main.rand.NextBool() ? 0f : 1f; // 0=白 1=黑
                    // 宽高比：localAI存储，不需要网络同步（纯视觉）
                    p.localAI[0] = Main.rand.NextFloat(4f, 10f); // 宽
                    p.localAI[1] = Main.rand.NextFloat(3f, 7f);  // 高
                    p.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                }
            }
            base.AI();
        }
    }

    public class TrailShard : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.timeLeft = 45;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.88f;
            Projectile.velocity.Y += 0.08f; // 轻微重力，让碎片自然下落
            Projectile.alpha += 5;
            // 自旋：ai[0]==0的白色碎片顺时针，黑色逆时针
            Projectile.rotation += Projectile.ai[0] == 0f ? 0.12f : -0.12f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.alpha >= 255)
                return false;

            Texture2D tex = TextureAssets.MagicPixel.Value;
            Color color = Projectile.ai[0] == 0f ? Color.White : Color.Black;
            float alpha = 1f - Projectile.alpha / 255f;

            // 主碎片：用localAI存储的随机宽高构造不规则形状
            float w = Projectile.localAI[0];
            float h = Projectile.localAI[1];

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color * alpha,
                Projectile.rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(w, h),
                SpriteEffects.None,
                0f
            );

            // 叠加一个旋转45°的小矩形，让轮廓更不规则
            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color * (alpha * 0.6f),
                Projectile.rotation + MathHelper.PiOver4,
                new Vector2(0.5f, 0.5f),
                new Vector2(w * 0.6f, h * 0.6f),
                SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}
