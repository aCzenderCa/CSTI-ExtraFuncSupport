using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CSTI_LuaActionSupport.Helper
{
    public static class TryModifyPack
    {
        private delegate T _TryNum<out T>(object? o) where T : struct;

        private static readonly Dictionary<Type, Dictionary<Type, MulticastDelegate>> tryNumCache = new();

        private static MulticastDelegate GenTryNum(Type self, Type tIn)
        {
            var dynamicMethod = new DynamicMethod($"TryModifyPack_TryNum_{self.Name}_{tIn.Name}", self,
                new[] {typeof(object)}, typeof(TryModifyPack));
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Unbox_Any, tIn);
            ilGenerator.Emit(FloatLike[self]);
            ilGenerator.Emit(OpCodes.Ret);
            return (MulticastDelegate) dynamicMethod.CreateDelegate(
                typeof(_TryNum<int>).GetGenericTypeDefinition().MakeGenericType(self));
        }

        public static T TryNum<T>(this object? o) where T : struct
        {
            if (o == null) return default;
            var type = o.GetType();
            if (FloatLike.ContainsKey(typeof(T)) && FloatLike.ContainsKey(type))
            {
                if (!tryNumCache.ContainsKey(typeof(T)))
                    tryNumCache[typeof(T)] = new Dictionary<Type, MulticastDelegate>();
                if (!tryNumCache[typeof(T)].TryGetValue(type, out var genTryNum))
                {
                    genTryNum = GenTryNum(typeof(T), type);
                    tryNumCache[typeof(T)][type] = genTryNum;
                }

                return ((_TryNum<T>) genTryNum)(o);
            }

            return default;
        }

        private static readonly Dictionary<Type, OpCode> FloatLike = new()
        {
            {typeof(double), OpCodes.Conv_R8}, {typeof(float), OpCodes.Conv_R4}, {typeof(long), OpCodes.Conv_I8},
            {typeof(ulong), OpCodes.Conv_U8}, {typeof(int), OpCodes.Conv_I4}, {typeof(uint), OpCodes.Conv_U4},
        };

        public static void TryModBy(this ref float self, object? o)
        {
            if (o == null) return;
            var type = o.GetType();
            if (FloatLike.ContainsKey(type))
            {
                self = o.TryNum<float>();
            }
        }

        public static void TryModBy(this ref int self, object? o)
        {
            if (o == null) return;
            var type = o.GetType();
            if (FloatLike.ContainsKey(type))
            {
                self = o.TryNum<int>();
            }
        }
    }
}