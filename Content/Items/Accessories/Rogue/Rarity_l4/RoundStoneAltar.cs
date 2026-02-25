using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using Terraria.Localization;
namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
    public class RoundStoneAltar : ModItem
    {
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

            Player player = Main.LocalPlayer;
            if (player != null)
            {
                var altarPlayer = player.GetModPlayer<RoundStoneAltarPlayer>();
                string TXT = Language.GetTextValue("Mods.sk.Default.ceng");
                // 添加提示(负责本地化的可能会很累)
                tooltips.Add(new TooltipLine(Mod, "BuffStacks",$"{TXT} {altarPlayer.buffStacks}/10"));
                foreach (TooltipLine line in tooltips)
                {
                    if (line.Mod == "Terraria" || line.Mod == Mod.Name)
                    {
                        if (line.Name == "BuffStacks")
                            line.OverrideColor = Color.Gold;
                        if (line.Name == "BuffEffect")
                            line.OverrideColor = Color.Gold;
                    }
                }
            }
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.Master;
			Item.value = Item.sellPrice(0, 16, 0, 0);
		}

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<RoundStoneAltarPlayer>().active = true;
        }
    }

    public class RoundStoneAltarPlayer : ModPlayer
    {
        public bool active;
        public int buffStacks;
        private bool wasInEventLastFrame;
        private bool hadBossLastFrame;

        public override void SaveData(TagCompound tag)
        {
            tag["RoundStoneAltarStacks"] = buffStacks;
        }

        public override void LoadData(TagCompound tag)
        {
            buffStacks = tag.GetInt("RoundStoneAltarStacks");
        }

        public override void UpdateEquips()
        {
            if (active && buffStacks > 0)
            {
                Player.GetDamage(DamageClass.Generic) += 0.05f * buffStacks;
                Player.statDefense += (int)(Player.statDefense * 0.05f * buffStacks);
            }
        }

        public override void ResetEffects()
        {
            active = false;
        }

        public override void PostUpdate()
        {
            if (!active) return;
            //非常猎奇的方法
            bool isInEventNow = Main.invasionType > 0 || Main.bloodMoon || Main.eclipse ||
                               Main.pumpkinMoon || Main.snowMoon;

            if (isInEventNow && !wasInEventLastFrame)
            {
                TryApplyBuff();
            }
            wasInEventLastFrame = isInEventNow;
            //非常猎奇的方法
            bool hasBossNow = NPC.AnyNPCs(NPCID.MoonLordCore) ||
                            NPC.AnyNPCs(NPCID.EaterofWorldsHead) ||
                            NPC.AnyNPCs(NPCID.SkeletronHead) ||
                            NPC.AnyNPCs(NPCID.TheDestroyer) ||
                            NPC.AnyNPCs(NPCID.SkeletronPrime) ||
                            NPC.AnyNPCs(NPCID.Retinazer) ||
                            NPC.AnyNPCs(NPCID.Spazmatism) ||
                            NPC.AnyNPCs(NPCID.Plantera) ||
                            NPC.AnyNPCs(NPCID.Golem) ||
                            NPC.AnyNPCs(113) ||
                            NPC.AnyNPCs(NPCID.DukeFishron) ||
                            NPC.AnyNPCs(NPCID.EyeofCthulhu) ||
                            NPC.AnyNPCs(NPCID.BrainofCthulhu) ||
                            NPC.AnyNPCs(NPCID.QueenBee) ||
                            false;

            if (hadBossLastFrame && !hasBossNow)
            {
                TryApplyBuff();
            }
            hadBossLastFrame = hasBossNow;
        }

        private void TryApplyBuff()
        {
            if (buffStacks >= 10) return;

            if (Main.rand.NextFloat() < 0.3f)
            {
                buffStacks++;
                string TXT1 = Language.GetTextValue("Mods.sk.Default.RoundStoneAltar");
                CombatText.NewText(Player.getRect(), Color.Blue,
                    $" ({TXT1}{buffStacks}/10)");
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (buffStacks > 0)
            {
                modifiers.FinalDamage *= 1f / (1f + 0.05f * buffStacks);
            }
        }

    }
}