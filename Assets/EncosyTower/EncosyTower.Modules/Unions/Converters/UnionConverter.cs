using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace EncosyTower.Modules.Unions.Converters
{
    public static partial class UnionConverter
    {
        private static ConcurrentDictionary<TypeId, object> s_converters;

        static UnionConverter()
        {
            Init();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_converters = new ConcurrentDictionary<TypeId, object>();

            TryRegisterGeneratedConverters();
            TryRegister(UnionConverterString.Default);
            TryRegister(UnionConverterObject.Default);
        }

        static partial void TryRegisterGeneratedConverters();

        public static bool TryRegister<T>(IUnionConverter<T> converter)
        {
            ThrowIfNullOrSizeOfTIsBiggerThanUnionDataSize(converter);

            return s_converters.TryAdd(TypeId.Get<T>(), converter);
        }

        public static IUnionConverter<T> GetConverter<T>()
        {
            if (s_converters.TryGetValue(TypeId.Get<T>(), out var candidate))
            {
                if (candidate is IUnionConverter<T> converterT)
                {
                    return converterT;
                }
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return UnionConverterObject<T>.Default;
            }

            return UnionConverterUndefined<T>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Union ToUnion<T>(T value)
            => GetConverter<T>().ToUnion(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Union<T> ToUnionT<T>(T value)
            => GetConverter<T>().ToUnionT(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(in Union union)
            => GetConverter<T>().GetValue(union);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(in Union union, out T result)
            => GetConverter<T>().TryGetValue(union, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySetValueTo<T>(in Union union, ref T dest)
            => GetConverter<T>().TrySetValueTo(union, ref dest);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString<T>(in Union union)
            => GetConverter<T>().ToString(union);

        [HideInCallstack, DoesNotReturn, Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void ThrowIfNullOrSizeOfTIsBiggerThanUnionDataSize<T>(IUnionConverter<T> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            var type = typeof(T);

            if (type.IsValueType == false)
            {
                return;
            }

            var sizeOfT = UnsafeUtility.SizeOf(type);

            if (sizeOfT > UnionData.BYTE_COUNT)
            {
                throw new NotSupportedException(
                    $"The size of {typeof(T)} is {sizeOfT} bytes, " +
                    $"while a Union can only store {UnionData.BYTE_COUNT} bytes of custom data. " +
                    $"To enable the automatic conversion between {typeof(T)} and {typeof(Union)}, " +
                    $"please {GetDefineSymbolMessage(sizeOfT)}"
                );
            }

            static string GetDefineSymbolMessage(int size)
            {
                var longCount = (int)Math.Ceiling((double)size / UnionData.SIZE_OF_LONG);
                var nextSize = longCount * UnionData.SIZE_OF_LONG;

                if (size > UnionData.MAX_BYTE_COUNT)
                {
                    return $"contact the author to increase the maximum size of Union type to {nextSize} bytes " +
                        $"(currently it is {UnionData.MAX_BYTE_COUNT} bytes).";
                }
                else
                {
                    return $"define UNION_{nextSize}_BYTES, or UNION_{longCount * 2}_INTS, or UNION_{longCount}_LONGS.";
                }
            }
        }
    }
}
