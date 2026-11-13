using System;
using ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace ArknightsMod.Content.BossBars;

public class AACTBossBar : ModBossBar
{
    private int head = -1;
    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) => head >= 0 ? TextureAssets.NpcHeadBoss[head] : null;
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        NPC npc = Main.npc[info.npcIndexToAimAt];
        if (!npc.active) return false;
        head = npc.GetBossHeadTextureIndex();
        // Never expose the real ratio through the default progress bar information.
        float t = (float)Main.GlobalTimeWrappedHourly;
        lifeMax = 100; life = 50 + 30 * MathF.Sin(t * 7.3f); shield = shieldMax = 0;
        return true;
    }
    public override bool PreDraw(SpriteBatch b, NPC npc, ref BossBarDrawParams drawParams)
    {
        Vector2 center = drawParams.BarCenter;
        int tick = (int)(Main.GameUpdateCount / 4);
        Rectangle outer = new((int)center.X - 230, (int)center.Y - 15, 460, 30);
        b.Draw(TextureAssets.MagicPixel.Value, outer, new Color(8, 7, 14) * .95f);
        HarbingerVisuals.Line(b, outer.TopLeft(), outer.TopLeft() + new Vector2(outer.Width, 0), HarbingerVisuals.Red, 2);
        HarbingerVisuals.Line(b, outer.BottomLeft(), outer.BottomLeft() + new Vector2(outer.Width, 0), HarbingerVisuals.Cyan * .7f, 2);
        for (int i = 0; i < 43; i++) {
            int noise = (i * 19 + tick * 7) % 29;
            if (noise < 5) continue;
            b.Draw(TextureAssets.MagicPixel.Value, new Rectangle(outer.X + 5 + i * 10, outer.Y + 6 + noise % 3,
                7, 10 + noise % 10), (noise % 3 == 0 ? HarbingerVisuals.Cyan : HarbingerVisuals.Red) * .65f);
        }
        string code = HarbingerRules.Corrupt("0xDEAD / NULL :: ??%", tick, 331);
        Utils.DrawBorderString(b, code, center - new Vector2(0, 11), Color.White, .8f, .5f);
        return false;
    }
}
