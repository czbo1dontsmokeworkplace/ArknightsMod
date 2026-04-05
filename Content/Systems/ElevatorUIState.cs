using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using ArknightsMod.Content.Tiles;

namespace ArknightsMod.Content.Systems
{
	internal class ElevatorUIState : UIState
	{
		private const bool ElevatorUiTextEnabled = false;
		// 楼层按钮列（右下角，无背景）
		private UIPanel _floorPanel;
		private UIList _floorList;

		// 设置窗口（屏幕中心，可关闭）
		private UIPanel _settingsPanel;
		private UIText _title;
		private UITextPanel<string> _closeButton;
		private UITextPanel<string> _modeButton;
		private UIPanel _debugPanel;
		private UIText _debugText;
		private float _debugScrollOffsetPx;
		private float _debugContentHeightPx;

		private int _targetX;
		private int _targetY;
		private readonly List<int> _floorBottomYs = new List<int>();
		private int _selectedFloorIndex;

		private bool _settingsVisible;
		private bool _debugExpanded = true;
		private string _debugRawText = "";

		private const int FloorPanelWidthPx = 220;
		private const int FloorPanelHeightPx = 300;
		private const int FloorMarginRightPx = 260;
		private const int FloorMarginBottomPx = 70;

		private const int SettingsPanelWidthPx = 340;
		private const int SettingsPanelHeightPx = 220;
		private const int ModeTopPx = 18;
		private const int SettingsPanelYOffsetPx = 18;
		private const int DebugCollapsedTopPx = 66;
		private const int DebugCollapsedHeightPx = 28;
		private const int DebugExpandedHeightPx = 102;
		private const float DebugLineHeightPx = 16f;
		private const float DebugScrollStepPx = 20f;

		public int TargetX => _targetX;
		public int TargetY => _targetY;
		public bool IsSettingsVisible => _settingsVisible;
		public bool IsMouseOverFloorPanel => _floorPanel != null && _floorPanel.ContainsPoint(Main.MouseScreen);

		public override void OnInitialize()
		{
			// 右下角楼层按钮列
			_floorPanel = new UIPanel();
			_floorPanel.SetPadding(0);
			_floorPanel.Width.Set(FloorPanelWidthPx, 0f);
			_floorPanel.Height.Set(FloorPanelHeightPx, 0f);
			_floorPanel.Left.Set(-(FloorPanelWidthPx + FloorMarginRightPx), 1f);
			_floorPanel.Top.Set(-(FloorPanelHeightPx + FloorMarginBottomPx), 1f);
			_floorPanel.BackgroundColor = Color.Transparent;
			_floorPanel.BorderColor = Color.Transparent;
			Append(_floorPanel);

			_floorList = new UIList();
			_floorList.Width.Set(0f, 1f);
			_floorList.Height.Set(0f, 1f);
			_floorList.Top.Set(0f, 0f);
			_floorList.ListPadding = 6f;
			_floorPanel.Append(_floorList);

			// 屏幕中心设置窗口
			_settingsPanel = new UIPanel();
			_settingsPanel.SetPadding(10);
			_settingsPanel.Width.Set(SettingsPanelWidthPx, 0f);
			_settingsPanel.Height.Set(SettingsPanelHeightPx, 0f);
			_settingsPanel.Left.Set(-SettingsPanelWidthPx / 2f, 0.5f);
			_settingsPanel.Top.Set(-2000f, 0.5f); // 默认隐藏
			Append(_settingsPanel);

			_title = new UIText("电梯设置");
			_title.Top.Set(0f, 0f);
			_settingsPanel.Append(_title);

			_closeButton = new UITextPanel<string>("X");
			_closeButton.Width.Set(30f, 0f);
			_closeButton.Height.Set(26f, 0f);
			_closeButton.Left.Set(278f, 0f);
			_closeButton.Top.Set(ModeTopPx, 0f);
			_closeButton.OnLeftClick += (_, __) => HideSettings();
			_settingsPanel.Append(_closeButton);

			_modeButton = new UITextPanel<string>("");
			// 向左缩短，给右侧留出关闭按钮位置
			_modeButton.Width.Set(268f, 0f);
			_modeButton.Height.Set(26f, 0f);
			_modeButton.Left.Set(0f, 0f);
			_modeButton.Top.Set(ModeTopPx, 0f);
			_modeButton.OnLeftClick += (_, __) =>
			{
				if (TryGetTargetElevator(out 电梯TE te))
				{
					te.CycleFloorDetectMode();
					te.ScanFloors();
					RefreshModeButtonText();
					RefreshDebugText();
					RebuildFloorButtons();
				}
			};
			_settingsPanel.Append(_modeButton);

			_debugPanel = new UIPanel();
			_debugPanel.SetPadding(6);
			_debugPanel.OverflowHidden = true; // 限制文本只在调试框内显示
			_debugPanel.Left.Set(0f, 0f);
			_debugPanel.Top.Set(DebugCollapsedTopPx, 0f);
			// 右侧留一点空，避免文字贴边/溢出观感很差
			_debugPanel.Width.Set(-8f, 1f);
			_debugPanel.Height.Set(DebugCollapsedHeightPx, 0f);
			_settingsPanel.Append(_debugPanel);

			_debugText = new UIText("", 0.76f);
			_debugText.Left.Set(0f, 0f);
			_debugText.Top.Set(0f, 0f);
			_debugPanel.Append(_debugText);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!_settingsVisible)
				return;

