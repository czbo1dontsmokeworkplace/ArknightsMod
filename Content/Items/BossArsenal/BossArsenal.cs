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
    public abstract int Kind { get; }
    internal static readonly string[] Names = { "HarbingerGuillotine", "CatastropheSalvo", "ScarletMeson", "UrsusArtilleryLink",
        "WinterScar", "YetiSnowGrenade", "BlackIceCoffin", "YetiChime", "BasaltBreaker", "CalderaBellows", "CinderTestament", "BasaltBrood" };
    internal static readonly int[] Art = { ItemID.BreakerBlade, ItemID.RocketLauncher, ItemID.LaserMachinegun, ItemID.DeadlySphereStaff,
        ItemID.IceBlade, ItemID.Grenade, ItemID.WaterBolt, ItemID.BlizzardStaff, ItemID.FieryGreatsword, ItemID.Flamethrower, ItemID.BookofSkulls, ItemID.SlimeStaff };
    internal static readonly int[] Damage = { 82, 66, 42, 48, 48, 58, 78, 34, 26, 15, 22, 15 };
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
        if (Kind == 4) Item.useTime = Item.useAnimation = (int)MathF.Round(28 / 2.2f);
        if (Kind == 9) Item.useAmmo = AmmoID.Gel;
        if (Kind == 1) Item.useAmmo = AmmoID.Rocket;
        if (Kind is 2 or 10) { Item.channel = Item.noUseGraphic = true; Item.shoot = ModContent.ProjectileType<BossArsenalHoldout>(); Item.useTime = Item.useAnimation = 20; }
        if (role == 3) { Item.shoot = ModContent.ProjectileType<BossArsenalMinion>(); Item.buffType = BossArsenalMinionBuff.ForKind(Kind); }
        Item.UseSound = role switch { 0 => SoundID.Item1, 1 => Kind == 9 ? SoundID.Item34 : SoundID.Item11, 2 => SoundID.Item8, _ => SoundID.Item44 };
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
        if (Kind == 0) Spawn(player.MountedCenter + aim * 25, aim * 15, ModContent.ProjectileType<HarbingerSickle>(), 0);
        else if (Kind == 4) {
            int count = Main.rand.Next(1, 3);
            Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < count; i++) Spawn(player.MountedCenter + aim * 28 + normal * ((i - (count - 1) * .5f) * 24),
                aim * 4, ModContent.ProjectileType<WinterScarBeam>(), 0);
        }
        else if (Kind is 2 or 10) Spawn(player.MountedCenter, aim, type, Kind);
        else if (Kind % 4 == 3)
        {
            if (Kind == 7) {
                foreach (Projectile old in Main.ActiveProjectiles)
                    if (old.owner == player.whoAmI && old.minion && old.minionSlots > 0) old.Kill();
            }
            player.AddBuff(Item.buffType, 2);
            var q = Spawn(Main.MouseWorld, Vector2.Zero, type, Kind); q.originalDamage = Item.damage;
            if (Kind == 7) {
                q.ai[2] = Item.damage;
                BossArsenalMinion.RefreshCrown(q, player);
                q.netUpdate = true;
            }
        }
        else if (Kind == 6)
        {
            Vector2 target = player.MountedCenter + (Main.MouseWorld - player.MountedCenter).SafeNormalize(aim)
                * Math.Min(900, Vector2.Distance(player.MountedCenter, Main.MouseWorld));
            Spawn(target - Vector2.UnitY * 400, Vector2.Zero, type, Kind, target.Y);
        }
        else if (Kind == 1)
            for (int i = 0; i < 2; i++) {
                Vector2 side = aim.RotatedBy(MathHelper.PiOver2) * ((i * 2 - 1) * 7);
                Spawn(player.MountedCenter + aim * 30 + side, aim * Main.rand.NextFloat(7, 9) * 1.35f,
                    ModContent.ProjectileType<BossArsenalShot>(), Kind, i);
            }
        else if (Kind == 5) Spawn(player.MountedCenter, aim * 18 - Vector2.UnitY * 2, type, Kind);
        else if (Kind == 9) Spawn(player.MountedCenter + aim * 34, aim.RotatedByRandom(.045f) * Main.rand.NextFloat(8, 11), ModContent.ProjectileType<BossArsenalShot>(), Kind);

        return false;
        Projectile Spawn(Vector2 at, Vector2 speed, int shot, int kind, float b = 0)
        {
            var p = Projectile.NewProjectileDirect(source, at, speed, shot, damage, knockback, player.whoAmI, kind, b);
            p.CritChance = player.GetWeaponCrit(Item); return p;
        }
    }
    public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind % 4 != 0 || player.whoAmI != Main.myPlayer) return;
        if (Kind == 8) {
            Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ProjectileID.Volcano, Math.Max(1, damageDone / 3), 4, player.whoAmI);
            int count = Main.rand.Next(2, 5);
            for (int i = 0; i < count; i++) Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center,
                (-Vector2.UnitY).RotatedBy(MathHelper.TwoPi * i / count + Main.rand.NextFloat(-.3f, .3f)) * Main.rand.NextFloat(5, 10),
                ModContent.ProjectileType<BasaltSpark>(), Math.Max(1, damageDone / 4), 1, player.whoAmI);
            return;
        }
        if (Kind == 4) target.AddBuff(BuffID.Frostburn2, 180);
        if (Kind == 8) target.AddBuff(BuffID.OnFire, 180);
        var q = Projectile.NewProjectileDirect(player.GetSource_OnHit(target), target.Center, Vector2.Zero,
            ModContent.ProjectileType<BossArsenalBurst>(), Math.Max(1, damageDone / 3), 0, player.whoAmI, Kind, Kind == 0 ? 96 : 72);
        q.DamageType = DamageClass.Melee;
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
