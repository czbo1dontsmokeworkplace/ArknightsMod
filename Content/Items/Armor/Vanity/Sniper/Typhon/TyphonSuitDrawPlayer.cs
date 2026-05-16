using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Typhon
{
	public class TyphonSuitDrawPlayer : ModPlayer
	{
		public override void TransformDrawData(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (!TyphonVanityAnim.BodyFrameMatchesHornsLongStrip(p))
				return;
			if (!IsTyphonBodyEquipped(p))
				return;

			Texture2D bodyArmorTex = TextureAssets.ArmorBody[p.body].Value;
			var cache = drawInfo.DrawDataCache;
			for (int i = 0; i < cache.Count; i++) {
				DrawData d = cache[i];
				if (d.texture != bodyArmorTex)
					continue;
				Vector2 pos = d.position;
				pos.Y -= p.gravDir;
				d.position = pos;
				cache[i] = d;
			}
		}

		private static bool IsTyphonBodyEquipped(Player p)
		{
			int t = ModContent.ItemType<TyphonBody>();
			if (p.armor[1].type == t)
				return true;
			if (p.armor.Length > 11 && p.armor[11].type == t)
				return true;
			return false;
		}
	}
}
