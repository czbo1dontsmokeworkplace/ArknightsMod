using System;
using ArknightsMod.Content.Items.Weapons.Scatterguns;
using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

public sealed class ScattergunHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int age;
    private float shotSerial = -1;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.timeLeft = 90;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        int tier = (int)Projectile.ai[0];
        if (!player.active || player.dead || player.HeldItem.ModItem is not Scattergun gun || gun.Tier != tier)
        { Projectile.Kill(); return; }
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Projectile.Center = player.MountedCenter + aim * 12;
        player.direction = aim.X < 0 ? -1 : 1;
        player.heldProj = Projectile.whoAmI;
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim.ToRotation() - MathHelper.PiOver2);
        if (shotSerial != Projectile.ai[2])
        {
            shotSerial = Projectile.ai[2];
            age = 0;
            Projectile.timeLeft = 90;
            SoundEngine.PlaySound((tier == 2 ? SoundID.Item84 : tier == 1 ? SoundID.Item38 : SoundID.Item11)
                with { Volume = tier == 1 ? .85f : .55f, Pitch = tier == 0 ? .25f : tier == 1 ? -.1f : 0f,
                    MaxInstances = 6 }, Projectile.Center);
            if (tier == 1) ScatterVisuals.ExecutorMuzzle(Projectile.Center + aim * 30, aim);
            else ScatterVisuals.Spray(Projectile.Center + aim * 30, aim, tier, tier == 2 ? 35 : 22, 9);
        }
        if (++age >= Projectile.ai[1]) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        int tier = (int)Projectile.ai[0];
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 center = Projectile.Center - Main.screenPosition;
        float kick = MathF.Exp(-age * .2f) * (tier == 1 ? 12 : 7);
        var player = Main.player[Projectile.owner];
        var texture = TextureAssets.Item[player.HeldItem.type].Value;
        float itemScale = 56f / Math.Max(texture.Width, texture.Height);
        bool left = aim.X < 0;
        PhalanxVisuals.Sprite(texture, center - aim * kick, texture.Size() * itemScale, lightColor,
            aim.ToRotation() + (left ? MathHelper.Pi : 0), left ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        float flash = Math.Max(0, 1 - age / 7f);
        if (flash > 0)
        {
            Color color = ScatterVisuals.ColorFor(tier); color.A = 0;
            Vector2 muzzle = center + aim * (30 - kick);
            PhalanxVisuals.Glow(muzzle, new Vector2(28, 18) * flash, color * flash * .65f, aim.ToRotation());
        }
        return false;
    }
}
