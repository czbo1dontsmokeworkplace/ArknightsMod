using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

internal static class ReaperWaveNovaVisuals
{
    internal const string BloomPath="ArknightsMod/Content/Textures/ShpcEnergy/BloomCircle";
    private static Texture2D bloom;
    internal static void Bloom(Vector2 at,Vector2 size,Color color,float rotation=0)
    {
        if(Main.dedServ)return;
        bloom??=ModContent.Request<Texture2D>(BloomPath).Value;
        color.A=0;
        Main.spriteBatch.Draw(bloom,at-Main.screenPosition,null,color,rotation,
            bloom.Size()*.5f,size/bloom.Size(),SpriteEffects.None,0);
    }
    internal static void Unload()=>bloom=null;
}
public sealed class ReaperWaveNovaVisualSystem:ModSystem
{
    public override void Unload()=>ReaperWaveNovaVisuals.Unload();
}
