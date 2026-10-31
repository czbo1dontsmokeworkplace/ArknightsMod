using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster;

// The counter belongs to one item, and a new native use animation starts a
// new volley even when the preceding volley was interrupted. Stored shots
// never enter this counter; their ordinal is already synchronized in ai[0].
public sealed class MysticPhotonVolley : GlobalItem
{
    private int normalShots;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => entity.ModItem is Indigo.IndigoStaff or Harmonie.HarmonieStaff or Ebenholz.EbenholzStaff;
    public override void UseAnimation(Item item, Player player) => normalShots = 0;
    internal static bool NextNormal(Item item)
    {
        var volley = item.GetGlobalItem<MysticPhotonVolley>();
        volley.normalShots = (volley.normalShots + 1) % 3;
        return volley.normalShots == 0;
    }
}
