using System;
using ArknightsMod.Content.ElementalImpairment.Effect;
using ArknightsMod.Content.Items.Weapons.Defender;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class DefenderRouteEffects
{
    internal static void FireSentinel(Player owner, Vector2 direction, int damage, float knockback)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        bool spread = owner.HeldItem.ModItem.Name == "FaithMixer";
        bool cluster = owner.HeldItem.ModItem.Name == "MechanistGun" && DefenderRules.Skill(owner, 0);
        int count = cluster ? 5 : spread ? 3 : 1;
        for (int i = 0; i < count; i++)
        {
            int index = Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), owner.MountedCenter + direction * 22f,
                direction.RotatedBy(count > 1 ? (i - (count - 1) * .5f) * .09f : 0) * 20f,
                ModContent.ProjectileType<DefenderElectron>(), cluster ? Math.Max(1, (int)(damage * .55f)) : spread ? Math.Max(1, (int)(damage * .45f)) : damage,
                knockback, owner.whoAmI, cluster ? 2 : DefenderRules.Skill(owner, 1) ? 1 : 0, owner.HeldItem.type);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = owner.GetWeaponCrit(owner.HeldItem);
        }
    }
    internal static void FireShalem(Player owner, Vector2 direction, int damage)
    {
        if (owner.whoAmI != Main.myPlayer || !DefenderRules.Skill(owner, 1) || owner.HeldItem.ModItem?.Name != "ShalemBlade") return;
        var targets = new System.Collections.Generic.List<NPC>();
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy() && owner.DistanceSQ(npc.Center) < 540f * 540f
                && Collision.CanHitLine(owner.Center, 1, 1, npc.position, npc.width, npc.height)) targets.Add(npc);
        if (targets.Count == 0) return;
        for (int i = 0; i < 6; i++)
        {
            NPC target = targets[Main.rand.Next(targets.Count)];
            int index = Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), owner.MountedCenter,
                direction.RotatedBy((i - 2.5f) * .14f) * 10f, ModContent.ProjectileType<DefenderArtsShard>(),
                System.Math.Max(1, (int)(damage * .3f)), 1f, owner.whoAmI, target.whoAmI);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = owner.GetWeaponCrit(owner.HeldItem);
        }
    }
    internal static void Contact(Player owner, NPC target, int damageDone)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || damageDone <= 0) return;
        string name = owner.HeldItem.ModItem?.Name;
        if (name == "SanbyBlade" && DefenderRules.Skill(owner, 0) && !target.boss && target.realLife < 0)
        {
            target.velocity += (target.Center - owner.Center).SafeNormalize(Vector2.UnitX) * 3f * target.knockBackResist;
            target.netUpdate = true;
        }
        if (name == "AuroraAxe")
        {
            target.AddBuff(BuffID.Frostburn, 180);
            if (DefenderRules.Skill(owner, 1)) target.GetGlobalNPC<DefenderColdNPC>().Chill(target);
        }
        if (name is "AsbestosBlade" or "HoshigumaBlazingBlade" && DefenderRules.SkillActive(owner)) target.AddBuff(BuffID.OnFire3, 180);
        if (!DefenderRules.IsPrimal(owner.HeldItem)) return;
        int amount = Math.Max(1, (int)(damageDone * (DefenderRules.SkillActive(owner) ? .3f : .15f)));
        var container = target.GetGlobalNPC<AfflictionGlobalNPC>().Container;
        if (container == null) return;
        AddElement(target, name switch {
            "PhilaeBlade" => DefenderAreaKind.Necrosis,
            "UnderflowBlade" or "SanbyBlade" => DefenderAreaKind.Corrosion,
            "YuBlade" => DefenderAreaKind.Burn,
            _ => DefenderAreaKind.Nervous
        }, amount);
    }

    internal static void AddElement(NPC target, DefenderAreaKind kind, int amount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || amount <= 0) return;
        var container = target.GetGlobalNPC<AfflictionGlobalNPC>().Container;
        if (container == null) return;
        switch (kind)
        {
            case DefenderAreaKind.Necrosis: container.AddAfflictionValue<NecrosisImpairment>(amount); break;
            case DefenderAreaKind.Nervous: container.AddAfflictionValue<NervousImpairment>(amount); break;
            case DefenderAreaKind.Glue: case DefenderAreaKind.Corrosion: container.AddAfflictionValue<CorrosionImpairment>(amount); break;
            case DefenderAreaKind.Burn: container.AddAfflictionValue<BurnImpairment>(amount); break;
        }
    }
}

public sealed class DefenderColdNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal int ChillHits;
    private int coldTime;
    internal void Chill(NPC npc)
    {
        ChillHits = System.Math.Min(2, ChillHits + 1); coldTime = 150;
        if (!npc.boss && npc.realLife < 0) npc.AddBuff(BuffID.Slow, 90);
        if (ChillHits >= 2) npc.AddBuff(BuffID.Frostburn2, 120);
        npc.netUpdate = true;
    }
    public override void PostAI(NPC npc) { if (coldTime > 0 && --coldTime == 0) ChillHits = 0; }
    public override void SendExtraAI(NPC npc, Terraria.ModLoader.IO.BitWriter bitWriter, System.IO.BinaryWriter writer)
    { writer.Write((byte)ChillHits); writer.Write((short)coldTime); }
    public override void ReceiveExtraAI(NPC npc, Terraria.ModLoader.IO.BitReader bitReader, System.IO.BinaryReader reader)
    { ChillHits = reader.ReadByte(); coldTime = reader.ReadInt16(); }
}
