using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

// Block the native switching/drop/UI paths before they move the bound item.
// Lifecycle and damage suppression remain owned by the synchronized skill controller.
public sealed class StandardBearerHoldLock : ModSystem
{
    public override void Load()
    {
        On_Player.HandleHotbar += Hotbar;
        On_Player.ScrollHotbar += Scroll;
        On_Player.SmartSelect_SelectItem += SmartSelect;
        On_Player.DropSelectedItem += Drop;
        On_ItemSlot.LeftClick_ItemArray_int_int += Left;
        On_ItemSlot.RightClick_ItemArray_int_int += Right;
    }
    private static bool Locked(Player player)
    {
        if (!StandardBearerPlayer.IsPhased(player)) return false;
        player.GetModPlayer<StandardBearerPlayer>().EnforceFlag();
        return true;
    }
    private static void Hotbar(On_Player.orig_HandleHotbar orig, Player self) { if (!Locked(self)) orig(self); }
    private static void Scroll(On_Player.orig_ScrollHotbar orig, Player self, int offset) { if (!Locked(self)) orig(self, offset); }
    private static void SmartSelect(On_Player.orig_SmartSelect_SelectItem orig, Player self, int index) { if (!Locked(self)) orig(self, index); }
    private static void Drop(On_Player.orig_DropSelectedItem orig, Player self) { if (!Locked(self)) orig(self); }
    private static bool Bound(Item[] inventory, int slot)
    {
        Player player = Main.LocalPlayer;
        return StandardBearerPlayer.IsPhased(player) && ReferenceEquals(inventory, player.inventory)
            && (slot == player.GetModPlayer<StandardBearerPlayer>().Active.BoundSlot
                || player.GetModPlayer<StandardBearerPlayer>().Active.BoundSlot == 58);
    }
    private static void Left(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inventory, int context, int slot)
    { if (!Bound(inventory, slot)) orig(inventory, context, slot); }
    private static void Right(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inventory, int context, int slot)
    { if (!Bound(inventory, slot)) orig(inventory, context, slot); }
}
