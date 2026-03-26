using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Tools;

namespace ArknightsMod.Content.Players.InstancedSpace
{
	public class InstancedSchematicWrenchPlayer : ModPlayer
	{
		public Point? First;

		public override void Initialize()
		{
			First = null;
		}

		public override void PostUpdate()
		{
			if (Player.whoAmI != Main.myPlayer)
				return;

			Item held = Player.HeldItem;
			if (held == null || held.IsAir)
				return;
			if (held.type != ModContent.ItemType<InstancedSchematicWrench>())
				return;
			if (!First.HasValue)
				return;

			Point a = First.Value;
			Point b = Main.MouseWorld.ToTileCoordinates();
			int left = Math.Min(a.X, b.X);
			int right = Math.Max(a.X, b.X);
			int top = Math.Min(a.Y, b.Y);
			int bottom = Math.Max(a.Y, b.Y);

			for (int x = left; x <= right; x++)
			{
				SpawnEdgeDust(x, top);
				SpawnEdgeDust(x, bottom);
			}
			for (int y = top; y <= bottom; y++)
			{
				SpawnEdgeDust(left, y);
				SpawnEdgeDust(right, y);
			}
		}

		private static void SpawnEdgeDust(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 1))
				return;

			Vector2 pos = new Vector2(x * 16 + 8, y * 16 + 8);
			int d = Dust.NewDust(pos - new Vector2(4, 4), 0, 0, DustID.Electric, 0f, 0f, 150, default, 0.9f);
			Main.dust[d].noGravity = true;
			Main.dust[d].velocity *= 0f;
		}
	}
}
