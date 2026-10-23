using System;
using System.Collections.Generic;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

public sealed class DefenderElectron : ModProjectile
{
    private int struck = -1;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = 100;
    }
    public override void AI()
    {
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, .1f, .45f, .8f);
        if (!Main.dedServ && Main.rand.NextBool(3))
        {
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * .1f, 120, default, .65f);
            d.noGravity = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { struck = target.whoAmI; target.AddBuff(BuffID.Electrified, 180); }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == 1f)
            DefenderPulse.Spawn(Main.player[Projectile.owner], Projectile.Center, Projectile.damage, 125, true, struck);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Color c = new(115, 210, 255, 0);
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), c, Projectile.rotation, new Vector2(1, .5f), new Vector2(26, 3), SpriteEffects.None);
        return false;
    }
}

public sealed class DefenderPulse : ModProjectile
{
    private readonly HashSet<int> hitGroups = new();
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Grenade;
    internal static void Spawn(Player owner, Vector2 center, int damage, float radius, bool arts, int ignore = -1)
    {
        int i = Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), center, Vector2.Zero,
            ModContent.ProjectileType<DefenderPulse>(), damage, 9f, owner.whoAmI, radius, arts ? 1 : 0, ignore + 1);
        if (Main.projectile.IndexInRange(i)) Main.projectile[i].CritChance = owner.GetWeaponCrit(owner.HeldItem);
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 20;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 18;
    public override bool? CanHitNPC(NPC target)
    {
        int group = target.realLife >= 0 ? target.realLife : target.whoAmI;
        int ignored = (int)Projectile.ai[2] - 1;
        int ignoredGroup = Main.npc.IndexInRange(ignored) && Main.npc[ignored].realLife >= 0 ? Main.npc[ignored].realLife : ignored;
        return group == ignoredGroup || hitGroups.Contains(group) ? false : null;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) <= Projectile.ai[0] * Projectile.ai[0]
        && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitGroups.Add(target.realLife >= 0 ? target.realLife : target.whoAmI);
        if (Projectile.ai[1] == 1) target.AddBuff(BuffID.Electrified, 180);
    }
    public override void AI()
    {
        bool arts = Projectile.ai[1] == 1;
        Projectile.DamageType = arts ? DamageClass.Magic : DamageClass.Melee;
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = arts;
        if (Projectile.localAI[0]++ != 0 || Main.dedServ) return;
        for (int i = 0; i < 28; i++)
        {
            Vector2 v = (MathHelper.TwoPi * i / 28).ToRotationVector2();
            Dust d = Dust.NewDustPerfect(Projectile.Center + v * Projectile.ai[0] * .25f,
                arts ? DustID.Electric : DustID.Stone, v * 5f, 100, default, 1.1f);
            d.noGravity = true;
        }
        SoundEngine.PlaySound(arts ? SoundID.Item93 : SoundID.Item14, Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
