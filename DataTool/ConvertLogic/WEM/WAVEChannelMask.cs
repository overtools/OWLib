namespace DataTool.ConvertLogic.WEM {
    enum WAVEChannelMask {
        MONO = WAVESpeakerPosition.FC,
        STEREO = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR,
        TWOPOINT1 = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.LFE,
        TWOPOINT1_xiph = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC, /* aka 3STEREO? */
        QUAD = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR,
        QUAD_surround = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.BC,
        QUAD_side = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.SL | WAVESpeakerPosition.SR,
        FIVEPOINT0 = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.LFE | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR,
        FIVEPOINT0_xiph = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR,
        FIVEPOINT0_surround = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.SL | WAVESpeakerPosition.SR,
        FIVEPOINT1 = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.LFE | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR,
        FIVEPOINT1_surround = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.LFE | WAVESpeakerPosition.SL | WAVESpeakerPosition.SR,
        SEVENPOINT0 = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.LFE | WAVESpeakerPosition.BC | WAVESpeakerPosition.FLC | WAVESpeakerPosition.FRC,
        SEVENPOINT1 = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.LFE | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR | WAVESpeakerPosition.FLC | WAVESpeakerPosition.FRC,
        SEVENPOINT1_surround = WAVESpeakerPosition.FL | WAVESpeakerPosition.FR | WAVESpeakerPosition.FC | WAVESpeakerPosition.LFE | WAVESpeakerPosition.BL | WAVESpeakerPosition.BR | WAVESpeakerPosition.SL | WAVESpeakerPosition.SR,
    }
}