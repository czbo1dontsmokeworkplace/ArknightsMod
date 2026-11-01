using System.Runtime.CompilerServices;
using ArknightsMod.Systems.Gameplay.Elemental;
using ArknightsMod.Systems;
using System.Runtime.InteropServices;
using System;
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ElementalData() {

	public EntityType EntityType;
	private readonly ushort[] _maxElementals;//四种最大值
	private readonly ushort[] _elementals; //[n, c, b, n]
	private byte _minElementalIndex;
	private ushort _minElementalValue;
	private ushort _recoveryTimer;
	private ushort _whoAmI;

	public byte Status; //用旗表示多种状态
	private const byte _Active = 0x20;
	private const byte _Full = 0x40;
	private const byte _OnElementalBurst = 0x80;

	/// <summary>是否已注册（元素值数组已分配）。</summary>
	public bool IsInitialized => _elementals != null && _maxElementals != null;


	public ElementalData(EntityType entityType, ushort whoAmI) : this() {
		EntityType = entityType;
		_whoAmI = whoAmI;
		ushort maxValue = entityType switch {
			EntityType.Player => (ushort)EntityMaxElemental.Player,
			EntityType.NPC => (ushort)EntityMaxElemental.NPC,
			EntityType.Enemy => (ushort)EntityMaxElemental.Enemy,
			EntityType.Boss => (ushort)EntityMaxElemental.Boss,
			_ => 2000
		};
		_maxElementals = new ushort[4];
		for (int i = 0; i < _maxElementals.Length; i++)
			_maxElementals[i] = maxValue; //假设四种最大元素值都相同，传入实体类型对应的最大元素值
		_elementals = (ushort[])_maxElementals.Clone();
		Status = _Active;
		UpdateStatus();
	}

	public ElementalData(EntityType entityType, ushort whoAmI, ushort[] initialValues) : this() {
		EntityType = entityType;
		_whoAmI = whoAmI;
		ushort maxValue = entityType switch {
			EntityType.Player => (ushort)EntityMaxElemental.Player,
			EntityType.NPC => (ushort)EntityMaxElemental.NPC,
			EntityType.Enemy => (ushort)EntityMaxElemental.Enemy,
			EntityType.Boss => (ushort)EntityMaxElemental.Boss,
			_ => 2000
		};
		_maxElementals = new ushort[4];
		for (int i = 0; i < _maxElementals.Length; i++)
			_maxElementals[i] = maxValue;
		_elementals = (ushort[])initialValues.Clone();
		Status = _Active;
		UpdateStatus();
	}
	//初始化时移除所有时钟


	public unsafe byte UpdateStatus() {
		// 未注册时跳过。
		if (!IsInitialized)
			return Status;
		if ((_minElementalValue = _elementals[findMin()]) <= 0) { //这里第一次记录最小值，如果爆条，所有元素值回复到最大值，那么最低元素应该是满值，返回满状态
			Status |= 0x80;
			// 处理器数量少于元素种类，越界时跳过。
			if ((uint)_minElementalIndex < (uint)ElementalSystem._burstHandlers.Length)
				ElementalSystem._burstHandlers[_minElementalIndex].OnBurst(_whoAmI,EntityType);
			MegaTimer.AddTimer((int)_whoAmI, &ElementalSystem.OffElementalBurst,600, true);
			//off elemental burst should be localized, just in case
            //这里写元素损伤爆条
		}
		
		
		return Status = (byte)(((_minElementalValue = _elementals[findMin()]) == _maxElementals[0] ? 0x40 : 0x00) | (Status & ~0x40));//每种元素最大值不同就不能这么写了

	}
	
	private int findMin() {
		int minIndex = 0;
		
		for (int i = 1; i < _elementals.Length; i++)
		    if (_elementals[i] <= 0) {
				minIndex = i;
				Status |= _OnElementalBurst;
				break;
			}else if (_elementals[i] < _elementals[minIndex]) {
                minIndex = i;
			}	
		return _minElementalIndex = (byte)minIndex;
	}

	//接口
	public bool IsActive => (Status & _Active) == 1;
	public bool IsFull => (Status & _Full) == 1;
	public bool OnElementalBurst => (Status & _Full) == 1;
	public ElementalType MinElementalType => (ElementalType)_minElementalIndex;//当前最小元素值的种类

	[MethodImpl(MethodImplOptions.AggressiveInlining)]//当前最小元素值
	public ushort GetElemental() => IsInitialized ? _elementals[_minElementalIndex] : (ushort)0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]//指定元素值
	public ushort GetElemental(ElementalType elementalType) => IsInitialized ? _elementals[(byte)elementalType] : (ushort)0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]//全部四种元素值
	public ushort[] GetElementals() => _elementals ?? System.Array.Empty<ushort>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyDamage(ElementalType elementalType,int dmg) {
		if (!IsInitialized)
			return;
		byte elemType = (byte)elementalType;
		_elementals[elemType] = (ushort)Math.Max(0, _elementals[elemType] - dmg);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyHealing(ElementalType elementalType, int heal) {
		if (!IsInitialized)
			return;
		byte elemType = (byte)elementalType;
		_elementals[elemType] = (ushort)Math.Min(_maxElementals[elemType], _elementals[elemType] + heal);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyHealingAll(int heal) {
		if (!IsInitialized)
			return;
		for (int i = 0; i < _elementals.Length; i++) {
			_elementals[i] = (ushort)Math.Min(_maxElementals[i], _elementals[i] + heal);
		}
	}
	
}