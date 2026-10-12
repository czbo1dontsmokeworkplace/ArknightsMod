using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Skyfire;

public sealed class SkyfireStaffPlayer : ModPlayer
{
    private bool keyConsumed;

    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not SkyfireStaff
            || Player.dead || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != nameof(SkyfireStaff) || skills.Skill is < 0 or > 1
            || skills.SkillActive || skills.StockCount <= 0) return;
        skills.DelStockCount();
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        SoundEngine.PlaySound(SoundID.Item20 with { Volume = .48f, Pitch = -.14f }, Player.Center);
    }

    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
