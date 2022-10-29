using System;

namespace DataTool.ConvertLogic.WEM {
    [Flags]
    enum WAVESpeakerPosition {
        FL  = 1 << 0,     /* front left */
        FR  = 1 << 1,     /* front right */
        FC  = 1 << 2,     /* front center */
        LFE = 1 << 3,     /* low frequency effects */
        BL  = 1 << 4,     /* back left */
        BR  = 1 << 5,     /* back right */
        FLC = 1 << 6,     /* front left center */
        FRC = 1 << 7,     /* front right center */
        BC  = 1 << 8,     /* back center */
        SL  = 1 << 9,     /* side left */
        SR  = 1 << 10,    /* side right */

        TC  = 1 << 11,    /* top center*/
        TFL = 1 << 12,    /* top front left */
        TFC = 1 << 13,    /* top front center */
        TFR = 1 << 14,    /* top front right */
        TBL = 1 << 15,    /* top back left */
        TBC = 1 << 16,    /* top back center */
        TBR = 1 << 17,    /* top back left */
    }
}