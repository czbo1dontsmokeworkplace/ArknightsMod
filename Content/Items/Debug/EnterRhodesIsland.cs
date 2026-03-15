using ArknightsMod.Subworlds.RhodesIsland;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Debug;

public class EnterRhodesIsland : ModItem
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.Wrench}";

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Wrench);
	}

	public override bool CanUseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			if (SubworldSystem.IsActive<RhodesIslandSubworld>())
			{
				SubworldSystem.Exit();
			}
			else
			{
				SubworldSystem.Enter<RhodesIslandSubworld>();
			}
		}

		return base.CanUseItem(player);
	}
}