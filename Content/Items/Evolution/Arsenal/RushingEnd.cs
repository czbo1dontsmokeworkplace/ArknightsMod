using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.NPCs.Enemy.Evolution;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

internal static class RushingEndRules
{
    internal const int Life = 84, HitInterval = 3, BurstCount = 3, UseTicks = 3, AnimationTicks = 15;
    internal const float Spread = (.38f + .055f) * .5f, Drag = .975f;
    internal static float Radius(float age) => MathHelper.Lerp(23 * .7f, 112 * .7f, MathHelper.SmoothStep(0, 1, Math.Clamp(age / 42, 0, 1)));
    internal static float Opacity(float age, int timeLeft) => Math.Clamp(age / 5, 0, 1) * Math.Clamp(timeLeft / 24f, 0, 1);
    internal static float ShotAngle(int index, float sample) => Math.Clamp(sample, -1, 1) * Spread;
}

// The prototype deliberately uses vanilla Flamethrower artwork.
public sealed class RushingEnd : ModItem
{
    public override string LocalizationCategory => "Items.EvolutionArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.Flamethrower}";
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 50; Item.height = 24; Item.damage = 42; Item.DamageType = DamageClass.Ranged;
        Item.useTime = RushingEndRules.UseTicks; Item.useAnimation = RushingEndRules.AnimationTicks;
        Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = true; Item.autoReuse = true;
        Item.useAmmo = AmmoID.Gel; Item.consumeAmmoOnFirstShotOnly = true;
        Item.shoot = ModContent.ProjectileType<EndPowder>(); Item.shootSpeed = 25;
        Item.knockBack = .7f; Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 7);
    }
    public override Vector2? HoldoutOffset() => new Vector2(-8, 0);
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        Vector2 muzzle = position;
        if (Collision.CanHitLine(position, 1, 1, position + direction * 36, 1, 1)) muzzle += direction * 36;
        for (int i = 0; i < RushingEndRules.BurstCount; i++)
        {
            Vector2 spray = direction.RotatedBy(RushingEndRules.ShotAngle(i, Main.rand.NextFloat(-1, 1))) * Main.rand.NextFloat(22, 28);
            Projectile.NewProjectile(source, muzzle, spray, ModContent.ProjectileType<EndPowder>(), damage, knockback,
                player.whoAmI, 0, Main.rand.Next(1, 10000));
        }
        if (!Main.dedServ && player.itemAnimation >= Item.useAnimation - 1)
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = .35f, Pitch = -.35f, MaxInstances = 2 }, muzzle);
        return false;
    }
}

public sealed class EndPowder : EvolutionArsenalProjectile
{
    internal float Age => Projectile.ai[0];
    internal float Radius => RushingEndRules.Radius(Age);
    internal float Opacity => RushingEndRules.Opacity(Age, Projectile.timeLeft);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.timeLeft = RushingEndRules.Life;
        Projectile.DamageType = DamageClass.Ranged; Projectile.friendly = true; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.ArmorPenetration = 12;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = RushingEndRules.HitInterval;
    }
    public override bool? CanDamage() => Age >= 3 && Projectile.timeLeft > 12;
    public override bool? CanHitNPC(NPC target) => target.GetGlobalNPC<RushingEndTarget>().Ready(Projectile.owner, Main.GameUpdateCount);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(Projectile.Center, nearest) <= Radius * Radius;
    }
    public override void AI()
    {
        Projectile.ai[0]++;
        Projectile.velocity *= RushingEndRules.Drag;
        Projectile.rotation = Projectile.ai[1] * .17f + Age * (.012f + Projectile.ai[1] % 3 * .003f);
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, new Vector3(.13f, .005f, .018f) * Opacity);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => target.GetGlobalNPC<RushingEndTarget>().Record(Projectile.owner, Main.GameUpdateCount);
    public override bool PreDraw(ref Color lightColor)
    {
        RushingEndVisuals.Details(this);
        return false;
    }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.Powder(Projectile.Center, Radius * 2, Opacity * .9f,
        Projectile.ai[1], Projectile.rotation);
}

// Shared by overlapping clouds from one player; different players keep their own high-frequency streams.
public sealed class RushingEndTarget : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private ulong[] nextHit;
    public override void SetDefaults(NPC entity) => nextHit = null;
    internal bool Ready(int owner, ulong tick) => owner >= 0 && owner < Main.maxPlayers && (nextHit == null || tick >= nextHit[owner]);
    internal void Record(int owner, ulong tick)
    {
        if (owner < 0 || owner >= Main.maxPlayers) return;
        nextHit ??= new ulong[Main.maxPlayers];
        nextHit[owner] = tick + RushingEndRules.HitInterval;
    }
}

internal static class RushingEndVisuals
{
    internal static void Details(EndPowder cloud)
    {
        float opacity = cloud.Opacity;
        Vector2 center = cloud.Projectile.Center;
        if (opacity <= .001f) return;
        // A handful of deterministic crystalline motes per cloud: no secondary projectiles or particle flood.
        for (int i = 0; i < 4; i++)
        {
            float phase = cloud.Projectile.ai[1] * .71f + i * 2.4f;
            Vector2 offset = (phase + cloud.Age * .014f).ToRotationVector2() * cloud.Radius * (.28f + i * .13f);
            Vector2 axis = (phase - cloud.Age * .025f).ToRotationVector2();
            float glint = (.35f + .65f * MathF.Pow(Math.Max(0, MathF.Sin(phase + cloud.Age * .18f)), 6)) * opacity;
            Vector2 point = center + offset;
            EvolutionVisuals.Line(point - axis * 3, point + axis * 3,
                new Color(255, 159, 185) * (glint * .55f), 1.1f, false);
            EvolutionVisuals.Line(point - axis.RotatedBy(MathHelper.PiOver2) * 2, point + axis.RotatedBy(MathHelper.PiOver2) * 2,
                EvolutionWeaponVisuals.Blood * (glint * .5f), 1, false);
        }
        if (!EvolutionWeaponRenderer.Ready)
        {
            TextureFallback(center, cloud.Radius, opacity, cloud.Projectile.rotation);
        }
    }
    private static void TextureFallback(Vector2 center, float radius, float opacity, float rotation)
    {
        // The established grayscale smoke texture is only a fallback; primary billows use our own material.
        var texture = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("ArknightsMod/Assets/GrayScaleTexture/Smoke2").Value;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, new Color(130, 12, 35) * (opacity * .18f),
            rotation, texture.Size() * .5f, new Vector2(radius * 2) / texture.Size(), Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
    }
}
