using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Typhon
{
	[AutoloadEquip(EquipType.Head)]
	public class TyphonHead : ArknightsVanityHead
	{
		public override int Rarity => 6;

		internal static int HeadEquipSlot = -1;

		public override void Load()
		{
			HeadEquipSlot = Item.headSlot;
		}

		public override void SafeSetStaticDefaults()
		{
			HeadEquipSlot = Item.headSlot;
		}

		public override void SafeSetDefaults()
		{
			HeadEquipSlot = Item.headSlot;
		}

		private static readonly int[] OverflowOnlyOnBodyRows = { 6, 7, 8, 13, 14, 15 };

		private const int PlayerArmorSheetRowHeight = 56;

		private const float VelocityBranchEpsilon = 0.0001f;

		private static readonly Vector2 OverflowMaterialTopNudge = Vector2.Zero;

		private const float OverflowDrawDownPx = 0f;

		internal class TyphonHeadOverflowLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() =>
				new AfterParent(PlayerDrawLayers.Head);

			private static int TyphonHeadItemType =>
				ModContent.ItemType<TyphonHead>();

			private static bool IsWearingTyphonHead(Player p)
			{
				int t = TyphonHeadItemType;
				if (p.armor[0].type == t)
					return true;
				if (p.armor.Length > 10 && p.armor[10].type == t)
					return true;
				return HeadEquipSlot >= 0 && p.head == HeadEquipSlot;
			}

			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			{
				Player plr = drawInfo.drawPlayer;
				return !plr.dead && IsWearingTyphonHead(plr) && ShouldDrawOverflow(plr);
			}

			private static bool MatchesVanillaWalkBodyLegSync(Player p)
			{
				if (Math.Abs(p.velocity.Y) > VelocityBranchEpsilon)
					return false;
				if (Math.Abs(p.velocity.X) <= VelocityBranchEpsilon)
					return false;
				if (p.legs == 140)
					return true;
				return p.bodyFrame.Y == p.legFrame.Y;
			}

			private static bool ShouldDrawOverflow(Player p)
			{
				if (!MatchesVanillaWalkBodyLegSync(p))
					return false;
				if (p.bodyFrame.Height != PlayerArmorSheetRowHeight)
					return false;
				int row = p.bodyFrame.Y / PlayerArmorSheetRowHeight;
				foreach (int r in OverflowOnlyOnBodyRows) {
					if (row == r)
						return true;
				}

				return false;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo)
			{
				Player p = drawInfo.drawPlayer;
				if (!ShouldDrawOverflow(p))
					return;

				Texture2D extra = ModContent.Request<Texture2D>(
					"ArknightsMod/Content/Items/Armor/Vanity/Sniper/Typhon/TyphonHead_Horns").Value;
				if (extra.Width <= 0 || extra.Height <= 0)
					return;

				Rectangle bodyFrame3 = p.bodyFrame;
				Vector2 headVect2 = drawInfo.headVect;
				if (p.gravDir == 1f)
					bodyFrame3.Height -= 4;
				else {
					headVect2.Y -= 4f;
					bodyFrame3.Height -= 4;
				}

				if (bodyFrame3.Width <= 0 || bodyFrame3.Height <= 0)
					return;

				Vector2 basePos = new Vector2(
					(float)((int)(drawInfo.Position.X - Main.screenPosition.X - (float)(p.bodyFrame.Width / 2) + (float)(p.width / 2))),
					(float)((int)(drawInfo.Position.Y - Main.screenPosition.Y + (float)p.height - (float)p.bodyFrame.Height + 4f)));

				Vector2 helmetDrawPos = drawInfo.helmetOffset + basePos + p.headPosition + drawInfo.headVect;

				Vector2 topCenter = helmetDrawPos + new Vector2(
					-headVect2.X + bodyFrame3.Width * 0.5f + OverflowMaterialTopNudge.X * p.direction,
					-headVect2.Y + OverflowMaterialTopNudge.Y * p.gravDir);
				topCenter.Y += OverflowDrawDownPx * p.gravDir;

				Vector2 origin = new(extra.Width * 0.5f, extra.Height);

				drawInfo.DrawDataCache.Add(
					new DrawData(extra, topCenter, null, drawInfo.colorArmorHead, p.headRotation, origin, 1f, drawInfo.playerEffect, 0) {
						shader = drawInfo.cHead
					});
			}
		}
	}
}
