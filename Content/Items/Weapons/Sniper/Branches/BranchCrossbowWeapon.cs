using System;
using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

public abstract class BranchCrossbowWeapon : CrossbowWeaponBase
{
    protected abstract int[] Damages { get; }
    protected abstract int PlaceholderItem { get; }
    protected abstract int Rarity { get; }
    public override string Texture => "Terraria/Images/Item_" + PlaceholderItem;
    public override int EliteStageMax => 2;
    protected override int GetDamage(int level) => Damages[Math.Clamp(EliteStage, 0, 2)];
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base += GetDamage(Level) - Damages[0];
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "BranchSkillKey", GetSkillActivateKeyHint()));
        if (Kind != CrossbowKind.Provence)
            tooltips.Add(new TooltipLine(Mod, "BranchPlaceholder", Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Placeholder")));
    }
    public override void SetDefaults()
    {
        Item.width = 58; Item.height = 30; Item.DamageType = Terraria.ModLoader.DamageClass.Ranged;
        Item.damage = Damages[0]; Item.knockBack = CrossbowBallistics.IsPiercing(Kind) ? 6 : 3;
        Item.shootSpeed = 20; Item.rare = Rarity; Item.value = Item.sellPrice(gold: 4);
        SetCrossbowDefaults();
    }
}
