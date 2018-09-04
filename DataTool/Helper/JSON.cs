using TankLib;
using TankLib.STU;

namespace DataTool.Helper {
    public static class JSON {
        public static teResourceGUID[] FixArray<T>(teStructuredDataAssetRef<T>[] arr) {
            if (arr == null) return null;
            var ret = new teResourceGUID[arr.Length];
            for (int i = 0; i < arr.Length; i++) {
                ret[i] = arr[i].GUID;
            }

            return ret;
        }
    }
}
