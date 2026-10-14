using ArknightsMod.Content.Projectiles.Caster.Mostima;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Mostima;

public sealed class MostimaStaffPlayer : ModPlayer
{
    private bool keyConsumed;
    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not MostimaStaff
            || Player.dead || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        WeaponPlayer s = Player.GetModPlayer<WeaponPlayer>();
        if (s.CurrentSkill?.Key.Item != nameof(MostimaStaff) || s.Skill is < 0 or > 2
            || s.SkillActive || s.StockCount <= 0) return;
        s.DelStockCount();
        s.SkillActive = true;
        s.SkillTimer = 0;
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .6f, Pitch = -.35f }, Player.Center);
    }
    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
