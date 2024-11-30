using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace EncosyTower.Modules.Types
{
    internal static class TypeIdVault
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Clearly denotes an undefined type")]
        private readonly struct __UndefinedType__ { }

        private static readonly object s_lock = new();
        private static readonly ConcurrentDictionary<uint, Type> s_vault = new();
        private static uint s_current;

        static TypeIdVault()
        {
            Init();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void Init()
        {
            s_vault.TryAdd(TypeId.Undefined._value, UndefinedType);

            _ = Type<bool>.Id;
            _ = Type<byte>.Id;
            _ = Type<sbyte>.Id;
            _ = Type<char>.Id;
            _ = Type<decimal>.Id;
            _ = Type<double>.Id;
            _ = Type<float>.Id;
            _ = Type<int>.Id;
            _ = Type<uint>.Id;
            _ = Type<long>.Id;
            _ = Type<ulong>.Id;
            _ = Type<short>.Id;
            _ = Type<ushort>.Id;
            _ = Type<string>.Id;
            _ = Type<object>.Id;
        }

        public static readonly Type UndefinedType = typeof(__UndefinedType__);

        internal static uint Next
        {
            get
            {
                lock (s_lock)
                {
                    Interlocked.Add(ref UnsafeUtility.As<uint, int>(ref s_current), 1);
                    return s_current;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Register(uint id, Type type)
            => s_vault.TryAdd(id, type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetType(uint id, out Type type)
            => s_vault.TryGetValue(id, out type);

        internal static class Cache<T>
        {
            private static readonly uint s_id;

            public static uint Id
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => s_id;
            }

            static Cache()
            {
#pragma warning disable IDE0002

                s_id = TypeIdVault.Next;

#pragma warning restore
#if UNITY_EDITOR && TYPE_ID_DEBUG_LOG
                EncosyTower.Modules.Logging.DevLoggerAPI.LogInfo(
                    $"{nameof(TypeId)} {s_id} is assigned to {typeof(T)}.\n" +
                    $"If the value is overflowed, enabling Domain Reloading will reset it."
                );
#endif
            }
        }
    }
}
