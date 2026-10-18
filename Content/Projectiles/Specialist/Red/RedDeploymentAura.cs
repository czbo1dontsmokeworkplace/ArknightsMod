using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Specialist.Red;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Red;

// 无伤害的五秒状态载体。ai[0] 同步已过去的时间，迟加入观察者不会重新看到五秒完整效果。
public sealed class RedDeploymentAura : ModProjectile
{
    public override string Texture => ArknightsMod.noTexture;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = RedDeploymentState.Duration;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        int remaining = RedDeploymentState.Duration - (int)Projectile.ai[0];
        if (!owner.active || owner.dead || remaining <= 0)
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = owner.MountedCenter;
        owner.GetModPlayer<RedDaggerPlayer>().ReceiveDeployment(remaining, (int)Projectile.ai[1], (int)Projectile.ai[2]);
        Projectile.timeLeft = remaining + 1;
        int age = (int)Projectile.ai[0]++;
        if (Projectile.owner == Main.myPlayer && age % 60 == 0)
            Projectile.netUpdate = true;
        if (Main.dedServ || owner.HeldItem.ModItem is not RedDagger)
            return;
        Lighting.AddLight(owner.Center, new Vector3(.24f, .012f, .028f) * Math.Min(1, remaining / 30f));
        ExecutorStatusVisuals.Emit(Projectile, age, ExecutorStatusVisuals.Red, Math.Min(1f, remaining / 24f));
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not RedDagger) return false;
        int age = (int)Projectile.ai[0];
        float fade = Math.Min(1f, Math.Min(age / 12f, (RedDeploymentState.Duration - age) / 24f));
        ExecutorStatusVisuals.Draw(Projectile.Center, age, ExecutorStatusVisuals.Red, fade, owner.gravDir);
        return false;
    }
}
