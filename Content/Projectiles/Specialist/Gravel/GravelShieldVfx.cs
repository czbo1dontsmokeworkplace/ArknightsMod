using System;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Specialist.Gravel;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Gravel;

// Both skills use the same small status marker; no filled shield or broad light ring.
public class GravelShieldVfx : ModProjectile
{
    public override string Texture => ArknightsMod.noTexture;
    private float strength = 1f;
    private int Age => (int)Projectile.ai[1];
    private float Duration => Math.Max(1f, Projectile.ai[2]);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(strength);
    public override void ReceiveExtraAI(BinaryReader reader) => strength = MathHelper.Clamp(reader.ReadSingle(), 0f, 1f);

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.HeldItem.ModItem is not GravelDualBlades || Age >= Duration)
        {
            Projectile.Kill();
            return;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            var skill = player.GetModPlayer<WeaponPlayer>();
            if (!skill.SkillActive || skill.Skill != (int)Projectile.ai[0])
            {
                Projectile.Kill();
                return;
            }
            strength = Projectile.ai[0] == 1f
                ? player.GetModPlayer<GravelShieldPlayer>().ShieldRatio
                : MathHelper.Clamp(1f - Age / Duration, 0f, 1f);
            if (Age % 30 == 0) Projectile.netUpdate = true;
        }
        Projectile.Center = player.MountedCenter;
        Projectile.timeLeft = 2;
        ExecutorStatusVisuals.Emit(Projectile, Age, ExecutorStatusVisuals.Gravel, strength);
        Projectile.ai[1]++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1f, Math.Min(Age / 12f, (Duration - Age) / 24f));
        ExecutorStatusVisuals.Draw(Projectile.Center, Age, ExecutorStatusVisuals.Gravel, fade * strength,
            Main.player[Projectile.owner].gravDir);
        return false;
    }
}
