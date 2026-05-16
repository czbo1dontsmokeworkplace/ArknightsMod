using ArknightsMod.Content.Players.InstancedSpace;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.InstancedSpace
{
	public class InstancedRoomTransitionVisualSystem : ModSystem
	{
		public override void PostDrawInterface(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
		{
			if (Main.dedServ)
				return;

			float a = InstancedRoomTransitionPlayer.BlackOverlayOpacity;
			if (a <= 0f)
				return;

			var pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * a);
		}
	}
}
