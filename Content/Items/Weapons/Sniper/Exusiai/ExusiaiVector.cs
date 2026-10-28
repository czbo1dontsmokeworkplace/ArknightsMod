using System;
using System.IO;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Sniper.Exusiai;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;

public class ExusiaiVector : UpgradeWeaponBase
{
    public VectorMagazine Magazine { get; private set; } = new();
    private bool pickingAmmo;
    private ulong lastTick = ulong.MaxValue;
    internal static readonly SoundStyle FireSound = new("ArknightsMod/Sounds/ExusiaiVectorA") { Volume = .48f, MaxInstances = 4 };
    internal static readonly SoundStyle SkillSound = new("ArknightsMod/Sounds/SkillActive1") { Volume = .4f, MaxInstances = 2 };
    public override void AddRecipes()
    {
        CreateRecipe().AddIngredient<global::ArknightsMod.Content.Items.Material.SugarLump>(1)
            .AddIngredient<global::ArknightsMod.Content.Items.Material.OrirockCube>(2)
            .AddIngredient(ItemID.Uzi).AddIngredient(ItemID.ChlorophyteBar, 15)
            .AddIngredient(ItemID.MusketBall, 999)
            .AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 25)
            .AddIngredient(ItemID.IllegalGunParts).AddTile(ModContent.TileType<FactoryTile>()).Register();
        Recipe.Create(ItemID.Uzi).AddIngredient(ItemID.Minishark)
            .AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 5)
            .AddIngredient(ItemID.IllegalGunParts).AddTile(TileID.Anvils).Register();
    }
    public override void SetDefaults()
    {
        Item.width = 54; Item.height = 28; Item.damage = VectorBalance.PanelDamage;
        Item.DamageType = DamageClass.Ranged; Item.knockBack = 5;
        Item.useTime = Item.useAnimation = VectorMagazine.Interval; Item.reuseDelay = 0;
        Item.shootSpeed = 16; Item.shoot = ModContent.ProjectileType<ExusiaiVectorHoldout>();
        Item.useAmmo = AmmoID.Bullet; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true; Item.autoReuse = true;
        Item.rare = ItemRarityID.Green; Item.value = Item.sellPrice(0);
        Item.accessory = true;
    }
    // Both slots grant the same cosmetic, including a hidden functional accessory.
    // No wing slot, flight, skill activation or combat modifiers are granted.
    public override void UpdateAccessory(Player player, bool hideVisual)
        => player.GetModPlayer<VectorWingPlayer>().EquippedVisual = true;
    public override void UpdateVanity(Player player)
        => player.GetModPlayer<VectorWingPlayer>().EquippedVisual = true;
    public override ModItem Clone(Item newEntity)
    {
        var copy = (ExusiaiVector)base.Clone(newEntity);
        copy.Magazine = Magazine.Copy(); copy.pickingAmmo = false; copy.lastTick = ulong.MaxValue;
        return copy;
    }
    public override bool AltFunctionUse(Player player) => false;
    // The owner-controlled holdout handles actual left input, not the skill key's synthetic click.
    public override bool CanUseItem(Player player) => false;
    public override bool CanConsumeAmmo(Item ammo, Player player) => pickingAmmo;
    public override void UpdateInventory(Player player)
    {
        if (player.whoAmI != Main.myPlayer || lastTick == Main.GameUpdateCount) return;
        lastTick = Main.GameUpdateCount;
        if (player.dead) Magazine.ClearEmpowerment();
        bool finished = Magazine.Tick();
        if (Magazine.Reloading) player.GetModPlayer<VectorReloadPlayer>().Bind(this);
        if (finished && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item149 with { Volume = .65f, Pitch = .1f }, player.Center);
    }
    public override void HoldItem(Player player)
    {
        // The cursor slot is not included in vanilla's UpdateInventory loop.
        if (player.selectedItem == 58) UpdateInventory(player);
        if (player.whoAmI != Main.myPlayer || player.dead) return;
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.Skill != 0) Magazine.ClearEmpowerment();
        if (skills.Skill == 0) skills.SkillActive = Magazine.EmpoweredShots > 0;
        bool activate = !skills.SummonMode && !skills.SkillActive && skills.StockCount > 0
            && (skills.Skill == 2 || skills.Skill == 1 && ArknightsKeybinds.SkillActivate.JustPressed
                || skills.Skill == 0 && !Magazine.Reloading);
        if (activate)
        {
            if (skills.Skill == 0) Magazine.Empower();
            skills.SkillActive = true; skills.SkillTimer = 0; skills.DelStockCount();
            SoundEngine.PlaySound(SkillSound, player.Center);
        }
        if (Magazine.Reloading) player.GetModPlayer<VectorReloadPlayer>().Bind(this);
        int held = ModContent.ProjectileType<ExusiaiVectorHoldout>();
        if (player.ownedProjectileCounts[held] == 0)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter,
                (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction), held, 0, 0,
                player.whoAmI, player.selectedItem);
    }
    internal int Mode(Player player)
    {
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.SummonMode) return 0;
        return skills.Skill == 0 ? Magazine.EmpoweredShots > 0 ? 1 : 0 : skills.Skill + 1;
    }
    internal bool PickRound(Player player, int mode, out int damage, out float knockback, out int ammo)
    {
        damage = ammo = 0; knockback = 0;
        pickingAmmo = true;
        try
        {
            if (!player.PickAmmo(Item, out _, out _, out int selectedDamage, out knockback, out ammo)) return false;
            int currentBase = player.GetWeaponDamage(Item);
            int saved = Item.damage;
            int legacyBase;
            // Reuse the real damage hooks, prefixes and player modifiers for the old 108 panel.
            // Restore even if another mod throws; the current item must keep its new panel.
            try { Item.damage = Math.Max(1, (int)MathF.Round(108f * saved / VectorBalance.PanelDamage)); legacyBase = player.GetWeaponDamage(Item); }
            finally { Item.damage = saved; }
            damage = VectorBalance.Damage(legacyBase, Math.Max(0, selectedDamage - currentBase), mode);
            return true;
        }
        finally { pickingAmmo = false; }
    }
    public override void SaveData(TagCompound tag)
    {
        base.SaveData(tag);
        tag["VectorRounds"] = Magazine.Rounds;
        tag["VectorReload"] = Magazine.ReloadTicks;
        tag["VectorEmpoweredShots"] = Magazine.EmpoweredShots;
    }
    public override void LoadData(TagCompound tag)
    {
        base.LoadData(tag);
        Magazine = new();
        int empowered = tag.ContainsKey("VectorEmpoweredShots") ? tag.GetInt("VectorEmpoweredShots")
            : tag.GetInt("VectorEmpowered") > 0 ? VectorMagazine.EmpoweredShotLimit : 0;
        Magazine.Restore(tag.ContainsKey("VectorRounds") ? tag.GetInt("VectorRounds") : VectorMagazine.Capacity,
            tag.GetInt("VectorReload"), empowered);
    }
    public override void NetSend(BinaryWriter writer)
    {
        base.NetSend(writer);
        writer.Write((byte)Magazine.Rounds); writer.Write((byte)Magazine.ReloadTicks); writer.Write((byte)Magazine.EmpoweredShots);
    }
    public override void NetReceive(BinaryReader reader)
    {
        base.NetReceive(reader);
        Magazine.Restore(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
    }
    public override string GetSkillActivateKeyHint() => Terraria.Localization.Language.GetTextValue(
        "Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
}
