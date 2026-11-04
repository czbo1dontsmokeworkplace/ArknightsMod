using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

// A single continuous mortar arc; no overhead transfer or teleport.
public sealed class UrsusMortarShell : ModProjectile
{
    private Vector2 strikePoint;
    private int Age => (int)Projectile.ai[1];
    internal const float Gravity = .32f;
    internal static Vector2 LaunchVelocity(Vector2 from, Vector2 target, Vector2 targetVelocity, out int flightTime) {
        flightTime = (int)Math.Clamp(Math.Abs(target.X - from.X) / 8 + 34, 50, 100);
        Vector2 delta = target + targetVelocity * (flightTime * .6f) - from;
        return new Vector2(delta.X / flightTime, delta.Y / flightTime - Gravity * (flightTime + 1) * .5f);
    }
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.RocketI;
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 18; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.penetrate = 1;
        Projectile.timeLeft = 240; Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(strikePoint.X); writer.Write(strikePoint.Y); }
    public override void ReceiveExtraAI(BinaryReader reader) { strikePoint = new Vector2(reader.ReadSingle(), reader.ReadSingle()); }
    public override bool? CanDamage() => Age < 8 || Projectile.velocity.Y <= 0 ? false : null;
    public override void AI() {
        NPC target = Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;
        if (target != null && target.CanBeChasedBy(Projectile)) strikePoint = target.Center;
        else if (strikePoint == Vector2.Zero) { Projectile.Kill(); return; }
        if (Age == 0 && Projectile.ai[2] <= 0) {
            Projectile.velocity = LaunchVelocity(Projectile.Center, strikePoint, target?.velocity ?? Vector2.Zero, out int duration);
            Projectile.ai[2] = duration;
        }
        Projectile.ai[1]++;
        Projectile.velocity.Y += Gravity;
        if (Projectile.velocity.Y > 0) {
            if (target != null && target.CanBeChasedBy(Projectile)) {
                float remaining = Math.Max(8, Projectile.ai[2] - Age);
                float desiredX = (target.Center.X + target.velocity.X * remaining * .5f - Projectile.Center.X) / remaining;
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Clamp(desiredX, -18, 18), .035f);
            }
            if (Projectile.Center.Y > strikePoint.Y - 64) Projectile.tileCollide = true;
        }
        if (Projectile.owner == Main.myPlayer && Age % 12 == 0) Projectile.netUpdate = true;
        Projectile.rotation = Projectile.velocity.LengthSquared() > .1f ? Projectile.velocity.ToRotation() + MathHelper.PiOver2 : MathHelper.Pi;
        if (!Main.dedServ && Age % 2 == 0) {
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * .12f, 60, Color.Orange, 1.1f * Projectile.Opacity);
            d.noGravity = true;
        }
    }
    public override void OnKill(int timeLeft) {
        if (Projectile.owner != Main.myPlayer || Age < 8) return;
        Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<BossArsenalBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 3, 100);
    }
    public override bool PreDraw(ref Color lightColor) {
        Main.instance.LoadProjectile(ProjectileID.RocketI); Texture2D body = TextureAssets.Projectile[ProjectileID.RocketI].Value;
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation, body.Size() * .5f, 1.1f, SpriteEffects.None); return false;
    }
}
