using System;

namespace TankView.ViewResources
{
    public class PatchHost
    {
        public string Host { get; set; }
        public string Name { get; set; }

        private bool _active { get; set; }
        public bool Active { get {
                return _active;
            } set {
                _active = value;
            }
        }

        public PatchHost(string v1, string v2)
        {
            Host = v1;
            Name = v2;
            _active = (null as bool?) ?? false; // todo: check 
        }

        public override int GetHashCode()
        {
            return Host.ToLowerInvariant().GetHashCode();
        }
    }
}
