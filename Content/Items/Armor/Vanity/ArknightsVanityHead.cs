using System;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsVanityHead : ModItem
	{
		/// <summary>
		/// 干员的星数，请输入1~6
		/// </summary>
		public virtual int Rarity => 6;
		public virtual int Value => 15000;
		public sealed override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
			SafeSetStaticDefaults();
		}
		public sealed override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.rare = GetRarity(Rarity);
			Item.value = Value;
			Item.vanity = true;
			SafeSetDefaults();
		}
		public static int GetRarity(int rarity) {
			rarity = Math.Clamp(rarity, 1, 6);
			int result = rarity switch {
				1 => ItemRarityID.White,
				2 => ItemRarityID.White,
				3 => ItemRarityID.White,
				4 => ItemRarityID.Blue,
				5 => ItemRarityID.Orange,
				6 => ItemRarityID.Quest,
				_ => ItemRarityID.Quest
			};
			return result;
		}
		public virtual void SafeSetDefaults() { }
		public virtual void SafeSetStaticDefaults() { }
	}
}
