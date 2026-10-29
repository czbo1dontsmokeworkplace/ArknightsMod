using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

// Closed visual dependency graphs are embedded for the authorized weapon ports. No CalamityReforged
// assembly, Phoenix installation, gameplay scheduler or external script is used.
internal static class ReaperSwarmData
{
    private static JsonDocument document, assignments, blades, gammaPlasma;
    private static readonly Dictionary<string,JsonElement> resources=new(StringComparer.Ordinal);
    internal static void Ensure()
    {
        if(document!=null)return;
        document=JsonDocument.Parse(ModContent.GetInstance<ArknightsMod>().GetFileBytes(
            "Content/Projectiles/Guard/Reaper/Phoenix/Assets/Reaper81.json"));
        foreach(var item in document.RootElement.GetProperty("resources").EnumerateObject())resources.Add(item.Name,item.Value);
        assignments=JsonDocument.Parse(ModContent.GetInstance<ArknightsMod>().GetFileBytes(
            "Content/Projectiles/Guard/Reaper/Phoenix/Assets/PhoenixAssignments.json"));
        foreach(var item in assignments.RootElement.GetProperty("resources").EnumerateObject())resources[item.Name]=item.Value;
        blades=JsonDocument.Parse(ModContent.GetInstance<ArknightsMod>().GetFileBytes(
            "Content/Projectiles/Guard/Reaper/Phoenix/Assets/BladeAssignments.json"));
        foreach(var item in blades.RootElement.GetProperty("resources").EnumerateObject())resources[item.Name]=item.Value;
        gammaPlasma=JsonDocument.Parse(ModContent.GetInstance<ArknightsMod>().GetFileBytes(
            "Content/Projectiles/Guard/Reaper/Phoenix/Assets/GammaPlasmaAssignments.json"));
        foreach(var item in gammaPlasma.RootElement.GetProperty("resources").EnumerateObject())resources[item.Name]=item.Value;
    }
    internal static JsonElement Get(string name)
    {
        Ensure();return name!=null && resources.TryGetValue(name,out var value)?value:default;
    }
    internal static void Unload(){document?.Dispose();assignments?.Dispose();blades?.Dispose();gammaPlasma?.Dispose();document=assignments=blades=gammaPlasma=null;resources.Clear();}
}

internal static class ReaperSwarmJson
{
    internal static JsonElement At(this JsonElement e,string key)=>e.ValueKind==JsonValueKind.Object && e.TryGetProperty(key,out var v)?v:default;
    internal static JsonElement At(this JsonElement e,int key)=>e.ValueKind==JsonValueKind.Array && key>=0 && key<e.GetArrayLength()?e[key]:default;
    internal static string Text(this JsonElement e,string fallback="")=>e.ValueKind==JsonValueKind.String?e.GetString():fallback;
    internal static string S(this JsonElement e,string key,string fallback="")=>e.At(key).Text(fallback);
    internal static float Number(this JsonElement e,float fallback=0)=>e.ValueKind==JsonValueKind.Number?e.GetSingle():fallback;
    internal static float F(this JsonElement e,string key,float fallback=0)=>e.At(key).Number(fallback);
    internal static bool B(this JsonElement e,string key,bool fallback=false)=>e.At(key).ValueKind==JsonValueKind.True || (e.At(key).ValueKind!=JsonValueKind.False && fallback);
    internal static Vector2 V(this JsonElement e,Vector2 fallback)=>e.ValueKind==JsonValueKind.Array && e.GetArrayLength()>=2?new(e[0].Number(),e[1].Number()):fallback;
    internal static Vector2 V(this JsonElement e,string key,Vector2 fallback)=>e.At(key).V(fallback);
    internal static Vector4 Tint(this JsonElement e)=>e.At("color").ValueKind==JsonValueKind.Array?new(e.At("color")[0].Number(1),e.At("color")[1].Number(1),e.At("color")[2].Number(1),e.At("color")[3].Number(1)):new Vector4(1,1,1,e.F("alpha",1));
    internal static IEnumerable<JsonElement> Array(this JsonElement e)=>e.ValueKind==JsonValueKind.Array?e.EnumerateArray():System.Array.Empty<JsonElement>();
    internal static Vector2 Local(Vector2 p,Vector2 forward)=>forward*p.Y+new Vector2(-forward.Y,forward.X)*p.X;
    internal static Vector2 Unit(this Vector2 p)=>p.LengthSquared()>.000001f?Vector2.Normalize(p):Vector2.UnitX;
    internal static Vector2 Rotate(this Vector2 p,float angle)=>new(p.X*MathF.Cos(angle)-p.Y*MathF.Sin(angle),p.X*MathF.Sin(angle)+p.Y*MathF.Cos(angle));
}
