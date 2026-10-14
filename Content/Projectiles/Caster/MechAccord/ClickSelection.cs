using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

internal static class ClickSelection
{
    internal const int MaxTargets = 80;
    internal static Rectangle Area(Vector2 start, Vector2 end)
    {
        Point a = start.ToTileCoordinates(), b = end.ToTileCoordinates();
        b.X = Math.Clamp(b.X, a.X - 19, a.X + 19);
        b.Y = Math.Clamp(b.Y, a.Y - 19, a.Y + 19);
        return new Rectangle(Math.Min(a.X, b.X) * 16, Math.Min(a.Y, b.Y) * 16,
            (Math.Abs(a.X - b.X) + 1) * 16, (Math.Abs(a.Y - b.Y) + 1) * 16);
    }
    internal static List<NPC> Targets(Rectangle area, Player player, float reach)
    {
        List<NPC> result = new();
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy() && area.Intersects(npc.Hitbox) && npc.DistanceSQ(player.Center) <= reach * reach)
                result.Add(npc);
        result.Sort((a,b) => a.DistanceSQ(area.Center.ToVector2()).CompareTo(b.DistanceSQ(area.Center.ToVector2())));
        if (result.Count > MaxTargets) result.RemoveRange(MaxTargets, result.Count - MaxTargets);
        return result;
    }
    internal static void Draw(Rectangle world, float opacity = 1f)
    {
        using var pass = new MechAccordVfx.Pass(BlendState.NonPremultiplied);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        void Rect(float x, float y, float w, float h, Color color) => Main.spriteBatch.Draw(pixel,
            new Vector2(x,y)-Main.screenPosition, new Rectangle(0,0,1,1),
            MechAccordVfx.Fade(color,opacity),0,Vector2.Zero,new Vector2(w,h),SpriteEffects.None,0);
        Color edge = new(90,235,210,230);
        Rect(world.X,world.Y,world.Width,world.Height,new Color(30,105,120,30));
        for(int x=world.Left+16;x<world.Right;x+=16) Rect(x,world.Y,1,world.Height,new Color(120,220,225,35));
        for(int y=world.Top+16;y<world.Bottom;y+=16) Rect(world.X,y,world.Width,1,new Color(120,220,225,35));
        Rect(world.X,world.Y,world.Width,2,edge); Rect(world.X,world.Bottom-2,world.Width,2,edge);
        Rect(world.X,world.Y,2,world.Height,edge); Rect(world.Right-2,world.Y,2,world.Height,edge);
        float scan=world.Y+(Main.GlobalTimeWrappedHourly*80f%world.Height);
        Rect(world.X,scan,world.Width,1,new Color(140,255,240,70));
    }
}
