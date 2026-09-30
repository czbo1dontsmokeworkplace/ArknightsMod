using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Content.Items.Evolution;

public class EvolutionTrophyTile : ModTile
{
    public override string Texture => EvolutionVisuals.Root + "Trophy";
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(163, 44, 48), CreateMapEntryName());
        DustType = DustID.Blood;
    }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];
        if (tile.TileFrameX == 0 && tile.TileFrameY == 0)
        {
            Vector2 offscreen = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Texture2D texture = EvolutionVisuals.Asset("Trophy");
            spriteBatch.Draw(texture, new Vector2(i * 16 - 3, j * 16 - 2) - Main.screenPosition + offscreen,
                this is EvolutionRelicTile ? new Color(255, 213, 116) : Lighting.GetColor(i, j));
        }
        return false;
    }
}
public sealed class EvolutionRelicTile : EvolutionTrophyTile { }
