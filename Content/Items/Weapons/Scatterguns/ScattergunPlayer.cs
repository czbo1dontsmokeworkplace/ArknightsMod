using System;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Scatterguns;

public sealed class ScattergunPlayer : ModPlayer
{
    private Item activeItem;
    private int slot = -1;
    private bool keyConsumed;
    internal int Mode
    {
        get
        {
            WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
            return !Player.dead && !Player.CCed && ReferenceEquals(activeItem, Player.HeldItem)
                && Player.HeldItem.ModItem is Scattergun gun && skills.CurrentSkill?.Key.Item == gun.Name
                && skills.Skill == slot && skills.SkillActive ? slot + 1 : 0;
        }
    }
    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.dead || Player.CCed || Player.noItems
            || Player.HeldItem.ModItem is not Scattergun gun) return;
        keyConsumed = true;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != gun.Name || skills.StockCount <= 0 || skills.SkillActive
            || skills.Skill is < 0 or > 1) return;
        activeItem = Player.HeldItem; slot = skills.Skill;
        skills.DelStockCount(); skills.SkillActive = true; skills.SkillTimer = 0;
        SoundEngine.PlaySound(SoundID.Item37 with { Volume = .65f, Pitch = .25f }, Player.Center);
    }
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
        if (activeItem != null && (!ReferenceEquals(activeItem, Player.HeldItem) || Player.dead
            || Player.GetModPlayer<WeaponPlayer>().Skill != slot))
        {
            var skills = Player.GetModPlayer<WeaponPlayer>();
            if (skills.CurrentSkill?.Key.Item == activeItem.ModItem?.Name) skills.SkillActive = false;
            activeItem = null;
        }
    }
}