			// 鼠标悬停调试面板时，滚轮滚动调试文字
			if (_debugPanel.ContainsPoint(Main.MouseScreen))
			{
				int wheelDelta = PlayerInput.ScrollWheelDelta / 120;
				if (wheelDelta != 0)
				{
					_debugScrollOffsetPx -= wheelDelta * DebugScrollStepPx;
					ClampDebugScrollOffset();
					ApplyDebugScrollOffset();
				}
			}
		}

		public void SetFloorTarget(int topLeftX, int topLeftY, bool resetSelection)
		{
			_targetX = topLeftX;
			_targetY = topLeftY;
			if (resetSelection)
				_selectedFloorIndex = 0;
			RebuildFloorButtons();
		}

		public void ShowSettings(int topLeftX, int topLeftY)
		{
			_settingsVisible = true;
			_targetX = topLeftX;
			_targetY = topLeftY;
			RefreshModeButtonText();
			RefreshSettingsLayout();
			RebuildFloorButtons();
			RefreshDebugText();
		}

		public void HideSettings()
		{
			_settingsVisible = false;
			RefreshSettingsLayout();
		}

		public void ScrollSelection(int wheelSteps)
		{
			if (_floorBottomYs.Count == 0 || wheelSteps == 0)
				return;
			_selectedFloorIndex -= wheelSteps;
			if (_selectedFloorIndex < 0)
				_selectedFloorIndex = _floorBottomYs.Count - 1;
			else if (_selectedFloorIndex >= _floorBottomYs.Count)
				_selectedFloorIndex = 0;
			RebuildFloorButtons();
		}

		public bool ConfirmSelectedFloor()
		{
			if (_floorBottomYs.Count == 0)
				return false;
			if (!TryGetTargetElevator(out 电梯TE te))
				return false;
			int bottomY = _floorBottomYs[_selectedFloorIndex];
			te.TargetFloorBottomY = bottomY;
			if (ElevatorUiTextEnabled)
				Main.NewText($"[电梯] 已选择楼层 {_selectedFloorIndex + 1} (bottomY={bottomY})");
			return true;
		}

		private void RefreshSettingsLayout()
		{
			float hiddenTop = -2000f;
			_settingsPanel.Top.Set(_settingsVisible ? (-SettingsPanelHeightPx / 2f + SettingsPanelYOffsetPx) : hiddenTop, 0.5f);
			_debugPanel.Height.Set(_debugExpanded ? DebugExpandedHeightPx : DebugCollapsedHeightPx, 0f);
			ClampDebugScrollOffset();
			ApplyDebugScrollOffset();
		}

		private void RefreshModeButtonText()
		{
			if (TryGetTargetElevator(out 电梯TE te))
				_modeButton.SetText($"检测: {电梯TE.GetFloorDetectModeLabel(te.FloorMode)}");
			else
				_modeButton.SetText("检测: 井隙+按钮");
		}

