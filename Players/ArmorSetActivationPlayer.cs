using ArknightsMod.Content;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

/// <summary>主动套装效果统一调度：只识别盔甲栏中的完整套装。</summary>
public sealed class ArmorSetActivationPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!ArknightsKeybinds.ArmorSetActivatePressed(Player)) return;
        NeoArmorReforgeVanityItem head = NeoArmorReforgeSetLoader.GetVanity(Player.armor[0].type);
        if (head is not NeoArmorReforgeVanityHead
            || !NeoArmorReforgeSetLoader.IsFullSetEquipped(Player, head)) return;
        head.SetProfile?.OnSetActivate?.Invoke(Player);
    }
}
