using ArknightsMod.Content.Projectiles.BossArsenal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
namespace ArknightsMod.Content.Items.BossArsenal;

internal static class BossArsenalLoot
{
 internal static readonly string[] Bosses={"AACT","FrostNova","Pompeii","TheFirstToTalk"};
 internal static readonly string[] Prefix={"Harbinger","FrostNova","Pompeii","FirstToTalk"};
 internal static int Item(string name)=>ModContent.GetInstance<ArknightsMod>().Find<ModItem>(name).Type;
 internal static int[] Weapons(int group)=>group*4 >= BossArsenalWeapon.Names.Length ? System.Array.Empty<int>() : new[]{Item(BossArsenalWeapon.Names[group*4]),Item(BossArsenalWeapon.Names[group*4+1]),Item(BossArsenalWeapon.Names[group*4+2]),Item(BossArsenalWeapon.Names[group*4+3])};
}
public sealed class BossArsenalDrops:GlobalNPC
{
 public override void ModifyNPCLoot(NPC npc,NPCLoot loot)
 {
  int group=System.Array.IndexOf(BossArsenalLoot.Bosses,npc.ModNPC?.Name);if(group<0)return;
  string prefix=BossArsenalLoot.Prefix[group];
  var normal=new LeadingConditionRule(new Conditions.NotExpert());
  var weapons=BossArsenalLoot.Weapons(group);
  if(weapons.Length>0)normal.OnSuccess(ItemDropRule.OneFromOptions(1,weapons));
  normal.OnSuccess(ItemDropRule.Common(BossArsenalLoot.Item(prefix+"Mask"),7));loot.Add(normal);
  loot.Add(ItemDropRule.BossBag(BossArsenalLoot.Item(prefix+"RewardBag")));
  loot.Add(ItemDropRule.Common(BossArsenalLoot.Item(prefix+"Trophy"),10));
  loot.Add(ItemDropRule.MasterModeCommonDrop(BossArsenalLoot.Item(prefix+"Relic")));
 }
}
public abstract class BossRewardBag:ModItem
{
 public abstract int Group{get;}
 public override string Texture=>"Terraria/Images/Item_"+ItemID.KingSlimeBossBag;
 public override void SetStaticDefaults(){ItemID.Sets.BossBag[Type]=true;Item.ResearchUnlockCount=3;}
 public override void SetDefaults(){Item.width=Item.height=32;Item.maxStack=Item.CommonMaxStack;Item.consumable=Item.expert=true;Item.rare=ItemRarityID.Expert;}
 public override bool CanRightClick()=>true;
 public override void ModifyItemLoot(ItemLoot loot)
 {
  var weapons=BossArsenalLoot.Weapons(Group);
  if(weapons.Length>0)loot.Add(ItemDropRule.OneFromOptions(1,weapons));
  loot.Add(ItemDropRule.Common(BossArsenalLoot.Item(BossArsenalLoot.Prefix[Group]+"Emblem")));
  loot.Add(ItemDropRule.Common(BossArsenalLoot.Item(BossArsenalLoot.Prefix[Group]+"Mask"),7));
  int boss=Mod.Find<ModNPC>(BossArsenalLoot.Bosses[Group]).Type;loot.Add(ItemDropRule.CoinsBasedOnNPCValue(boss));
 }
}
public abstract class BossRewardEmblem:ModItem
{
 public abstract int Group{get;}
 public override string Texture=>"Terraria/Images/Item_"+new[]{ItemID.AvengerEmblem,ItemID.FrozenTurtleShell,ItemID.MagmaStone,ItemID.NeptunesShell}[Group];
 public override void SetDefaults(){Item.width=Item.height=28;Item.accessory=true;Item.expert=true;Item.rare=ItemRarityID.Expert;Item.value=Item.sellPrice(gold:2);}
 public override void UpdateAccessory(Player p,bool hideVisual)
 {
  if(Group==0){p.GetDamage(DamageClass.Ranged)+=.08f;p.GetDamage(DamageClass.Magic)+=.08f;}
  if(Group==1){p.statManaMax2+=20;p.buffImmune[BuffID.Chilled]=p.buffImmune[BuffID.Frostburn]=true;}
  if(Group==2){p.buffImmune[BuffID.OnFire]=true;p.GetAttackSpeed(DamageClass.Generic)+=.06f;}
  if(Group==3){p.gills=true;p.statDefense+=4;}
 }
}
public abstract class BossRewardMask:ModItem
{
 public override string Texture=>"Terraria/Images/Item_"+ItemID.KingSlimeMask;
 public override void SetDefaults(){Item.CloneDefaults(ItemID.KingSlimeMask);Item.rare=ItemRarityID.Blue;}
}
public abstract class BossRewardMemento:ModItem
{
 public abstract int Group{get;} public virtual bool Relic=>false;
 public override string Texture=>"Terraria/Images/Item_"+(Relic?ItemID.KingSlimeMasterTrophy:ItemID.KingSlimeTrophy);
 public override void SetDefaults()
 {
  Item.DefaultToPlaceableTile(ModContent.TileType<BossRewardMementoTile>(),Group*2+(Relic?1:0));
  Item.width=Item.height=32;Item.rare=Relic?ItemRarityID.Master:ItemRarityID.Blue;Item.master=Relic;Item.value=Item.sellPrice(gold:1);
 }
}
public sealed class BossRewardMementoTile:ModTile
{
 public override string Texture=>"Terraria/Images/Tiles_"+TileID.Banners;
 public override void SetStaticDefaults()
 {
  Main.tileFrameImportant[Type]=true;Main.tileLavaDeath[Type]=true;
  TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);TileObjectData.newTile.StyleHorizontal=true;
  TileObjectData.addTile(Type);AddMapEntry(new Color(150,170,200),CreateMapEntryName());
 }
 public override bool PreDraw(int i,int j,SpriteBatch batch)
 {
  Tile tile=Main.tile[i,j];if(tile.TileFrameX%36!=0||tile.TileFrameY!=0)return false;
  bool relic=(tile.TileFrameX/36)%2==1;int item=relic?ItemID.KingSlimeMasterTrophy:ItemID.KingSlimeTrophy;
  Main.instance.LoadItem(item);Texture2D body=TextureAssets.Item[item].Value;
  Vector2 off=Main.drawToScreen?Vector2.Zero:new Vector2(Main.offScreenRange);
  batch.Draw(body,new Vector2(i*16+16,j*16+32)-Main.screenPosition+off,null,Lighting.GetColor(i,j),0,new Vector2(body.Width*.5f,body.Height),1,SpriteEffects.None,0);return false;
 }
}
public sealed class BossArsenalShimmer:ModSystem
{
 public override void PostSetupContent(){for(int group=0;group<4;group++){var types=BossArsenalLoot.Weapons(group);for(int i=0;i<types.Length;i++)ItemID.Sets.ShimmerTransformToItem[types[i]]=types[(i+1)%types.Length];}}
}
public sealed class HarbingerRewardBag:BossRewardBag{public override int Group=>0;}
public sealed class HarbingerEmblem:BossRewardEmblem{public override int Group=>0;}
public sealed class HarbingerMask:BossRewardMask{}
public sealed class HarbingerTrophy:BossRewardMemento{public override int Group=>0;public override bool Relic=>false;}
public sealed class HarbingerRelic:BossRewardMemento{public override int Group=>0;public override bool Relic=>true;}
public sealed class FrostNovaRewardBag:BossRewardBag{public override int Group=>1;}
public sealed class FrostNovaEmblem:BossRewardEmblem{public override int Group=>1;}
public sealed class FrostNovaMask:BossRewardMask{}
public sealed class FrostNovaTrophy:BossRewardMemento{public override int Group=>1;public override bool Relic=>false;}
public sealed class FrostNovaRelic:BossRewardMemento{public override int Group=>1;public override bool Relic=>true;}
public sealed class PompeiiRewardBag:BossRewardBag{public override int Group=>2;}
public sealed class PompeiiEmblem:BossRewardEmblem{public override int Group=>2;}
public sealed class PompeiiMask:BossRewardMask{}
public sealed class PompeiiTrophy:BossRewardMemento{public override int Group=>2;public override bool Relic=>false;}
public sealed class PompeiiRelic:BossRewardMemento{public override int Group=>2;public override bool Relic=>true;}
public sealed class FirstToTalkRewardBag:BossRewardBag{public override int Group=>3;}
public sealed class FirstToTalkEmblem:BossRewardEmblem{public override int Group=>3;}
public sealed class FirstToTalkMask:BossRewardMask{}
public sealed class FirstToTalkTrophy:BossRewardMemento{public override int Group=>3;public override bool Relic=>false;}
public sealed class FirstToTalkRelic:BossRewardMemento{public override int Group=>3;public override bool Relic=>true;}
