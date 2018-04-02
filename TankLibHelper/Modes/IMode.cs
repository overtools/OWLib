namespace TankLibHelper.Modes {
    public interface IMode {
        ModeResult Run(string[] args);
        string Mode { get; }
    }
}