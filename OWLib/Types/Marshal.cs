using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    public class ArrayMarshalerI<T> : ICustomMarshaler {
        private readonly IntPtr[] pointers;

        public static ICustomMarshaler GetInstance(string @null) {
            return new ArrayMarshalerI<T>();
        }

        public void CleanUpManagedData(object ManagedObj) {
        }

        public void CleanUpNativeData(IntPtr pNativeData) {
            Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize() {
            return sizeof(uint) + Marshal.SizeOf<T>();
        }

        public IntPtr MarshalManagedToNative(object ManagedObj) {
            if (ManagedObj == null) {
                return IntPtr.Zero;
            }

            T[] array = (T[])ManagedObj;
            int elementSize = Marshal.SizeOf<T>();
            int size = sizeof(uint) + elementSize * array.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.WriteInt32(ptr, 0, array.Length);
            for (int i = 0; i < array.Length; ++i) {
                Marshal.StructureToPtr(array[i], ptr + sizeof(int) + elementSize * i, false);
            }
            return ptr;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData) {
            if (pNativeData == IntPtr.Zero) {
                return null;
            }

            uint length = (uint)Marshal.ReadInt32(pNativeData, 0);
            T[] array = new T[length];
            int elementSize = Marshal.SizeOf<T>();
            for (int i = 0; i < length; ++i) {
                array[i] = Marshal.PtrToStructure<T>(pNativeData + sizeof(int) + elementSize * i);
            }
            return array;
        }
    }
    
    public class ArrayMarshalerB<T> : ICustomMarshaler {
        private readonly IntPtr[] pointers;

        public static ICustomMarshaler GetInstance(string @null) {
            return new ArrayMarshalerB<T>();
        }

        public void CleanUpManagedData(object ManagedObj) {
        }

        public void CleanUpNativeData(IntPtr pNativeData) {
            Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize() {
            return sizeof(byte) + Marshal.SizeOf<T>();
        }

        public IntPtr MarshalManagedToNative(object ManagedObj) {
            if (ManagedObj == null) {
                return IntPtr.Zero;
            }

            T[] array = (T[])ManagedObj;
            int elementSize = Marshal.SizeOf<T>();
            int size = sizeof(byte) + elementSize * array.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.WriteByte(ptr, 0, (byte)array.Length);
            for (int i = 0; i < (byte)array.Length; ++i) {
                Marshal.StructureToPtr(array[i], ptr + sizeof(byte) + elementSize * i, false);
            }
            return ptr;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData) {
            if (pNativeData == IntPtr.Zero) {
                return null;
            }

            byte length = Marshal.ReadByte(pNativeData, 0);
            T[] array = new T[length];
            int elementSize = Marshal.SizeOf<T>();
            for (int i = 0; i < length; ++i) {
                array[i] = Marshal.PtrToStructure<T>(pNativeData + sizeof(byte) + elementSize * i);
            }
            return array;
        }
    }
}