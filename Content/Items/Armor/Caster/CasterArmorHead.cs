using ArknightsMod.Content.Items.Armor.NeoArmorReforge;

namespace ArknightsMod.Content.Items.Armor.Caster
{
	/// <summary>
	/// 术师干员头部件的公共类型。跨干员效果通过此类型识别职业，
	/// 再由套装注册表验证头、身、腿属于同一套；不需要维护干员名单。
	/// </summary>
	public abstract class CasterArmorHead : NeoArmorReforgeVanityHead
	{
	}
}
