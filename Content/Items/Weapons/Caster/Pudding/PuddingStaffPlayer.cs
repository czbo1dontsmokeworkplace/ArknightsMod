using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Pudding;

public sealed class PuddingStaffPlayer : ModPlayer
{
    private bool keyConsumed;

    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not PuddingStaff
            || Player.dead || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != nameof(PuddingStaff)
            || skills.Skill is < 0 or > 1 || skills.SkillActive || skills.StockCount <= 0) return;
        skills.DelStockCount();
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        SoundEngine.PlaySound(SoundID.Item122 with { Volume = .36f, Pitch = .35f }, Player.Center);
    }

    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
