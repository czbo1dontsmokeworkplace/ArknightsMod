using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.Damage
{
    //示例在690行附近
    public class DamageCategoryNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public float artsResistance = 0;
        public float elemResistance = 0;

        public byte DamageGenre = 0; 

        private const byte PHYS_FLAG = 0x1;
        private const byte ARTS_FLAG = 0x2;
        private const byte ELEM_FLAG = 0x4;
        private const byte TRUE_FLAG = 0x8;
        private const byte ORIG_FLAG = 0x10;

        public float baseDamage;
        //GetDamage原版函数的baseDamage，当有其他伤害时，由于finalDamage是求和，baseDamage造成的实际伤害写回这里

        public Vector4 PhysDamages;
        public Vector4 ArtsDamages;
        public Vector4 ElemDamages;
        public Vector4 TrueDamages;
        //这四个向量运算并行，计算造成的实际伤害后存回向量

        public Vector4 DamageVarianceVector;

        public float PhysFrag = 1f;
        public float ArtsFrag = 1f;
        public float ElemFrag = 1f;
        public float TrueFrag = 1f; //脆弱
        public Vector4 CriticalCoefficientVector;
        public int CriticalChance = 0; 

        public DamageClass DamageType; //orig dmgType

    }

    public class DamageCategorySystem : ModSystem
    {

        [ThreadStatic] private static NPC _currentNPC;
        [ThreadStatic] private static DamageClass _currentDamageClass;

        /* 暂时给钩子注释了
         * public override void Load()
        {
            IL_NPC.GetIncomingStrikeModifiers += InjectDamageType;
            IL_NPC.HitModifiers.GetDamage += GetDamageInject;
            IL_NPC.HitModifiers.ToHitInfo += InjectDamageTypeRestore;
        }
        public override void Unload()
        {
            IL_NPC.GetIncomingStrikeModifiers -= InjectDamageType; 
            IL_NPC.HitModifiers.GetDamage -= GetDamageInject;
            IL_NPC.HitModifiers.ToHitInfo -= InjectDamageTypeRestore;
        }
		*/
        public DamageCategorySystem()
        {
            //缓存反射以加速游戏启动
        }
        private void InjectDamageType(ILContext il)
        {

            var _IL = new ILCursor(il);

            _IL.Emit(OpCodes.Ldarg_0); // NPC remember
            _IL.Emit(OpCodes.Ldarg_1); //暂时占用damageType
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("SaveDamageContext", System.Reflection.BindingFlags.Public | BindingFlags.Static));


            if (_IL.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg1()
                ))
            {
                _IL.Emit(OpCodes.Ldarg_0); // NPC

                // Checked Here, is NPC

                _IL.Emit(OpCodes.Call, typeof(NPC).GetMethods()
                    .Where(method => method.Name == "GetGlobalNPC" && method.IsGenericMethod && method.GetParameters().Length == 0).First()
                    .MakeGenericMethod(typeof(DamageCategoryNPC)));


                _IL.Emit(OpCodes.Starg_S,(byte)1);

            }
            if (_IL.TryGotoNext(MoveType.Before, i => i.MatchRet()))
            {

                _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ClearDamageContext", BindingFlags.Public | BindingFlags.Static));
            }

        }

        public static void SaveDamageContext(NPC npc, DamageClass damageType)
        {
            _currentNPC = npc;
            _currentDamageClass = damageType;

            if(npc.TryGetGlobalNPC(out DamageCategoryNPC genreNPC)) 
            {
                genreNPC.DamageType = damageType;
            }
        }
        public static DamageClass GetSavedDamageType() => _currentDamageClass;
        public static void ClearDamageContext()
        {
            _currentNPC = null;
            _currentDamageClass = null;
        }

        private void InjectDamageTypeRestore(ILContext il)
        {
            var _IL = new ILCursor(il);
            while (il.Body.Variables.Count <= 7)
            {
                il.Body.Variables.Add(new VariableDefinition(il.Module.ImportReference(typeof(int))));
            }
            
            if (_IL.TryGotoNext(MoveType.After,
                i => i.MatchCall(typeof(NPC.HitInfo).GetConstructor(Type.EmptyTypes))))
            {
                _IL.Emit(OpCodes.Ldarg_0);
                _IL.Emit(OpCodes.Ldarg_1);
                _IL.Emit(OpCodes.Ldarg_2);
                _IL.Emit(OpCodes.Ldarg, 4);
                _IL.Emit(OpCodes.Ldarg, 5);
                _IL.Emit(OpCodes.Call, typeof(NPC.HitModifiers).GetMethod("GetDamage"));
                _IL.Emit(OpCodes.Stloc_S, (byte)7);


            }

            if(_IL.TryGotoNext(MoveType.Before,
                i => i.MatchCall(typeof(NPC.HitModifiers).GetMethod("GetDamage"))))
            {
                _IL.Remove();
                _IL.Emit(OpCodes.Pop);
                _IL.Emit(OpCodes.Pop);
                _IL.Emit(OpCodes.Pop);
                _IL.Emit(OpCodes.Pop);
                _IL.Emit(OpCodes.Pop);
                _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            }

        }

        private void GetDamageInject(ILContext il)
        {
        
            var _IL = new ILCursor(il);

            var locVars = il.Body.Variables;
            if (locVars.Count < 17)
            {
                var Vars = new Type[]
                {
                    typeof(DamageCategoryNPC),
                    typeof(byte),
                    typeof(Vector4),
                    typeof(float),
                    typeof(float),
                    typeof(int),
                    typeof(Vector4),
                    typeof(Vector4),
                    typeof(Vector4),
                    typeof(int),
                };
                for(int i = locVars.Count; i < 17; i++)
                {
                    Type varType = (i >= 7 && i <= 16) ? Vars[i - 7] : typeof(object);
                    locVars.Add(new VariableDefinition(il.Module.ImportReference(varType)));
                }
            }



            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Call, typeof(NPC.HitModifiers).GetMethod("get_DamageType"));

            _IL.Emit(OpCodes.Isinst, typeof(DamageCategoryNPC));

           
            //ALL SAFE HERE
            _IL.Emit(OpCodes.Stloc_S,(byte)7);


            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("DamageType"));
            _IL.Emit(OpCodes.Call, typeof(NPC.HitModifiers).GetMethod("set_DamageType"));


            GetDamageEmit(_IL);


        }
        private void GetDamageEmit(ILCursor _IL)
        {

            //var skipPhysicDmgCalc = _IL.DefineLabel();
            var PhysOnly = _IL.DefineLabel();
            var All = _IL.DefineLabel();

            var withOrig = _IL.DefineLabel();
            var end = _IL.DefineLabel();



            _IL.Emit(OpCodes.Ldloc_S, (byte)7); //gNPC

            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("DamageGenre")); //Genre

            _IL.Emit(OpCodes.Dup);
            _IL.Emit(OpCodes.Stloc_S, (byte)8); //Genre

			_IL.Emit(OpCodes.Ldloc_S, (byte)7); // 加载 gNPC
			_IL.Emit(OpCodes.Ldc_I4_0); // 加载 0
			_IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("DamageGenre"));

			_IL.Emit(OpCodes.Ldc_I4_0);
            _IL.Emit(OpCodes.Ceq);
            _IL.Emit(OpCodes.Brtrue, PhysOnly); //tells if physic damage only

            //EMPTY !!!
            //NO BUGS EVERYTHING OK HERE

            _IL.MarkLabel(All);

            SharedModifiersEmit(_IL); // flat scaling targetMulti in 8 9 10


            VariationVectorEmit(_IL); // variation in 12

            _IL.Emit(OpCodes.Ldc_R4, 0f);
            _IL.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor(new[] { typeof(float) }));  // TOTAL DMG


            VectorDamageEmitInJumpTable(_IL);


            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("VectorSum", BindingFlags.NonPublic | BindingFlags.Static));


            _IL.Emit(OpCodes.Conv_I4);
            _IL.Emit(OpCodes.Ldc_I4_1);
            _IL.Emit(OpCodes.Call, typeof(Math).GetMethod("Max", new[] { typeof(int), typeof(int) }));


            _IL.Emit(OpCodes.Stloc_S, (byte)16); //ttl dmg

            _IL.Emit(OpCodes.Ldloc_S, (byte)8);
            _IL.Emit(OpCodes.Ldc_I4,0x10);
            _IL.Emit(OpCodes.And);
            _IL.Emit(OpCodes.Ldc_I4_0);
            _IL.Emit(OpCodes.Ceq);
            _IL.Emit(OpCodes.Brfalse, withOrig);


            ////recycle NPC => put DamageType back
            _IL.Emit(OpCodes.Ldloc_S, (byte)16);
            _IL.Emit(OpCodes.Ret);


            _IL.MarkLabel(withOrig);
            _IL.Emit(OpCodes.Ldarg_0); // modifiers
            _IL.Emit(OpCodes.Dup); // modifiers modifiers
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("FinalDamage")); // modifiers finalDmg
            _IL.Emit(OpCodes.Dup); //modifiers finalDmg finalDmg
            _IL.Emit(OpCodes.Ldloc_S, (byte)16);
            _IL.Emit(OpCodes.Stfld, typeof(StatModifier).GetField("Flat"));

            //modifiers finalDmg
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("PhysFrag"));
            _IL.Emit(OpCodes.Call, typeof(StatModifier).GetMethod("op_Multiply",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(StatModifier), typeof(float) },
                null)); //HItModifiers newStatModifer

            _IL.Emit(OpCodes.Stfld, typeof(NPC.HitModifiers).GetField("FinalDamage"));


			_IL.MarkLabel(PhysOnly);
        }
        private void VectorDamageEmitInJumpTable(ILCursor _IL)
        {
            var JumpTable = new ILLabel[16]; //仅执行一次
            for(int i = 0; i < 16; i++)
            {
                JumpTable[i] = _IL.DefineLabel();
            }
            var EndTable = _IL.DefineLabel();


            _IL.Emit(OpCodes.Ldloc_S, (byte)8);
            _IL.Emit(OpCodes.Switch, JumpTable);

            //0x00 是原版纯物理
            _IL.MarkLabel(JumpTable[0x00]);

            _IL.MarkLabel(JumpTable[0x01]);
            PhysDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x02]);
            ArtsDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x03]);
            PhysDamageEmit(_IL);
            ArtsDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x04]);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x05]);
            PhysDamageEmit(_IL);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x06]);
            ArtsDamageEmit(_IL);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x07]);
            PhysDamageEmit(_IL);
            ArtsDamageEmit(_IL);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x08]);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x09]);
            PhysDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0A]);
            ArtsDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0B]);
            PhysDamageEmit(_IL);
            ArtsDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0C]);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0D]);
            PhysDamageEmit(_IL);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0E]);
            ArtsDamageEmit(_IL);
            ElemDamageEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击
            _IL.Emit(OpCodes.Br, EndTable);

            _IL.MarkLabel(JumpTable[0x0F]);
            PhysDamageEmit(_IL);
            ArtsDamageEmit(_IL);
            ElemDamageEmit(_IL);
            CriticalCoeffEmit(_IL);
            TrueDamageEmit(_IL); //真伤没有暴击

            _IL.MarkLabel(EndTable);

        }
        private void SharedModifiersEmit(ILCursor _IL)
        {

            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("FlatBonusDamage"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("GetAddableFloatValue", BindingFlags.NonPublic | BindingFlags.Static));
            _IL.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor(new[] { typeof(float) }));
            _IL.Emit(OpCodes.Stloc_S, (byte)9); //flat in 9


            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("ScalingBonusDamage"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("GetAddableFloatValue", BindingFlags.NonPublic | BindingFlags.Static));
            _IL.Emit(OpCodes.Ldc_R4, 1f);
            _IL.Emit(OpCodes.Add);
            _IL.Emit(OpCodes.Stloc_S, (byte)10); //scaling in 10

            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("TargetDamageMultiplier"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("GetAddableFloatValue", BindingFlags.NonPublic | BindingFlags.Static));
            _IL.Emit(OpCodes.Stloc_S, (byte)11); //targetDamageMultiplier in 11

        }
        private static float GetAddableFloatValue(AddableFloat addableFloat) => addableFloat.Value;
        private static float GetMultipliableFloatValue(MultipliableFloat multipliableFloat) => multipliableFloat.Value;

        private void VariationVectorEmit(ILCursor _IL)
        {

            var noVariation = _IL.DefineLabel();
            var endVariation = _IL.DefineLabel();

            _IL.Emit(OpCodes.Ldarg_3);
            _IL.Emit(OpCodes.Brfalse, noVariation);

            _IL.Emit(OpCodes.Ldc_R4, 15f);
            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("DamageVariationScale"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("GetMultipliableFloatValue", BindingFlags.NonPublic | BindingFlags.Static));
            _IL.Emit(OpCodes.Mul);
            _IL.Emit(OpCodes.Call, typeof(Math).GetMethod("Round", new[] { typeof(float) }));
            _IL.Emit(OpCodes.Conv_I4);
            _IL.Emit(OpCodes.Ldc_I4_0);
            _IL.Emit(OpCodes.Ldc_I4, 100);
            _IL.Emit(OpCodes.Call, typeof(Utils).GetMethod("Clamp").MakeGenericMethod(typeof(int)));
            _IL.Emit(OpCodes.Stloc_S, (byte)12); //variation percent in loc 12

            _IL.Emit(OpCodes.Ldloc_S, (byte)12);
            _IL.Emit(OpCodes.Ldc_I4_0);
            _IL.Emit(OpCodes.Ble, noVariation);

            _IL.Emit(OpCodes.Ldloc_S, (byte)12);
            _IL.Emit(OpCodes.Ldarg, 4); //luck
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("VariationVector4"));


            _IL.Emit(OpCodes.Stloc_S, (byte)14); //variation multiplier in loc 14
            _IL.Emit(OpCodes.Br, endVariation);

            _IL.MarkLabel(noVariation);
            _IL.Emit(OpCodes.Ldc_R4, 1f);
            _IL.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor(new[] { typeof(float) }));
            _IL.Emit(OpCodes.Stloc_S, (byte)14);

            _IL.MarkLabel(endVariation);

        }
        public static Vector4 VariationVector4(int variationPercent, float luck)
            => new Vector4(
                DamageVar(variationPercent, luck),
                DamageVar(variationPercent, luck + 1f),
                DamageVar(variationPercent, luck + 2f),
                DamageVar(variationPercent, luck + 3f)
                );

        private static float DamageVar(int variationPercent, float luck)
        {
            float variance = Main.rand.Next(-variationPercent, variationPercent + 1);
            if(Main.rand.NextFloat() < luck)
            {
                variance = Math.Max(Main.rand.Next(-variationPercent, variationPercent + 1), variance);
            }
            return variance / 100f + 1f;
        }


        public static Vector4 ApplySourceDamageToVector4(Vector4 damages, StatModifier sourceDamage)
            => new Vector4(
                sourceDamage.ApplyTo(damages.X),
                sourceDamage.ApplyTo(damages.Y),
                sourceDamage.ApplyTo(damages.Z),
                sourceDamage.ApplyTo(damages.W)
                );

        private void SharedModifiersApply(ILCursor _IL) // dmg = dmg * (1 + scale) + flat
        {
            _IL.Emit(OpCodes.Ldloc_S, (byte)10); //scale
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));
            _IL.Emit(OpCodes.Ldloc_S, (byte)9); //flat
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Addition"));

            _IL.Emit(OpCodes.Ldloc_S, (byte)11); // targetDamageMultiplier
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));
            _IL.Emit(OpCodes.Ldloc_S, (byte)14); // variation
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(Vector4) }));
            // REM : CRIT AT FINAL
        }
        private void TrueDamageEmit(ILCursor _IL)
        {
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);

            _IL.Emit(OpCodes.Dup); //后续存回TrueDamages使用
            _IL.Emit(OpCodes.Ldfld,typeof(DamageCategoryNPC).GetField("TrueDamages"));
            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("SourceDamage"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ApplySourceDamageToVector4"));

            SharedModifiersApply(_IL);

            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("TrueFrag"));
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));

            _IL.Emit(OpCodes.Dup);
            _IL.Emit(OpCodes.Stloc_S, (byte)13);

            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("TrueDamages"));
            //写回，消耗掉复制的gNPC
            _IL.Emit(OpCodes.Ldloc_S, (byte)13);
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Addition"));
        }
        private void ArtsDamageEmit(ILCursor _IL)
        {
            _IL.Emit(OpCodes.Ldloc_S, (byte)7); // dmg gNPC

            _IL.Emit(OpCodes.Dup); //dmg gNPC gNPC
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("ArtsDamages")); //dmg gNPC artsDmgs
            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("SourceDamage"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ApplySourceDamageToVector4")); //dmg gNPC artsDmgsApplied

            SharedModifiersApply(_IL);

            _IL.Emit(OpCodes.Ldc_R4, 1f);
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("artsResistance")); //get resistance from globalNPC saved
            _IL.Emit(OpCodes.Sub);  // 1 - resistance
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("ArtsFrag"));
            _IL.Emit(OpCodes.Mul);
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));

            _IL.Emit(OpCodes.Dup); // dmg gNPC artsDmgsFin artsDmgsFin
            _IL.Emit(OpCodes.Stloc_S, (byte)13);

            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("ArtsDamages"));

            _IL.Emit(OpCodes.Ldloc_S, (byte)13);
            _IL.Emit(OpCodes.Call,typeof(Vector4).GetMethod("op_Addition"));
        }
        private void ElemDamageEmit(ILCursor _IL)
        {
            _IL.Emit(OpCodes.Ldloc_S, (byte)7); // dmg gNPC

            _IL.Emit(OpCodes.Dup); //dmg gNPC gNPC
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("ElemDamages")); //dmg gNPC artsDmgs
            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("SourceDamage"));
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ApplySourceDamageToVector4")); //dmg gNPC artsDmgsApplied

            SharedModifiersApply(_IL);

            _IL.Emit(OpCodes.Ldc_R4, 1f);
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("elemResistance")); //get resistance from globalNPC saved
            _IL.Emit(OpCodes.Sub);  // 1 - resistance
            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("ElemFrag"));
            _IL.Emit(OpCodes.Mul);
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));

            _IL.Emit(OpCodes.Dup); // dmg gNPC artsDmgsFin artsDmgsFin
            _IL.Emit(OpCodes.Stloc_S, (byte)13);

            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("ElemDamages"));

            _IL.Emit(OpCodes.Ldloc_S, (byte)13);
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Addition"));
        }
        private void PhysDamageEmit(ILCursor _IL)
        {
            _IL.Emit(OpCodes.Ldloc_S, (byte)7); //dmg gNPC

            _IL.Emit(OpCodes.Dup);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("PhysDamages")); // dmg gNPC physDmgs
            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Ldfld, typeof(NPC.HitModifiers).GetField("SourceDamage")); // dmg gNPC physDmgs SourceDmg
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ApplySourceDamageToVector4")); //dmg gNPC physDmgsApplied

            SharedModifiersApply(_IL);

            _IL.Emit(OpCodes.Ldarg_0);
            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("ApplyDefenseToVector4", BindingFlags.NonPublic | BindingFlags.Static));

            _IL.Emit(OpCodes.Ldloc_S, (byte)7);
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("PhysFrag"));
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(float) }));

            _IL.Emit(OpCodes.Dup);
            _IL.Emit(OpCodes.Stloc_S, (byte)13);

            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("PhysDamages"));

            _IL.Emit(OpCodes.Ldloc_S, (byte)13);
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Addition"));
        }

        private static Vector4 ApplyDefenseToVector4(Vector4 damage, ref NPC.HitModifiers modifiers)
        {
            float defense = Math.Max(modifiers.Defense.ApplyTo(0), 0);
            float armorPenetration = defense * Math.Clamp(modifiers.ScalingArmorPenetration.Value, 0, 1) + modifiers.ArmorPenetration.Value;
            defense = Math.Max(defense - armorPenetration, 0);

            float damageReduction = defense * modifiers.DefenseEffectiveness.Value;
            return new Vector4(
                Math.Max(damage.X - damageReduction, 1f),
                Math.Max(damage.Y - damageReduction, 1f),
                Math.Max(damage.Z - damageReduction, 1f),
                Math.Max(damage.W - damageReduction, 1f)
                );
        }

        private void CriticalCoeffEmit(ILCursor _IL)
        {
            var noCrit = _IL.DefineLabel();
            var endCrit = _IL.DefineLabel();
            _IL.Emit(OpCodes.Ldloc_S, (byte)7); //dmg gNPC
            _IL.Emit(OpCodes.Dup); //dmg gNPC gNPC
            _IL.Emit(OpCodes.Ldfld, typeof(DamageCategoryNPC).GetField("CriticalChance")); //dmg gNPC critChance
            _IL.Emit(OpCodes.Dup); //dmg gNPC critChance critChance
            _IL.Emit(OpCodes.Ldc_I4_0);
            _IL.Emit(OpCodes.Ble_S, noCrit); //dmg gNPC critChance

            _IL.Emit(OpCodes.Call, typeof(DamageCategorySystem).GetMethod("GenerateCritCoeffVector4", BindingFlags.NonPublic | BindingFlags.Static)); //dmg gNPC crit
            _IL.Emit(OpCodes.Dup);
            _IL.Emit(OpCodes.Stloc_S, (byte)15); //dmg gNPC crit
            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("CriticalCoefficientVector"));

            //局部空栈,栈顶是外部的Damage
            _IL.Emit(OpCodes.Ldloc_S, (byte)15); //dmg crit
            _IL.Emit(OpCodes.Call, typeof(Vector4).GetMethod("op_Multiply", new Type[] { typeof(Vector4), typeof(Vector4) }));
            _IL.Emit(OpCodes.Br, endCrit);

            _IL.MarkLabel(noCrit);
            _IL.Emit(OpCodes.Pop);
            _IL.Emit(OpCodes.Ldc_R4, 1f); //dmg gNPC  1f
            _IL.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor(new[] { typeof(float) }));
            _IL.Emit(OpCodes.Stfld, typeof(DamageCategoryNPC).GetField("CriticalCoefficientVector")); //dmg

            _IL.MarkLabel(endCrit);
        }

        private static Vector4 GenerateCritCoeffVector4(int critChance) {
            return new Vector4(
                Main.rand.Next(100) < critChance ? 2f : 1f,
                Main.rand.Next(100) < critChance ? 2f : 1f,
                Main.rand.Next(100) < critChance ? 2f : 1f,
                Main.rand.Next(100) < critChance ? 2f : 1f
                );
        }
        private static float VectorSum(Vector4 vector)
            => vector.X + vector.Y + vector.Z + vector.W;
    }





 //   public class Bababoi : ModItem
 //   {
 //       public override void SetDefaults()
 //       {
 //           Item.width = 32;
 //           Item.height = 32;

 //           Item.useStyle = ItemUseStyleID.Swing;
 //           Item.useTime = 15;
 //           Item.useAnimation = 15;
 //           Item.autoReuse = true;

 //           Item.DamageType = DamageClass.Melee;
            
 //           Item.damage = 27;
 //           Item.knockBack = 6;
 //           Item.crit = 6;
            

 //           Item.value = Item.buyPrice(gold: 10, silver: 50);
 //           Item.rare = ItemRarityID.Green;
 //           Item.UseSound = SoundID.Item1;

 //       }

 //       public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
 //       {
 //           if (target.TryGetGlobalNPC(out DamageCategoryNPC genreNPC))
 //           {

 //               genreNPC.DamageGenre = (byte)0x01 | 0x02;
 //               //类型旗 空旗仅原版 0x01启用物理 0x02启用法术 0x03启用元素 0x04启用真实 0x10启用原版

 //               genreNPC.PhysDamages = new Vector4(40f, 40f, 40f, 40f);

 //               genreNPC.ArtsDamages = new Vector4(40f, 40f, 40f, 40f);
 //               //同分量共享伤害浮动和暴击，原版物理伤害不共享
 //               genreNPC.ArtsFrag = 1;
 //               //当前类型脆弱，基础值为1
 //               genreNPC.artsResistance = 0;
 //               //当前类型抗性，基础值为0
 //               genreNPC.CriticalChance = 10;
 //               //暴击率

 //               //仅当启用原版伤害时SetDefault() Item.damage才有效，其他伤害类型都需要手动赋值
 //           }
 //       }

	//}


	public class DamageGenreProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public byte WeaponGenre = 0;
        public byte DamageGenre = 0; //更改弹射物伤害类型
        public float ArtsAdditional = 0f; 
        public float ElemAdditional = 0f;
        public float TrueAdditional = 0f;

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            var genreNPC = target.GetGlobalNPC<DamageCategoryNPC>();
        }
    }
}



//public class Int32Expand
//{
//    public static void ExpandInt32()
//    {
//        var intassem = AssemblyDefinition.ReadAssembly(typeof(int).Assembly.Location);
//        var intType = intassem.MainModule.Types.FirstOrDefault(t => t.FullName == "System.Int32");

//        intType.Attributes &= ~Mono.Cecil.TypeAttributes.Sealed;
//        var field1 = new FieldDefinition("Int64field1", Mono.Cecil.FieldAttributes.Public, intassem.MainModule.TypeSystem.Int64);
//        var field2 = new FieldDefinition("Int64field2", Mono.Cecil.FieldAttributes.Public, intassem.MainModule.TypeSystem.Int64);
//        intType.Fields.Add(field1);
//        intType.Fields.Add(field2);
//    }
//}
