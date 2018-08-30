namespace TankView.ViewModel {
    public class ProductLocation {
        public string Label { get; set; }
        public string Value { get; set; }

        public ProductLocation(string v1, string v2) {
            Label = v1;
            Value = v2;
        }

        public override int GetHashCode() {
            return Value.ToLowerInvariant().GetHashCode();
        }
    }
}
