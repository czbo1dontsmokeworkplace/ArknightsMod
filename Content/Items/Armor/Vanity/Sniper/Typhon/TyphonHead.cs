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

		/// <summary>
		/// 仅在行走躯体动画的第几行（0-based）绘制补图。
		/// 对应长条从上往下数第 7、8、9、14、15、16 帧 → 行号 6–8、13–15。
		/// </summary>
		private static readonly int[] OverflowOnlyOnBodyRows = { 6, 7, 8, 13, 14, 15 };

		/// <summary>
		/// 在「当前头饰帧矩形顶边中点」基础上的微调（像素）。
		/// X 乘朝向，Y 乘重力方向；若补图整体偏下，可把 Y 调成负数（例如 -2）往上移。
		/// </summary>
		private static readonly Vector2 OverflowMaterialTopNudge = Vector2.Zero;

		internal class TyphonHeadOverflowLayer : PlayerDrawLayer
		{
			/// <summary>紧跟原版头部层之后，补图与 <c>TyphonHead_Head</c> 同一套 headPosition/headVect 对齐。</summary>
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
				return !plr.dead && IsWearingTyphonHead(plr);
			}

			private static bool ShouldDrawForBodyFrame(Player p)
			{
				int bh = p.bodyFrame.Height;
				if (bh <= 0)
					return false;
				int row = p.bodyFrame.Y / bh;
				foreach (int r in OverflowOnlyOnBodyRows) {
					if (row == r)
						return true;
				}

				return false;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo)
			{
				Player p = drawInfo.drawPlayer;
				if (!ShouldDrawForBodyFrame(p))
					return;

				Texture2D extra = ModContent.Request<Texture2D>(
					"ArknightsMod/Content/Items/Armor/Vanity/Sniper/Typhon/TyphonHead_Horns").Value;
				if (extra.Width <= 0 || extra.Height <= 0)
					return;

				// 与 vanilla DrawPlayer_21_Head（默认头盔分支）完全一致：
				// 基准不是 Position-screen，而是 body 与实体宽度对齐后的整数坐标，再 + helmetOffset + headPosition + headVect；
				// headPosition 不得再乘 direction（朝向由 playerEffect 翻转处理）。
				// 头盔 DrawData 的 position 为旋转锚点 headVect2，source 为 bodyFrame3，故顶边中点：
				// topCenter = helmetDrawPos + (-headVect2.X + bodyFrame3.Width/2, -headVect2.Y)。
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

				Vector2 origin = new(extra.Width * 0.5f, extra.Height);

				drawInfo.DrawDataCache.Add(
					new DrawData(extra, topCenter, null, drawInfo.colorArmorHead, p.headRotation, origin, 1f, drawInfo.playerEffect, 0) {
						shader = drawInfo.cHead
					});
			}
		}
	}
}