		private static string Ellipsize(string text, int maxChars)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
				return text;
			if (maxChars <= 1)
				return "…";
			return text.Substring(0, maxChars - 1) + "…";
		}

		private static string WrapByChars(string text, int charsPerLine)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			string[] lines = text.Split('\n');
			List<string> outLines = new List<string>();
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (line.Length <= charsPerLine)
				{
					outLines.Add(line);
					continue;
				}
				int start = 0;
				while (start < line.Length)
				{
					int len = Math.Min(charsPerLine, line.Length - start);
					outLines.Add(line.Substring(start, len));
					start += len;
				}
			}
			return string.Join("\n", outLines);
		}

		private void RefreshDebugText()
		{
			if (TryGetTargetElevator(out 电梯TE te))
			{
				te.ScanFloors();
				if (te.DebugLastLeftWallX < 0 || te.DebugLastRightWallX < 0)
				{
					_debugRawText =
						$"井壁: {te.DebugLastLeftWallX}, {te.DebugLastRightWallX}  楼层数: {te.FloorBottomYs.Count}  失败: {te.DebugWallFailReason}\n" +
						$"候选: L={te.DebugBestLeftCandidateX}({te.DebugBestLeftWallScore}/{te.DebugBestLeftInsideAirScore}) R={te.DebugBestRightCandidateX}({te.DebugBestRightWallScore}/{te.DebugBestRightInsideAirScore})";
				}
				else
				{
					_debugRawText =
						$"井壁: {te.DebugLastLeftWallX}, {te.DebugLastRightWallX}  楼层数: {te.FloorBottomYs.Count}\n" +
						$"门候选: {te.DebugDoorCandidateCount}  {te.DebugDoorSample}";
				}
			}
			else
			{
				_debugRawText = "未找到电梯实体";
			}

			string wrapped = WrapByChars(_debugRawText, 34);
			_debugText.SetText(wrapped);

			int lineCount = Math.Max(1, wrapped.Split('\n').Length);
			_debugContentHeightPx = lineCount * DebugLineHeightPx;
			ClampDebugScrollOffset();
			ApplyDebugScrollOffset();
		}

		private void ClampDebugScrollOffset()
		{
			float viewportHeight = _debugPanel.Height.Pixels - 12f; // padding 上下各 6
			float maxOffset = Math.Max(0f, _debugContentHeightPx - viewportHeight);
			if (_debugScrollOffsetPx < 0f)
				_debugScrollOffsetPx = 0f;
			if (_debugScrollOffsetPx > maxOffset)
				_debugScrollOffsetPx = maxOffset;
		}

		private void ApplyDebugScrollOffset()
		{
			_debugText.Top.Set(-_debugScrollOffsetPx, 0f);
		}

		private void RebuildFloorButtons()
		{
			_floorList?.Clear();
			_floorBottomYs.Clear();

			if (!TryGetTargetElevator(out 电梯TE te))
			{
				_floorList.Add(new UITextPanel<string>("未找到电梯"));
				return;
			}
			te.ScanFloors();
			if (te.FloorBottomYs.Count == 0)
			{
				_floorList.Add(new UITextPanel<string>("无楼层"));
				return;
			}

			for (int i = te.FloorBottomYs.Count - 1; i >= 0; i--)
				_floorBottomYs.Add(te.FloorBottomYs[i]);
			if (_selectedFloorIndex < 0 || _selectedFloorIndex >= _floorBottomYs.Count)
				_selectedFloorIndex = 0;

			for (int i = 0; i < _floorBottomYs.Count; i++)
			{
				int bottomY = _floorBottomYs[i];
				int floorIndex = i;
				bool selected = floorIndex == _selectedFloorIndex;

				UITextPanel<string> btn = new UITextPanel<string>($"{(selected ? "> " : "  ")}楼层 {floorIndex + 1}");
				btn.Width.Set(0f, 1f);
				btn.Height.Set(34f, 0f);
				btn.BackgroundColor = selected ? new Color(85, 120, 185) : new Color(63, 82, 151) * 0.6f;
				btn.OnLeftClick += (_, __) =>
				{
					_selectedFloorIndex = floorIndex;
					ConfirmSelectedFloor();
					RebuildFloorButtons();
				};
				_floorList.Add(btn);
			}
		}

		private bool TryGetTargetElevator(out 电梯TE te)
		{
			te = null;
			int id = ModContent.GetInstance<电梯TE>().Find(_targetX, _targetY);
			if (id >= 0 && TileEntity.ByID.TryGetValue(id, out TileEntity entity) && entity is 电梯TE direct)
			{
				te = direct;
				return true;
			}

			// 兜底：移动中的电梯/成帧更新时，TE 坐标可能与 UI 缓存 topLeft 短暂不一致。
			float worldX = (_targetX + 2f) * 16f;
			float worldY = (_targetY + 3.5f) * 16f;
			if (电梯TE.TryFindNearbyElevatorByWorld(worldX, worldY, maxDistanceTiles: 12, out 电梯TE nearTe, out _, out _))
			{
				te = nearTe;
				return true;
			}

			return false;
		}
	}
}
