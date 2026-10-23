using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class FighterDispatchTerminal : ModItem
{
    public override string LocalizationCategory => "Items.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.XenoStaff}";
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40; Item.damage = 36; Item.DamageType = DamageClass.Summon;
        Item.mana = 10; Item.useTime = Item.useAnimation = 30; Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 2;
        Item.rare = ItemRarityID.Lime; Item.value = Item.sellPrice(gold: 8); Item.UseSound = SoundID.Item44;
        Item.shoot = ModContent.ProjectileType<DispatchDrone>();
        Item.buffType = ModContent.BuffType<DispatchDroneBuff>(); Item.shootSpeed = 10;
        // Casting never takes ammunition. Each real drone shot selects and consumes it separately.
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 18000);
        Vector2 spawn = MaterialistArsenalRules.ClampAim(Main.MouseWorld, player, 700);
        int index = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}

public sealed class MiniGatling : ModItem
{
    private bool pickingAmmo;
    public override string LocalizationCategory => "Items.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.ChainGun}";
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 56; Item.height = 24; Item.damage = 27; Item.DamageType = DamageClass.Ranged;
        Item.ArmorPenetration = 20;
        Item.useTime = Item.useAnimation = 2; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true; Item.channel = true; Item.autoReuse = true;
        Item.useAmmo = AmmoID.Bullet; Item.shootSpeed = 15; Item.knockBack = 1.5f;
        Item.rare = ItemRarityID.Lime; Item.value = Item.sellPrice(gold: 8);
        Item.shoot = ModContent.ProjectileType<MiniGatlingHoldout>();
    }
    public override ModItem Clone(Item newEntity)
    {
        var result = (MiniGatling)base.Clone(newEntity); result.pickingAmmo = false; return result;
    }
    public override bool CanConsumeAmmo(Item ammo, Player player) => pickingAmmo;
    internal bool PickRound(Player player, out int damage, out float knockback)
    {
        pickingAmmo = true;
        try { return player.PickAmmo(Item, out _, out _, out damage, out knockback, out _); }
        finally { pickingAmmo = false; }
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter,
                (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction),
                ModContent.ProjectileType<MiniGatlingHoldout>(), 0, 0, player.whoAmI);
        return false;
    }
}

public sealed class OrbitalLaser : ModItem
{
    public override string LocalizationCategory => "Items.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.MeteorStaff}";
    public override void SetStaticDefaults() { Item.ResearchUnlockCount = 1; Item.staff[Type] = true; }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40; Item.damage = 106; Item.DamageType = DamageClass.Magic;
        Item.mana = 14; Item.useTime = Item.useAnimation = 25; Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true; Item.noMelee = true; Item.knockBack = 3;
        Item.rare = ItemRarityID.Lime; Item.value = Item.sellPrice(gold: 8); Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<OrbitalBurstMarker>();
    }
    public override bool CanUseItem(Player player) => Main.GameUpdateCount >= player.GetModPlayer<MaterialistArsenalPlayer>().NextOrbitalTick;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.GetModPlayer<MaterialistArsenalPlayer>().NextOrbitalTick = Main.GameUpdateCount + 25;
        Vector2 mark = MaterialistArsenalRules.ClampAim(Main.MouseWorld, player, 1200);
        Projectile.NewProjectile(source, mark, Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }
}

public sealed class MaterialistArsenalPlayer : ModPlayer
{
    internal ulong NextOrbitalTick;
    public override void OnEnterWorld() => NextOrbitalTick = 0;
}

public sealed class RotorBladeCraft : ModItem
{
    public override string LocalizationCategory => "Items.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.LightDisc}";
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = Item.height = 36; Item.damage = 36; Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = 24; Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = Item.noUseGraphic = true; Item.autoReuse = true;
        Item.knockBack = 4; Item.rare = ItemRarityID.Lime; Item.value = Item.sellPrice(gold: 8);
        Item.shoot = ModContent.ProjectileType<RotorBladeProjectile>(); Item.shootSpeed = 16;
        Item.UseSound = SoundID.Item1;
    }
}
