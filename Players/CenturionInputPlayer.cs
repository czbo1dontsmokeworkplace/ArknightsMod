using ArknightsMod.Common.GlobalItems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

// Both buttons enter the same native item-use pipeline and spawn the same holdout.
public sealed class CenturionInputPlayer : ModPlayer
{
    private bool rightHeld;
    private bool previousRightHeld;
    private static int SawAxePower => ContentSamples.ItemsByType[ItemID.ButchersChainsaw].axe;

    public override void SetControls()
    {
        rightHeld = false;
        if (OperatorBranchTraits.Branch(Player.HeldItem) != "Centurion")
        {
            previousRightHeld = false;
            return;
        }

        rightHeld = Player.controlUseTile && !Player.mouseInterface && !Main.blockMouse;
        if (rightHeld)
        {
            Player.controlUseItem = true;
            Player.controlUseTile = false;
            if (!previousRightHeld && Player.itemAnimation == 0)
                Player.releaseUseItem = true;
        }
        previousRightHeld = rightHeld;
    }

    public override bool PreItemCheck()
    {
        if (OperatorBranchTraits.Branch(Player.HeldItem) == "Centurion")
            Player.HeldItem.axe = rightHeld ? SawAxePower : 0;
        return true;
    }
}
