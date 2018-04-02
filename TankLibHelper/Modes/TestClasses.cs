namespace TankLibHelper.Modes {
    public class TestClasses : IMode {
        public string Mode => "testclasses";

        public ModeResult Run(string[] args) {
            return ModeResult.Fail;
        }
    }
}