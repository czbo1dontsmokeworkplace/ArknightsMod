using System;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

public sealed class AutoMedicalPlayer : ModPlayer
{
    internal bool Equipped;
    internal int Cooldown => Player.GetModPlayer<MedicCastPlayer>().Cooldown;
    public override void ResetEffects()=>Equipped=false;
    public override void PostUpdate() => TryUseHotbarMedic();
    internal bool TryUseHotbarMedic()
    {
        if(!Equipped || Cooldown>0 || Player.whoAmI!=Main.myPlayer || !Player.active
            || Player.dead || Player.CCed || Player.noItems || Player.silence
            || Player.statLife>=Player.statLifeMax2)return false;
        // One background use, from the first available medical item. Nothing here
        // selects an inventory slot or starts/changes the held weapon's animation.
        for(int slot=0;slot<Math.Min(10,Player.inventory.Length);slot++)
            if(Player.inventory[slot].ModItem is MedicWeapon weapon && weapon.TryAutomaticCast(Player))return true;
        return false;
    }
}
