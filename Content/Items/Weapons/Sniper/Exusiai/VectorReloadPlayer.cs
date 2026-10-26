using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;

public sealed class VectorReloadPlayer : ModPlayer
{
    private ExusiaiVector bound;
    internal int BoundSlot { get; private set; } = -1;
    public bool Locked => Player.active && !Player.dead && bound?.Magazine.Reloading == true
        && BoundSlot >= 0 && BoundSlot < Player.inventory.Length
        && ReferenceEquals(Player.inventory[BoundSlot].ModItem, bound);
    internal void Bind(ExusiaiVector weapon)
    {
        if (Locked || !weapon.Magazine.Reloading) return;
        for (int i = 0; i < Player.inventory.Length; i++)
            if (ReferenceEquals(Player.inventory[i].ModItem, weapon)) { bound = weapon; BoundSlot = i; return; }
    }
    internal void Enforce()
    {
        if (!Locked) return;
        Player.selectedItem = BoundSlot;
        Player.controlUseItem = Player.controlUseTile = Player.controlThrow = false;
    }
    public override void SetControls() => Enforce();
    public override bool PreItemCheck() { Enforce(); return true; }
    public override void PostUpdate()
    {
        if (Locked) Enforce();
        else { bound = null; BoundSlot = -1; }
    }
    public override void UpdateDead() { bound = null; BoundSlot = -1; }
}

// Same native switch/drop/slot boundaries as StandardBearerHoldLock; only active during reload.
public sealed class VectorReloadLock : ModSystem
{
    public override void Load()
    {
        On_Player.HandleHotbar += Hotbar;
        On_Player.ScrollHotbar += Scroll;
        On_Player.SmartSelect_SelectItem += Smart;
        On_Player.DropSelectedItem += Drop;
        On_ItemSlot.LeftClick_ItemArray_int_int += Left;
        On_ItemSlot.RightClick_ItemArray_int_int += Right;
    }
    private static bool Locked(Player p)
    {
        var state = p.GetModPlayer<VectorReloadPlayer>();
        if (!state.Locked) return false;
        state.Enforce(); return true;
    }
    private static void Hotbar(On_Player.orig_HandleHotbar orig, Player p) { if (!Locked(p)) orig(p); }
    private static void Scroll(On_Player.orig_ScrollHotbar orig, Player p, int offset) { if (!Locked(p)) orig(p, offset); }
    private static void Smart(On_Player.orig_SmartSelect_SelectItem orig, Player p, int index) { if (!Locked(p)) orig(p, index); }
    private static void Drop(On_Player.orig_DropSelectedItem orig, Player p) { if (!Locked(p)) orig(p); }
    private static bool Bound(Item[] items, int slot)
    {
        var state = Main.LocalPlayer.GetModPlayer<VectorReloadPlayer>();
        return state.Locked && ReferenceEquals(items, Main.LocalPlayer.inventory)
            && (slot == state.BoundSlot || state.BoundSlot == 58);
    }
    private static void Left(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] items, int context, int slot)
    { if (!Bound(items, slot)) orig(items, context, slot); }
    private static void Right(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] items, int context, int slot)
    { if (!Bound(items, slot)) orig(items, context, slot); }
}
