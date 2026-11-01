using ArknightsMod.Content.Items.Armor.Specialist.Mortis;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets;

public sealed class MortisSubstituteBuff : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.ShadowDodge}";
    public override void SetStaticDefaults() {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex) {
        int remaining = player.GetModPlayer<MortisSetPlayer>().SubstituteTime;
        if (remaining > 0) player.buffTime[buffIndex] = remaining;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed class NecrosisAfflictionBuff : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.ShadowFlame}";
    public override void SetStaticDefaults() {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = Main.buffNoTimeDisplay[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
        var state = Main.LocalPlayer.GetModPlayer<NecrosisPlayer>();
        tip = state.IsBursting
            ? Language.GetTextValue("Mods.ArknightsMod.Mortis.NecrosisBurst", state.BurstTime / 60f)
            : Language.GetTextValue("Mods.ArknightsMod.Mortis.NecrosisAccumulation", state.Accumulation);
    }
}
