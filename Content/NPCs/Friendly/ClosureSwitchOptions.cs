using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Content.NPCs.Friendly
{
	// 「切换」按钮弹出的可点击选项。
	//
	// 原版城镇 NPC 只有两个聊天按钮（SetChatButtons 的那两个 ref 参数），没有现成的"多选一"界面；
	// 但 tModLoader 把 NPC 对话正文改成了经 ChatManager 解析出来的 TextSnippet 列表
	// （对话显示缓存的 PrepareCache 走 Utils.WordwrapStringSmart），并且每帧对鼠标悬停的那一个
	// snippet 调用 OnHover()/OnClick()（见 tML 对 Main 的 #FixNPCChat 改动）。
	// 于是只要注册一个自定义聊天标签，就能让对话正文里的文字变成可点击的选项：
	// 既不用自制 UI 面板，也不会和原版那两个聊天按钮抢鼠标。
	//
	// 标签名的合法字符只有字母（原版正则 (?<tag>[a-zA-Z]{1,10})），所以用 "closure"。

	/// <summary>[closure/序号:显示名] 里的一个选项：悬停高亮，点击即切到对应页面。</summary>
	internal sealed class ClosureSwitchOptionSnippet : TextSnippet
	{
		private static readonly Color IdleColor = new(120, 200, 255);
		private static readonly Color HoverColor = new(255, 225, 120);

		private readonly int _option;

		// 悬停高亮的时机：OnHover() / OnClick() 是 tModLoader 在"画完对话文本之后"才调用的，
		// 也就是说本帧取色时还看不到本帧的悬停结果，记录帧号再比对相等会永远差一帧、高亮不出来。
		// 所以这里只记录"最近一次被悬停的帧号"，取色时看它离当前帧有多近：
		// 0~1 帧内算悬停中（0 是同一帧内的第二次取色，1 是下一帧的正常情况），
		// 鼠标移开后 OnHover() 不再被调用，差值自然变大就恢复原色，不需要额外的清理时机。
		private uint _lastHoverTick;

		public ClosureSwitchOptionSnippet(string text, int option) : base(text, IdleColor) {
			_option = option;
		}

		public override void OnHover() {
			_lastHoverTick = Main.GameUpdateCount;
			Main.instance.MouseText(Language.GetTextValue(
				"Mods.ArknightsMod.NPCs.Closure.Buttons.SwitchHint", Closure.GetSwitchOptionLabel(_option)), 0, 0);
		}

		// 用减法比较而不是直接比大小：GameUpdateCount 是 uint，重进世界后一旦回绕变小，
		// 差值会变成一个极大的数，只会判定成"没悬停"，不会误亮。
		public override Color GetVisibleColor() => Main.GameUpdateCount - _lastHoverTick <= 1u ? HoverColor : IdleColor;

		// 原版 Utils.WordwrapStringSmart 在拆行/折行时都用 CopyMorph 复制片段：
		// 每个 '\n' 会把该 snippet 的文本切下一段并结束当前行（菜单"一行一个选项"就靠它），
		// 某一行超过 460px 时也会把超宽的那一段切开。
		// 不覆写的话，切出来的片段会退回成普通 TextSnippet、点不动；这里保留同一个选项号，切成几段都能点。
		public override TextSnippet CopyMorph(string newText) => new ClosureSwitchOptionSnippet(newText, _option);

		public override void OnClick() => Closure.SelectSwitchOption(_option);
	}

	/// <summary>解析 [closure/序号:显示名]，序号对应 <see cref="Closure.ButtonCount"/>。</summary>
	internal sealed class ClosureSwitchOptionTagHandler : ITagHandler
	{
		public TextSnippet Parse(string text, Color baseColor, string options) {
			// 序号认得出来才做成可点选项；认不出来就退回普通文本，
			// 保证菜单文本万一写坏也只是"点不动"，不会报错也不会崩对话。
			if (!int.TryParse(options, out int option) || option < 0 || option >= Closure.SwitchOptionCount)
				return new TextSnippet(text, baseColor);
			return new ClosureSwitchOptionSnippet(text, option);
		}
	}

	[Autoload(Side = ModSide.Client)]
	public sealed class ClosureSwitchOptionSystem : ModSystem
	{
		// 纯客户端 UI：选项只出现在本机玩家的对话框里，服务器不需要注册这个聊天标签。
		public override void Load() => ChatManager.Register<ClosureSwitchOptionTagHandler>(Closure.SwitchOptionTag);
	}
}
