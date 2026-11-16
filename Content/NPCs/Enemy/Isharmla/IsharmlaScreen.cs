using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class IsharmlaScreen : ModSystem
{
    private const string Key="ArknightsMod:IsharmlaOceanPressure";
    private float strength;
    public override void Load()
    {
        if(Main.dedServ)return;
        var effect=ModContent.Request<Effect>("ArknightsMod/Assets/Effects/IsharmlaScreen",AssetRequestMode.ImmediateLoad);
        Filters.Scene[Key]=new Filter(new ScreenShaderData(new Ref<Effect>(effect.Value),"OceanPressure").UseOpacity(0),EffectPriority.High);
        Filters.Scene[Key].Load();
    }
    public override void PostUpdateEverything()
    {
        if(Main.dedServ)return;
        var filter=Filters.Scene[Key];if(filter==null)return;
        var boss=IsharmlaWorld.ActiveBoss();
        bool active=!Main.gameMenu&&!Main.LocalPlayer.dead&&boss is {Initialized:true}&&boss.Stage!=TideStage.Finale
            &&Vector2.DistanceSquared(Main.LocalPlayer.Center,boss.NPC.Center)<6500*6500;
        float target=active?(boss.Stage>=TideStage.Leviathan?.38f:.24f):0;
        strength=MathHelper.Lerp(strength,target,target>strength?.025f:.065f);
        if(strength<.001f){strength=0;if(filter.IsActive())filter.Deactivate();return;}
        var shader=filter.GetShader();shader.UseOpacity(strength);
        Vector2 at=active?(boss.Dragon is {Pattern:6}?boss.Dragon.Aim:boss.Dragon?.NPC.Center??boss.NPC.Center):Main.LocalPlayer.Center;
        Vector2 center=(at-Main.screenPosition)/new Vector2(Math.Max(1,Main.screenWidth),Math.Max(1,Main.screenHeight));
        shader.Shader.Parameters["WaveCenter"].SetValue(Vector2.Clamp(center,new Vector2(-.25f),new Vector2(1.25f)));
        shader.Shader.Parameters["Aspect"].SetValue(Main.screenWidth/(float)Math.Max(1,Main.screenHeight));
        shader.Shader.Parameters["Pulse"].SetValue(boss?.Dragon is {Pattern:6}?1.4f:boss?.Transition>0?1.1f:.55f);
        if(active&&!filter.IsActive())Filters.Scene.Activate(Key,at);
    }
    private void Reset(){strength=0;if(!Main.dedServ&&Filters.Scene[Key] is { } f){f.GetShader().UseOpacity(0);f.Deactivate();}}
    public override void OnWorldUnload()=>Reset();
    public override void Unload()=>Reset();
}
