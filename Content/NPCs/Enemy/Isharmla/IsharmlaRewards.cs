using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public static class IsharmlaRewards
{
    public static int[] WeaponTypes => new[] { ModContent.ItemType<AbyssalSpine>(), ModContent.ItemType<HeavensDeluge>(),
        ModContent.ItemType<FinalTideFlute>(), ModContent.ItemType<TidebornStaff>() };
}

public abstract class TideItem : ModItem
{
    public override string Texture => Isharmla.Root + IconArt;
    protected virtual string IconArt => "Head";
    protected virtual Rectangle? IconCrop => null;
    protected virtual float IconRotation => 0;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override bool PreDrawInInventory(SpriteBatch batch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D art = IsharmlaVisuals.Art(IconArt); Rectangle area = IconCrop ?? art.Bounds;
        batch.Draw(art, position, area, Color.White, IconRotation, area.Size() * .5f,
            42f / System.Math.Max(area.Width, area.Height), SpriteEffects.None, 0); return false;
    }
    public override bool PreDrawInWorld(SpriteBatch batch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D art = IsharmlaVisuals.Art(IconArt); Rectangle area = IconCrop ?? art.Bounds;
        batch.Draw(art, Item.Center - Main.screenPosition, area, lightColor, IconRotation + rotation, area.Size() * .5f,
            38f / System.Math.Max(area.Width, area.Height), SpriteEffects.None, 0); return false;
    }
    protected void Weapon(int damage, DamageClass damageClass, int ticks)
    {
        Item.width = Item.height = 40; Item.damage = damage; Item.DamageType = damageClass;
        Item.useTime = Item.useAnimation = ticks; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 4;
        Item.rare = ItemRarityID.Red; Item.value = Item.sellPrice(gold: 14);
    }
}

public sealed class CallOfTheTides : TideItem
{
    protected override string IconArt => "Fin";
    public override void SetStaticDefaults() { base.SetStaticDefaults(); ItemID.Sets.SortingPriorityBossSpawns[Type] = 21; }
    public override void SetDefaults()
    {
        Item.width = 32; Item.height = 26; Item.maxStack = 1; Item.rare = ItemRarityID.Red;
        Item.useTime = Item.useAnimation = 45; Item.useStyle = ItemUseStyleID.HoldUp; Item.consumable = false;
    }
    public override bool CanUseItem(Player player) => NPC.downedMoonlord && player.ZoneBeach && !NPC.AnyNPCs(ModContent.NPCType<Isharmla>());
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return true;
        SoundEngine.PlaySound(SoundID.Item29, player.Center);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<Isharmla>());
        else NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Isharmla>());
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Coral, 15).AddIngredient(ItemID.Seashell, 5)
        .AddIngredient(ItemID.FragmentNebula, 5).AddIngredient(ItemID.FragmentStardust, 5)
        .AddTile(TileID.LunarCraftingStation).AddCondition(Condition.DownedMoonLord).Register();
}

public sealed class AbyssalSpine : TideItem
{
    public override void SetDefaults()
    {
        Weapon(245, DamageClass.Melee, 28); Item.channel = true; Item.noUseGraphic = true;
        Item.knockBack = 8; Item.shoot = ModContent.ProjectileType<TideSpine>(); Item.shootSpeed = 18;
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
}

public sealed class HeavensDeluge : TideItem
{
    public override void SetDefaults()
    {
        Weapon(190, DamageClass.Ranged, 24); Item.useAmmo = AmmoID.Gel; Item.shootSpeed = 23; Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<TideBolt>(); Item.UseSound = SoundID.Item85;
    }
    public override Vector2? HoldoutOffset() => new Vector2(-8, 0);
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter + velocity.SafeNormalize(Vector2.UnitX) * 48, velocity,
                ModContent.ProjectileType<TideBolt>(), damage, knockback, player.whoAmI);
        return false;
    }
}

public sealed class FinalTideFlute : TideItem
{
    protected override string IconArt => "Fin";
    protected override float IconRotation => -.45f;
    public override void SetDefaults()
    {
        Weapon(135, DamageClass.Magic, 25); Item.mana = 15; Item.shootSpeed = 15; Item.UseSound = SoundID.Item26; Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<TideBolt>();
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            for (int i = 0; i < 3; i++) Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 1, i * MathHelper.TwoPi / 3);
        return false;
    }
}

public sealed class TidebornStaff : TideItem
{
    protected override string IconArt => "Human";
    protected override Rectangle? IconCrop => new Rectangle(32, 4, 20, 48);
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults(); ItemID.Sets.StaffMinionSlotsRequired[Type] = 1;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true; ItemID.Sets.LockOnIgnoresCollision[Type] = true;
    }
    public override void SetDefaults()
    {
        Weapon(105, DamageClass.Summon, 30); Item.mana = 10; Item.useStyle = ItemUseStyleID.Swing; Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<TidebornMinion>(); Item.buffType = ModContent.BuffType<TidebornBuff>(); Item.shootSpeed = 1;
        Item.UseSound = SoundID.Item44;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 18000);
        if (player.whoAmI == Main.myPlayer)
        {
            int slot = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (slot < Main.maxProjectiles) Main.projectile[slot].originalDamage = Item.damage;
        }
        return false;
    }
}

public sealed class HeartOfTheTides : TideItem
{
    protected override string IconArt => "Body";
    protected override Rectangle? IconCrop => new Rectangle(80, 110, 110, 110);
    public override void SetDefaults()
    { Item.width = Item.height = 30; Item.accessory = true; Item.rare = ItemRarityID.Red; Item.value = Item.sellPrice(gold: 12); }
    public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<IsharmlaPlayer>().HeartEquipped = true;
}

public sealed class IsharmlaTreasureBag : TideItem
{
    protected override string IconArt => "TailBase";
    public override void SetStaticDefaults() { Item.ResearchUnlockCount = 3; ItemID.Sets.BossBag[Type] = true; }
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.maxStack = Item.CommonMaxStack; Item.consumable = true;
        Item.expert = true; Item.rare = ItemRarityID.Expert;
    }
    public override bool CanRightClick() => true;
    public override void ModifyItemLoot(ItemLoot loot)
    {
        loot.Add(ItemDropRule.OneFromOptions(1, IsharmlaRewards.WeaponTypes));
        loot.Add(ItemDropRule.Common(ModContent.ItemType<HeartOfTheTides>()));
        loot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Isharmla>()));
    }
}
