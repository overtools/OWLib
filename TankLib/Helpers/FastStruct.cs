using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace TankLib.Helpers {
    public static class FastStruct<T> where T : struct 
    {
        private delegate T LoadFromByteRefDelegate(ref byte source);
        private delegate void CopyMemoryDelegate(ref T dest, ref byte src, int count);
        private delegate void RevCopyMemoryDelegate(ref byte dest, ref T src, int count);

        private static readonly LoadFromByteRefDelegate LoadFromByteRef = BuildLoadFromByteRefMethod();
        private static readonly CopyMemoryDelegate CopyMemory = BuildCopyMemoryMethod();
        private static readonly RevCopyMemoryDelegate RevCopyMemory = BuildRevCopyMemoryMethod();

        public static readonly int Size = Marshal.SizeOf<T>();

        private static LoadFromByteRefDelegate BuildLoadFromByteRefMethod()
        {
            var methodLoadFromByteRef = new DynamicMethod("LoadFromByteRef<" + typeof(T).FullName + ">",
                typeof(T), new[] { typeof(byte).MakeByRefType() }, typeof(FastStruct<T>));

            ILGenerator generator = methodLoadFromByteRef.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldobj, typeof(T));
            generator.Emit(OpCodes.Ret);

            return (LoadFromByteRefDelegate)methodLoadFromByteRef.CreateDelegate(typeof(LoadFromByteRefDelegate));
        }

        private static CopyMemoryDelegate BuildCopyMemoryMethod()
        {
            var methodCopyMemory = new DynamicMethod("CopyMemory<" + typeof(T).FullName + ">",
                typeof(void), new[] { typeof(T).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }, typeof(FastStruct<T>));

            ILGenerator generator = methodCopyMemory.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Cpblk);
            generator.Emit(OpCodes.Ret);

            return (CopyMemoryDelegate)methodCopyMemory.CreateDelegate(typeof(CopyMemoryDelegate));
        }

        private static RevCopyMemoryDelegate BuildRevCopyMemoryMethod()
        {
            var methodCopyMemory = new DynamicMethod("RevCopyMemory<" + typeof(T).FullName + ">",
                typeof(void), new[] { typeof(byte).MakeByRefType(), typeof(T).MakeByRefType(), typeof(int) }, typeof(FastStruct<T>));

            ILGenerator generator = methodCopyMemory.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Cpblk);
            generator.Emit(OpCodes.Ret);

            return (RevCopyMemoryDelegate)methodCopyMemory.CreateDelegate(typeof(RevCopyMemoryDelegate));
        }

        public static T ArrayToStructure(byte[] src)
        {
            return LoadFromByteRef(ref src[0]);
        }

        public static T[] ReadArray(byte[] source)
        {
            T[] buffer = new T[source.Length / Size];

            if (source.Length > 0)
                CopyMemory(ref buffer[0], ref source[0], source.Length);

            return buffer;
        }

        public static byte[] StructureToArray(T source)
        {
            byte[] buffer = new byte[Size];

            if (buffer.Length > 0)
                RevCopyMemory(ref buffer[0], ref source, buffer.Length);

            return buffer;
        }

        public static byte[] WriteArray(T[] source)
        {
            byte[] buffer = new byte[Size * source.Length];

            if (buffer.Length > 0)
                RevCopyMemory(ref buffer[0], ref source[0], buffer.Length);

            return buffer;
        }
    }
}