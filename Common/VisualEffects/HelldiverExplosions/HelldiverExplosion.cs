using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using ReLogic.Content;

namespace ArknightsMod.Common.VisualEffects.HelldiverExplosions;

// A synchronized visual event. It never hits NPCs or creates damaging children.
public sealed class HelldiverExplosion : ModProjectile
{
    private HelldiverExplosionEmitter emitter;
    private int Theme => Math.Clamp((int)Projectile.ai[0], 0, 7);
    internal static float Radius(int theme) => theme < 4 ? 65 : 230 * (theme switch { 4 => .7f, 5 => .85f, 6 => 1f, _ => 1.2f });
    internal static Color ThemeColor(int theme) => theme < 4 ? ChargedOperatorRules.ColorFor((ChargedOperatorKind)theme) :
        SpreadArtsVfx.Body(theme switch { 4 => SpreadArt.Ember, 5 => SpreadArt.Crystal, 6 => SpreadArt.Rupture, _ => SpreadArt.Meteor });
    internal static void Spawn(Projectile source, Vector2 at, int theme)
    {
        if (source.owner != Main.myPlayer) return;
        Projectile.NewProjectile(source.GetSource_FromThis(), at, Vector2.Zero, ModContent.ProjectileType<HelldiverExplosion>(), 0, 0, source.owner, theme);
    }
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 90; Projectile.penetrate = -1; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Main.dedServ) return;
        if (emitter == null) emitter = new HelldiverExplosionEmitter(Radius(Theme), Theme < 4 ? ExplosionStyle.Plasma : ExplosionStyle.Nuclear, ThemeColor(Theme), Projectile.identity);
        else emitter.Update();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (emitter == null || Vector2.DistanceSquared(Projectile.Center, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2) > 2300 * 2300) return false;
        if (HelldiverExplosionArt.Bloom == null || HelldiverExplosionArt.Bloom.IsDisposed)
            HelldiverExplosionArt.Assign(name => ModContent.Request<Texture2D>(HelldiverExplosionArt.Assets + name, AssetRequestMode.ImmediateLoad).Value);
        emitter.Draw(Main.spriteBatch, Projectile.Center - Main.screenPosition);
        return false;
    }
}
