using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace ArknightsMod.Content.SwingHelper
{
    public class EffectLoad : ModSystem
    {
        public const string Index = "ArknightsMod/Content/SwingHelper/Effects/";
        public override void Load()
        {
            SwingHelper.Flow = ModContent.Request<Effect>(Index+"BladeFlow",AssetRequestMode.ImmediateLoad).Value;
            SwingHelper.Dissolve = ModContent.Request<Effect>(Index+"BladeDissolve",AssetRequestMode.ImmediateLoad).Value;
            SwingHelper.NoiseFlow = ModContent.Request<Effect>(Index+"BladeNoiseFlow",AssetRequestMode.ImmediateLoad).Value;
        }
    }
}

