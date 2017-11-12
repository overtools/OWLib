namespace STULib.Types {
    [STU(0x32A19631)]
    public class STUSoundInfoContainer: STU_C1A2DB26 {  // indirect, parent is referenced
        [STUField(0x4FF98D41, EmbeddedInstance = true)]
        public STUSoundInfo SoundInfo;
    }
}
