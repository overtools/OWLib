using System;

namespace CMFLib {
    public interface ICMFProvider {
        byte[] Key(CMFHeader header, string name, byte[] digest, int length);
        byte[] IV(CMFHeader header, string name, byte[] digest, int length);
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CMFMetadataAttribute : Attribute {
        public bool AutoDetectVersion = true;
        public CMFApplication App = CMFApplication.Prometheus;
        public uint[] BuildVersions = new uint[0];
    }
}