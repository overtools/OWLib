using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    public class ArrayMarshaler<T, U> : ICustomMarshaler {
        public static ICustomMarshaler GetInstance(string @null) {
            return new ArrayMarshaler<T, U>();
        }

        public void CleanUpManagedData(object ManagedObj) {
        }

        public void CleanUpNativeData(IntPtr pNativeData) {
            Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize() {
            return Marshal.SizeOf<U>() + Marshal.SizeOf<T>();
        }

        public IntPtr MarshalManagedToNative(object ManagedObj) {
            if (ManagedObj == null) {
                return IntPtr.Zero;
            }

            T[] array = (T[])ManagedObj;
            int elementSize = Marshal.SizeOf<T>();
            int indexSize = Marshal.SizeOf<U>();
            int size = indexSize + elementSize * array.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            if (indexSize == 1) {
                Marshal.WriteByte(ptr, 0, (byte)array.Length);
            } else if (indexSize == 2) {
                Marshal.WriteInt16(ptr, 0, (short)array.Length);
            } else if (indexSize == 4) {
                Marshal.WriteInt32(ptr, 0, array.Length);
            } else if (indexSize == 8) {
                Marshal.WriteInt64(ptr, 0, array.Length);
            }
            for (int i = 0; i < array.Length; ++i) {
                Marshal.StructureToPtr(array[i], ptr + indexSize + elementSize * i, false);
            }
            return ptr;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData) {
            if (pNativeData == IntPtr.Zero) {
                return null;
            }
            
            int elementSize = Marshal.SizeOf<T>();
            int indexSize = Marshal.SizeOf<U>();
            int length = 0;
            if (indexSize == 1) {
                length = Marshal.ReadByte(pNativeData, 0);
            } else if (indexSize == 2) {
                length = Marshal.ReadInt16(pNativeData, 0);
            } else if (indexSize == 4) {
                length = Marshal.ReadInt32(pNativeData, 0);
            } else if (indexSize == 8) {
                length = (int) Marshal.ReadInt64(pNativeData, 0);
            }
            T[] array = new T[length];
            for (int i = 0; i < length; ++i) {
                array[i] = Marshal.PtrToStructure<T>(pNativeData + indexSize + elementSize * i);
            }
            return array;
        }
    }
}