using System;
using ArknightsMod.Content.Projectiles.BossArsenal;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.BossArsenal;

public abstract class BossArsenalWeapon : ModItem
{
    private bool extraHarbingerShot;
    public abstract int Kind { get; }
    internal static readonly string[] Names = { "HarbingerGuillotine", "CatastropheSalvo", "ScarletMeson", "UrsusArtilleryLink",
        "WinterScar", "YetiSnowGrenade", "BlackIceCoffin", "YetiChime", "BasaltBreaker", "CalderaBellows", "CinderTestament", "BasaltBrood" };
    internal static readonly int[] Art = { ItemID.BreakerBlade, ItemID.RocketLauncher, ItemID.LaserMachinegun, ItemID.SnowmanCannon,
        ItemID.IceBlade, ItemID.Grenade, ItemID.WaterBolt, ItemID.BlizzardStaff, ItemID.FieryGreatsword, ItemID.Flamethrower, ItemID.BookofSkulls, ItemID.SlimeStaff };
    internal static readonly int[] Damage = { 82, 66, 105, 48, 48, 133, 78, 34, 26, 15, 37, 15 };
    public override string Texture => "Terraria/Images/Item_" + Art[Kind];
    public override void SetDefaults()
    {
        int role = Kind % 4;
        Item.width = Item.height = 40; Item.damage = Damage[Kind]; Item.knockBack = role == 0 ? 6 : 3;
        Item.DamageType = role switch { 0 => DamageClass.Melee, 1 => DamageClass.Ranged, 2 => DamageClass.Magic, _ => DamageClass.Summon };
        Item.useStyle = role == 0 ? ItemUseStyleID.Swing : ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Kind switch { 1 => 72, 5 => 32, 9 => 6, 6 => 55, _ => 28 };
        Item.noMelee = role != 0; Item.autoReuse = true; Item.useTurn = role == 0;
        Item.mana = role == 2 ? Kind == 6 ? 12 : 5 : role == 3 ? 10 : 0;
        Item.rare = Kind < 4 ? ItemRarityID.Pink : Kind < 8 ? ItemRarityID.LightRed : ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: Kind < 8 ? 4 : 2);
        Item.shoot = ModContent.ProjectileType<BossArsenalShot>(); Item.shootSpeed = 14;
        if (Kind == 1) Item.useTime = Item.useAnimation = 48; // 72 / 1.5: 50% more attacks per second.
        if (Kind == 4) Item.useTime = Item.useAnimation = 38; // Half the previous 19-tick attack rate.
        if (Kind == 8) Item.useTime = Item.useAnimation = (int)MathF.Round(28 / .85f);
        if (Kind == 9) {
            var vanilla = new Item(ItemID.Flamethrower);
            Item.useTime = vanilla.useTime; Item.useAnimation = vanilla.useAnimation; Item.reuseDelay = vanilla.reuseDelay;
            Item.shootSpeed = vanilla.shootSpeed; Item.useAmmo = vanilla.useAmmo;
            Item.consumeAmmoOnFirstShotOnly = vanilla.consumeAmmoOnFirstShotOnly;
            Item.consumeAmmoOnLastShotOnly = vanilla.consumeAmmoOnLastShotOnly;
            Item.shoot = ModContent.ProjectileType<CalderaFlame>();
        }
        if (Kind == 1) Item.useAmmo = AmmoID.Rocket;
        if (Kind is 2 or 10) { Item.channel = Item.noUseGraphic = true; Item.shoot = ModContent.ProjectileType<BossArsenalHoldout>(); Item.useTime = Item.useAnimation = 20; }
        if (role == 3) { Item.shoot = ModContent.ProjectileType<BossArsenalMinion>(); Item.buffType = BossArsenalMinionBuff.ForKind(Kind); }
        Item.UseSound = role switch { 0 => SoundID.Item1, 1 => Kind == 9 ? SoundID.Item34 : SoundID.Item11, 2 => SoundID.Item8, _ => SoundID.Item44 };
        if (Kind == 5) { var grenade = new Item(ItemID.Grenade); Item.UseSound = grenade.UseSound; Item.noUseGraphic = true; }
    }
    public override bool AltFunctionUse(Player player) => Kind % 4 == 3;
    public override bool CanUseItem(Player player)
    {
        if (Kind % 4 == 3 && player.altFunctionUse == 2)
        { player.MinionNPCTargetAim(false); return false; }
        return Kind is not (2 or 10) || player.ownedProjectileCounts[ModContent.ProjectileType<BossArsenalHoldout>()] == 0;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        if (Kind == 0) {
            int rainCount = extraHarbingerShot ? 7 : 6;
            extraHarbingerShot = !extraHarbingerShot;
            Spawn(player.MountedCenter + aim * 25, aim * 20, ModContent.ProjectileType<HarbingerRain>(), Main.MouseWorld.X, Main.MouseWorld.Y);
            for (int i = 0; i < rainCount; i++) {
                Vector2 from = new(Main.MouseWorld.X + Main.rand.NextFloat(-180, 180), player.Center.Y - 480 - i * 64);
                Vector2 direction = (Main.MouseWorld - from).SafeNormalize(Vector2.UnitY);
                Spawn(from, direction * Main.rand.NextFloat(18, 23), ModContent.ProjectileType<HarbingerRain>(), Main.MouseWorld.X, Main.MouseWorld.Y);
            }
        }
        else if (Kind == 4) {
            Spawn(player.MountedCenter + aim * 28, aim * 12, ModContent.ProjectileType<WinterScarBeam>(), 0);
        }
        else if (Kind is 2 or 10) Spawn(player.MountedCenter, aim, type, Kind);
        else if (Kind % 4 == 3)
        {
            if (Kind is 3 or 7) {
                foreach (Projectile old in Main.ActiveProjectiles)
                    if (old.owner == player.whoAmI && old.minion && old.minionSlots > 0) old.Kill();
            }
            player.AddBuff(Item.buffType, 2);
            var q = Spawn(Main.MouseWorld, Vector2.Zero, type, Kind); q.originalDamage = Item.damage;
            if (Kind is 3 or 7) {
                q.ai[2] = Item.damage;
                BossArsenalMinion.RefreshCrown(q, player);
                q.netUpdate = true;
            }
        }
        else if (Kind == 6)
        {
            // Lock the three nearest distinct enemies; their crystals follow until launch.
            var targets = new System.Collections.Generic.List<NPC>();
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy(player) && Vector2.DistanceSquared(player.Center, npc.Center) <= 1200 * 1200)
                    targets.Add(npc);
            targets.Sort((a, b) => Vector2.DistanceSquared(player.Center, a.Center).CompareTo(Vector2.DistanceSquared(player.Center, b.Center)));
            var roots = new System.Collections.Generic.HashSet<int>();
            var distinct = new System.Collections.Generic.List<NPC>();
            foreach (NPC target in targets) {
                if (!roots.Add(target.realLife >= 0 ? target.realLife : target.whoAmI)) continue;
                distinct.Add(target); if (distinct.Count == 3) break;
            }
            if (distinct.Count > 0) for (int i = 0; i < 3; i++) {
                NPC target = distinct[i % distinct.Count];
                float lane = distinct.Count == 1 ? i + 1 : distinct.Count == 2 && i != 1 ? (i == 0 ? 1 : 3) : 0;
                var ice = Spawn(target.Top - Vector2.UnitY * 160, Vector2.Zero, ModContent.ProjectileType<BlackIceJudgement>(), target.whoAmI);
                ice.ai[2] = lane; ice.netUpdate = true;
            }
        }
        else if (Kind == 1)
            Spawn(player.MountedCenter + aim * 30, aim * Main.rand.NextFloat(7, 9) * 1.35f,
                ModContent.ProjectileType<BossArsenalShot>(), Kind);
        else if (Kind == 5) Spawn(player.MountedCenter, aim * 18 - Vector2.UnitY * 2, type, Kind);
        else if (Kind == 8) {
            for (int i = 0; i < 2; i++)
                Spawn(position + aim * 28, aim.RotatedBy((i * 2 - 1) * .13f) * Main.rand.NextFloat(9, 12) - Vector2.UnitY * 2,
                    ModContent.ProjectileType<BasaltRollingMeteor>(), 0);
        }
        else if (Kind == 9) Spawn(position, velocity, ModContent.ProjectileType<CalderaFlame>(), 0);

        return false;
        Projectile Spawn(Vector2 at, Vector2 speed, int shot, float kind, float b = 0)
        {
            var p = Projectile.NewProjectileDirect(source, at, speed, shot, damage, knockback, player.whoAmI, kind, b);
            p.CritChance = player.GetWeaponCrit(Item); return p;
        }
    }
    public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind % 4 != 0 || player.whoAmI != Main.myPlayer) return;
        if (Kind == 0) {
            HarbingerEmber.Seed(player.GetSource_OnHit(target), target.Center, target.whoAmI, damageDone / 2, player.whoAmI, 2);
            return;
        }
        if (Kind == 8) {
            Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ProjectileID.Volcano, Math.Max(1, damageDone / 3), 4, player.whoAmI);
            target.AddBuff(BuffID.OnFire, 180);
            return;
        }
        if (Kind == 4) target.AddBuff(BuffID.Frostburn2, 180);
        if (Kind == 8) target.AddBuff(BuffID.OnFire, 180);
        var q = Projectile.NewProjectileDirect(player.GetSource_OnHit(target), target.Center, Vector2.Zero,
            ModContent.ProjectileType<BossArsenalBurst>(), Math.Max(1, damageDone / 3), 0, player.whoAmI, Kind, Kind == 0 ? 96 : 72);
        q.DamageType = DamageClass.Melee;
        if (Kind == 4) q.ai[2] = (target.Center - player.Center).ToRotation();
    }
}

public sealed class HarbingerGuillotine : BossArsenalWeapon { public override int Kind => 0; }
public sealed class CatastropheSalvo : BossArsenalWeapon { public override int Kind => 1; }
public sealed class ScarletMeson : BossArsenalWeapon { public override int Kind => 2; }
public sealed class UrsusArtilleryLink : BossArsenalWeapon { public override int Kind => 3; }
public sealed class WinterScar : BossArsenalWeapon { public override int Kind => 4; }
public sealed class YetiSnowGrenade : BossArsenalWeapon { public override int Kind => 5; }
public sealed class BlackIceCoffin : BossArsenalWeapon { public override int Kind => 6; }
public sealed class YetiChime : BossArsenalWeapon { public override int Kind => 7; }
public sealed class BasaltBreaker : BossArsenalWeapon { public override int Kind => 8; }
public sealed class CalderaBellows : BossArsenalWeapon { public override int Kind => 9; }
public sealed class CinderTestament : BossArsenalWeapon { public override int Kind => 10; }
public sealed class BasaltBrood : BossArsenalWeapon { public override int Kind => 11; }
