using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

public class GoldenglowBeacon : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Goldenglow/GoldenglowBeacon";
    public const int BaseMaxBeacons = 3;
    public ref float SpawnTick => ref Projectile.localAI[0];
    public static int GetMaxBeacons(Player player)
    {
        var mp = player.GetModPlayer<WeaponPlayer>();
        return BaseMaxBeacons + (mp.SkillActive ? mp.Skill == 2 ? 2 : 1 : 0);
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 24;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = int.MaxValue;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (SpawnTick == 0f) SpawnTick = Main.GameUpdateCount;
        if (Projectile.ai[1] == 0f) Projectile.ai[1] = Projectile.Center.Y;
        Projectile.Center = new Vector2(Projectile.Center.X, Projectile.ai[1] +
            MathF.Sin((float)Main.GameUpdateCount * 0.05f + Projectile.identity * 1.3f) * 4f);
        if (!Main.dedServ)
            Lighting.AddLight(Projectile.Center, 0.15f, 0.25f, 0.5f);
        if (Projectile.owner != Main.myPlayer || Projectile.ai[0]-- > 0)
            return;
        GoldenglowHeldStaff held = null;
        foreach (Projectile proj in Main.ActiveProjectiles)
            if (proj.owner == owner.whoAmI && proj.ModProjectile is GoldenglowHeldStaff staff)
            { held = staff; break; }
        if (held == null || owner.HeldItem.ModItem is not Items.Weapons.Caster.Goldenglow.GoldenglowWand)
            return;
        var mp = owner.GetModPlayer<WeaponPlayer>();
        float range = mp.SkillActive ? mp.Skill == 2 ? 1800f : 1300f : 1000f;
        Vector2 target = GoldenglowHeldStaff.FindTarget(held.Aim, owner.Center, range, 230f);
        if (Vector2.DistanceSquared(Projectile.Center, target) > 2300f * 2300f)
            return;
        int damage = (int)(owner.GetWeaponDamage(owner.HeldItem) * (held.Bursting ? 0.65f : 0.4f));
        GoldenglowLightningStrike.Spawn(Projectile.GetSource_FromThis(), Projectile.Center,
            target, owner.whoAmI, damage, 3f, 3);
        Projectile.ai[0] = (held.Bursting ? 12f : 24f) / (mp.SkillActive && mp.Skill == 0 ? 1.5f : 1f) - 1f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
        GoldenglowLightningRenderer.DrawFlare(Projectile.Center, new Color(80, 155, 255), 0.15f, 0.65f);
        return false;
    }
}
