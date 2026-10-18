using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

public abstract class MedicWeapon : ExpansionWeaponBase
{
    public abstract int ProfileId { get; }
    public MedicProfile Profile => MedicCatalog.Get(ProfileId);
    public override int EliteStageMax => ProfileId == 0 ? 1 : 2;
    protected override int[] EliteDamage => [0, 0, 0];
    // Existing staff art is retained until dedicated inventory/held artwork is supplied.
    public override string Texture => Profile.Branch == MedicBranch.Physician && Profile.Tier == 5
        ? "ArknightsMod/Content/Items/Weapons/Medic/Shining/ShiningSword"
        : "ArknightsMod/Content/Items/Weapons/Medic/Sussurro/SussurroStaff";
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.useTime = Item.useAnimation = Profile.CastAnimation;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.autoReuse = true;
        Item.mana = 3 + Profile.Tier * 2;
        Item.knockBack = 0;
        Item.rare = Profile.Tier switch { 0 => ItemRarityID.Blue, 1 => ItemRarityID.Green,
            2 => ItemRarityID.LightRed, 3 => ItemRarityID.Pink, 4 => ItemRarityID.Yellow, _ => ItemRarityID.Red };
        Item.value = Item.sellPrice(silver: 30 + Profile.Tier * 130);
        Item.shootSpeed = Profile.Speed;
        Item.shoot = ModContent.ProjectileType<MedicArt>();
    }

    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        if (!ArknightsKeybinds.SkillActivatePressed(player))
            return (Profile.Branch != MedicBranch.MultiTarget || player.GetModPlayer<MedicCastPlayer>().GroupCooldown == 0)
                && base.CanUseItem(player);
        player.GetModPlayer<MedicSkillPlayer>().TryActivate(this);
        return false;
    }
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (ArknightsKeybinds.SkillActivatePressed(player))
            player.GetModPlayer<MedicSkillPlayer>().TryActivate(this);
    }

    public override float UseSpeedMultiplier(Player player) =>
        player.GetModPlayer<MedicSkillPlayer>().ActiveFor(this) && Name == "SussurroStaff"
            && player.GetModPlayer<WeaponPlayer>().Skill == 1 ? 1.35f : 1f;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        // Preserve attack-speed scaling and the full resistance interval after shortening the animation.
        int interval = player.itemAnimationMax > 0
            ? Math.Max(1, (int)MathF.Ceiling(Profile.Interval * (float)player.itemAnimationMax / Profile.CastAnimation))
            : Profile.Interval;
        if (Profile.Branch == MedicBranch.MultiTarget
            && !player.GetModPlayer<MedicCastPlayer>().TryStartGroupCast(interval)) return false;
        Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
        position = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(8), aim * 28, 16, 16, true, true);
        var skill = player.GetModPlayer<MedicSkillPlayer>();
        var skillState = player.GetModPlayer<WeaponPlayer>();
        float skillBonus = Name == "SussurroStaff" && skillState.Skill == 1
            || Name == "ShiningStaff" && skillState.Skill == 0 ? .8f : .4f;
        float power = (1f + EliteStage * .12f) * (1f + (skill.ActiveFor(this) ? skillBonus + skill.Rank * .02f : 0f));
        Projectile.NewProjectile(source, position, aim * Profile.Speed, type, 0, 0,
            player.whoAmI, ProfileId, power, interval);
        MedicArtsVfx.Cast(position, aim, ProfileId, skill.ActiveFor(this));
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "MedicalNumbers", Language.GetTextValue("Mods.ArknightsMod.MedicalArts.Numbers",
            Profile.Healing, Profile.Interval / 60f, Profile.ArmingDelay / 60f,
            Profile.Interval * Profile.ResistanceDurationMultiplier / 60f, Profile.ResistanceReduction * 100f)));
        if (Profile.Branch == MedicBranch.Chain)
            tooltips.Add(new TooltipLine(Mod, "MedicalRegen", Language.GetTextValue("Mods.ArknightsMod.MedicalArts.Regen",
                Profile.StackLimit, Profile.RegenPerStack, Profile.StackDuration / 60)));
        if (Profile.Branch == MedicBranch.Wandering)
            tooltips.Add(new TooltipLine(Mod, "MedicalCleanse", Language.GetTextValue("Mods.ArknightsMod.MedicalArts.Cleanse", Profile.CleanseLimit)));
        tooltips.Add(new TooltipLine(Mod, "MedicalSkillKey", GetSkillActivateKeyHint()));
    }
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);

    public override void AddRecipes()
    {
        if (ProfileId == 21) return;
        Recipe recipe = CreateRecipe();
        if (Profile.Parent >= 0) recipe.AddIngredient(Mod.Find<ModItem>(MedicCatalog.Get(Profile.Parent).Key).Type);
        switch (Profile.Progression)
        {
            case 0: recipe.AddRecipeGroup(RecipeGroupID.Wood, 12).AddIngredient(ItemID.Gel, 8).AddIngredient(ItemID.Daybloom, 2).AddTile(TileID.WorkBenches); break;
        }
        // Stage gates are explicit: thematic ingredients cannot accidentally open a later branch early.
        switch (Profile.Progression)
        {
            case 1:
                recipe.AddIngredient(ItemID.LesserHealingPotion, 8).AddIngredient(ItemID.JungleSpores, 6).AddTile(TileID.Anvils);
                break;
            case 2:
                recipe.AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 10).AddIngredient(ItemID.PixieDust, 12).AddTile(TileID.Anvils);
                break;
            case 3:
                recipe.AddIngredient(ItemID.HallowedBar, 10).AddIngredient(ItemID.SoulofLight, 8).AddTile(TileID.MythrilAnvil);
                break;
            case 4:
                recipe.AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.ChlorophyteBar, 6).AddTile(TileID.MythrilAnvil);
                break;
            case 5:
                recipe.AddIngredient(ItemID.Ectoplasm, 8).AddIngredient(ItemID.SpectreBar, 8).AddTile(TileID.MythrilAnvil);
                break;
            case 6:
                recipe.AddIngredient(ItemID.FragmentNebula, 10).AddIngredient(ItemID.FragmentStardust, 10).AddTile(TileID.LunarCraftingStation);
                break;
        }
        if (Profile.Progression > 0)
        {
            int stage = Profile.Progression;
            recipe.AddCondition(new Condition(Language.GetOrRegister("Mods.ArknightsMod.MedicalArts.Gate" + stage), () => stage switch
            {
                1 => NPC.downedBoss2, 2 => Main.hardMode, 3 => NPC.downedMechBossAny,
                4 => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
                5 => NPC.downedPlantBoss, _ => NPC.downedAncientCultist
            }));
        }
        recipe.Register();
    }
}

