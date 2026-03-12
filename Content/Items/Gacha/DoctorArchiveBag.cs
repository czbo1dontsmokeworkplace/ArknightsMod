using System;
using System.Collections.Generic;
using ArknightsMod.Players;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Gacha
{
	public class DoctorArchiveBag : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			try
			{
				var player = Main.LocalPlayer;
				if (player is null)
					return;

				var gacha = player.GetModPlayer<DoctorArchiveGachaPlayer>();
				int remaining = Utils.Clamp(90 - gacha.PullsSinceLastSixStar, 1, 90);
				string text = $"距离6星保底：还差 {remaining} 抽";
				tooltips.Add(new TooltipLine(Mod, "DoctorArchivePity", text));
			}
			catch
			{
			}
		}

		public override void SetDefaults()
		{
			Item.maxStack = 9999;
			Item.consumable = true;
			Item.width = 30;
			Item.height = 56;
			Item.rare = ItemRarityID.White;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.UseSound = SoundID.Item4;
		}

		private static bool IsShiftDown()
		{
			var state = Main.keyState;
			return state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
		}

		public override bool CanRightClick()
		{
			if (Item.stack <= 0)
				return false;

			bool shift = IsShiftDown();

			if (shift)
				return Item.stack >= 10;

			return true;
		}

		public override void RightClick(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return;

			bool shift = IsShiftDown();
			int pulls = shift ? 10 : 1;

			// Note: For CanRightClick() items, tModLoader will automatically consume 1 item
			// from the stack before calling RightClick(). So at this point, Item.stack is
			// already (originalStack - 1).
			int originalStack = Item.stack + 1;
			if (originalStack < pulls)
				return;

			int extraToConsume = pulls - 1;
			if (extraToConsume > 0)
			{
				if (Item.stack < extraToConsume)
					return;
				Item.stack -= extraToConsume;
				if (Item.stack <= 0)
					Item.TurnToAir();
			}

			DoPulls(player, pulls);
		}

		private void DoPulls(Player player, int pulls)
		{
			var gacha = player.GetModPlayer<DoctorArchiveGachaPlayer>();
			if (pulls >= 10)
				gacha.BeginTenPull();

			for (int i = 0; i < pulls; i++)
			{
				int stars = RollStars(gacha, pulls >= 10);
				var set = RollSet(stars);
				GiveSet(player, set);
				gacha.RegisterPull(stars);
			}
		}

		private int RollStars(DoctorArchiveGachaPlayer gacha, bool isTenPull)
		{
			if (gacha.PullsSinceLastSixStar >= 89)
				return 6;

			bool forceAtLeastFiveStar =
				isTenPull &&
				gacha.PullsInCurrentTenPull == 9 &&
				!gacha.TenPullHadAtLeastFiveStar;

			if (forceAtLeastFiveStar)
			{
				int highRoll = Main.rand.Next(20);
				return highRoll < 4 ? 6 : 5;
			}

			int roll = Main.rand.Next(100);
			if (roll < 4)
				return 6;
			if (roll < 20)
				return 5;
			if (roll < 60)
				return 4;
			return 3;
		}

		private static DoctorArchiveGachaData.VanitySet RollSet(int stars)
		{
			var sets = DoctorArchiveGachaData.GetSetsByStars(stars);
			if (sets.Length == 0)
				sets = DoctorArchiveGachaData.GetSetsByStars(3);

			return sets[Main.rand.Next(sets.Length)];
		}

		private void GiveSet(Player player, DoctorArchiveGachaData.VanitySet set)
		{
			var source = player.GetSource_OpenItem(Type);

			if (TryGiveVanityBag(player, source, set))
				return;

			foreach (string itemKey in set.ItemKeys)
			{
				if (!Mod.TryFind<ModItem>(itemKey, out var modItem))
					continue;

				player.QuickSpawnItem(source, modItem.Type, 1);
			}
		}

		private bool TryGiveVanityBag(Player player, IEntitySource source, DoctorArchiveGachaData.VanitySet set)
		{
			string[] candidateKeys = set.SetKey == "Wisadel"
				?
				[
					$"{set.SetKey}VanityBag",
					$"{set.SetKey}Default",
					"WisdelVanityBag",
					"WisdelDefault",
				]
				:
				[
					$"{set.SetKey}VanityBag",
					$"{set.SetKey}Default",
				];

			foreach (string key in candidateKeys)
			{
				if (!Mod.TryFind<ModItem>(key, out var modItem))
					continue;

				Item spawned = player.QuickSpawnItemDirect(source, modItem.Type, 1);
				spawned.rare = GetRarityForStars(set.Stars);
				return true;
			}

			return false;
		}

		private static int GetRarityForStars(int stars)
		{
			return stars switch
			{
				6 => ItemRarityID.Red,
				5 => ItemRarityID.Pink,
				4 => ItemRarityID.Orange,
				_ => ItemRarityID.Green,
			};
		}
	}
}
