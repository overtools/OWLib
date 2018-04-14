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
                if (value == true)
                {
                    Properties.Settings.Default.NGDPHost = GetHashCode();
                    Properties.Settings.Default.Save();
                }
                _active = value;
            }
        }

        public PatchHost(string v1, string v2)
        {
            Host = v1;
            Name = v2;
            _active = Properties.Settings.Default.NGDPHost == GetHashCode(); // todo: check 
        }

        public override int GetHashCode()
        {
            return Host.ToLowerInvariant().GetHashCode();
        }
    }
}
