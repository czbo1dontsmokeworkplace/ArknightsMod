using ArknightsMod.Content.Items.Gacha;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Gacha
{
	public class DoctorArchiveVanityRarityGlobalItem : GlobalItem
	{
		public override void SetDefaults(Item item)
		{
			if (item.ModItem is null)
				return;

			if (!DoctorArchiveGachaData.ItemKeyToStars.TryGetValue(item.ModItem.Name, out int stars))
				return;

			item.rare = stars switch
			{
				3 => ItemRarityID.Blue,
				4 => ItemRarityID.White,
				5 => ItemRarityID.Yellow,
				6 => ItemRarityID.Red,
				_ => ItemRarityID.White
			};
		}
	}
}
