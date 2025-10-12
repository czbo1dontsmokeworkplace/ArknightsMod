using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace ArknightsMod.Content.Items.Weapons
{
    public class BeagleWeapon : ModItem
    {
        public override string Texture => base.Texture + "_Sword";
        public override void SetDefaults()
        {
            Item.damage = 25; // 基础伤害
            Item.knockBack = 7;
            Item.crit = 2; // 爆击率
            Item.DamageType = DamageClass.Melee; // 伤害类型
            Item.width = 48; // 物品宽度
            Item.height = 60; // 物品高度
            Item.useTime = 25; // 使用时间
            Item.useAnimation = 25; // 使用动画时间
            Item.autoReuse = true; // 自动使用
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HiddenAnimation;
        }
    }
}