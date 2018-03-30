using System;

namespace CMFLib {
    public interface ICMFProvider {
        byte[] Key(CMFHeaderCommon header, string name, byte[] digest, int length);
        byte[] IV(CMFHeaderCommon header, string name, byte[] digest, int length);
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CMFMetadataAttribute : Attribute {
        public bool AutoDetectVersion = true;
        public CMFApplication App = CMFApplication.Prometheus;
        public uint[] BuildVersions = new uint[0];
    }
}