public sealed class AnselStaff : MedicWeapon { public override int ProfileId => 0; }
public sealed class MyrrhStaff : MedicWeapon { public override int ProfileId => 1; }
public sealed class SilenceStaff : MedicWeapon { public override int ProfileId => 3; }
public sealed class TuyeStaff : MedicWeapon { public override int ProfileId => 4; }
public sealed class PurestreamStaff : MedicWeapon { public override int ProfileId => 6; }
public sealed class CeylonStaff : MedicWeapon { public override int ProfileId => 7; }
public sealed class WhisperainStaff : MedicWeapon { public override int ProfileId => 8; }
public sealed class LumenStaff : MedicWeapon { public override int ProfileId => 9; }
public sealed class PerfumerStaff : MedicWeapon { public override int ProfileId => 10; }
public sealed class BreezeStaff : MedicWeapon { public override int ProfileId => 11; }
public sealed class PtilopsisStaff : MedicWeapon { public override int ProfileId => 12; }
public sealed class NightingaleStaff : MedicWeapon { public override int ProfileId => 13; }
public sealed class PapyrusStaff : MedicWeapon { public override int ProfileId => 14; }
public sealed class PaprikaStaff : MedicWeapon { public override int ProfileId => 15; }
public sealed class Mon3trCore : MedicWeapon { public override int ProfileId => 16; }
public sealed class ChestnutStaff : MedicWeapon { public override int ProfileId => 17; }
public sealed class HoneyberryStaff : MedicWeapon { public override int ProfileId => 18; }
public sealed class MulberryStaff : MedicWeapon { public override int ProfileId => 19; }
public sealed class EyjafjallaAlterStaff : MedicWeapon { public override int ProfileId => 20; }
