using System;
using ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.FaustAndMephisto;

public sealed class FaustCrossbow : ModItem
{
    private int shot;
    public override string LocalizationCategory => "FaustAndMephisto.Items";
    public override string Texture => "Terraria/Images/Item_495";
    public override void SetDefaults()
    {
        Item.width = 44; Item.height = 24; Item.damage = 34; Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = 26; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 3;
        Item.useAmmo = AmmoID.Arrow; Item.shoot = ModContent.ProjectileType<FaustPlayerBolt>(); Item.shootSpeed = 14;
        Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold: 3); Item.UseSound = SoundID.Item5;
    }
    public override Vector2? HoldoutOffset() => new Vector2(-5, 0);
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        bool strong = DuoRules.StrongShot(shot++);
        Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitX) * 14,
            ModContent.ProjectileType<FaustPlayerBolt>(), strong ? (int)(damage * 1.5f) : damage, knockback, player.whoAmI, strong ? 1 : 0);
        return false;
    }
}
public sealed class FaustPlayerBolt : ModProjectile
{
    public override string LocalizationCategory => "FaustAndMephisto.Projectiles";
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = true; Projectile.penetrate = 1; Projectile.extraUpdates = 2; Projectile.timeLeft = 240;
    }
    public override void AI()
    {
        if (Projectile.ai[0] == 1)
        {
            NPC nearest = null; float distance = 480 * 480;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float d = npc.DistanceSQ(Projectile.Center);
                if (d < distance && npc.CanBeChasedBy(Projectile) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                { nearest = npc; distance = d; }
            }
            if (nearest != null)
            {
                float heading = Projectile.velocity.ToRotation(), desired = (nearest.Center - Projectile.Center).ToRotation();
                if (Math.Abs(MathHelper.WrapAngle(desired - heading)) < .8f)
                    Projectile.velocity = heading.AngleTowards(desired, .005f).ToRotationVector2() * Projectile.velocity.Length();
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = Projectile.ai[0] == 1 ? new Color(205, 127, 255) : new Color(164, 213, 194);
        Vector2 p = Projectile.Center - Main.screenPosition;
        DuoVisuals.Line(Main.spriteBatch, p - Projectile.velocity * 2.5f, p, color * .3f, 5);
        DuoVisuals.Line(Main.spriteBatch, p - Projectile.velocity * 1.5f, p, color, 2);
        return false;
    }
    public override void OnKill(int timeLeft) => DuoVisuals.Puff(Projectile.Center, Projectile.ai[0] == 1 ? Color.MediumPurple : Color.LightGray, 4);
}

public sealed class MephistoCenser : ModItem
{
    public override string LocalizationCategory => "FaustAndMephisto.Items";
    public override string Texture => "Terraria/Images/Item_739";
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 38; Item.damage = 26; Item.DamageType = DamageClass.Magic;
        Item.mana = 10; Item.useTime = Item.useAnimation = 36; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 1.5f;
        Item.shoot = ModContent.ProjectileType<MephistoHealingDust>(); Item.shootSpeed = 6;
        Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold: 3); Item.UseSound = SoundID.Item20;
    }
}
public sealed class MephistoHealingDust : ModProjectile
{
    public override string LocalizationCategory => "FaustAndMephisto.Projectiles";
    public override string Texture => "Terraria/Images/Projectile_14";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.timeLeft = 90; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 30;
    }
    public override void OnSpawn(IEntitySource source) => Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
    public override void AI()
    {
        Vector2 center = Projectile.Center;
        int age = 90 - Projectile.timeLeft;
        Projectile.width = Projectile.height = (int)MathHelper.Lerp(20, 112, MathHelper.Clamp(age / 35f, 0, 1));
        Projectile.Center = center; Projectile.velocity *= .965f; Projectile.rotation += .012f;
        if (!Main.dedServ && Main.rand.NextBool(2))
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * .4f, Projectile.height * .4f),
                DustID.Cloud, Projectile.velocity * .15f, 150, new Color(206, 210, 202), 1).noGravity = true;
    }
    public override bool OnTileCollide(Vector2 oldVelocity) { Projectile.velocity = Vector2.Zero; return false; }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(closest, Projectile.Center) < Projectile.width * Projectile.width * .25f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            Main.player[Projectile.owner].GetModPlayer<DuoPlayer>().StealLife(target, damageDone);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Math.Min(1, Projectile.timeLeft / 25f);
        Vector2 p = Projectile.Center - Main.screenPosition;
        float radius = Projectile.width * .44f;
        // Soft overlapping vanilla dust sprites are emitted above; these broken rings mark the AoE.
        for (int i = 0; i < 3; i++)
            DuoVisuals.Ring(Main.spriteBatch, p + (Projectile.rotation + i * 2.09f).ToRotationVector2() * radius * .15f,
                radius * (.7f + .1f * i), new Color(210, 216, 205) * (.12f * opacity), 24);
        return false;
    }
}
