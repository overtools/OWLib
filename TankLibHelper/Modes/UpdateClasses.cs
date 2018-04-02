namespace TankLibHelper.Modes {
    public class UpdateClasses : IMode {
        public string Mode => "updateclasses";

        public ModeResult Run(string[] args) {
            return ModeResult.Fail;
        }
    }
}