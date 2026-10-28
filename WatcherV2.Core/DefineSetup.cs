using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Core
{
    public enum LocalizationType
    {
        LocalTypeDefault = 0,
        LocalTypeENUS,
        LocalTypeKOKR
    }

    public enum PermitShutdownTime
    {
        Seconds_30 = 0,
        Minute_1,
        Minutes_2,
        Minutes_3,
        Minutes_4,
        Minutes_5,
        Minutes_6,
        Minutes_7,
        Minutes_8,
        Minutes_9,
        Minutes_10,
    }

    public enum DispWithRatio
    {
        Disp_20P = 1,
        Disp_30P,
        Disp_40P,
    }

    public enum DefineHours
    {
        Hours_00 = 0,
        Hours_01,
        Hours_02,
        Hours_03,
        Hours_04,
        Hours_05,
        Hours_06,
        Hours_07,
        Hours_08,
        Hours_09,
        Hours_10,
        Hours_11,
        Hours_12,
        Hours_13,
        Hours_14,
        Hours_15,
        Hours_16,
        Hours_17,
        Hours_18,
        Hours_19,
        Hours_20,
        Hours_21,
        Hours_22,
        Hours_23
    }

    public enum DefineMins
    {
        Mins_00 = 0,
        Mins_01,
        Mins_02,
        Mins_03,
        Mins_04,
        Mins_05,
        Mins_06,
        Mins_07,
        Mins_08,
        Mins_09,
        Mins_10,
        Mins_11,
        Mins_12,
        Mins_13,
        Mins_14,
        Mins_15,
        Mins_16,
        Mins_17,
        Mins_18,
        Mins_19,
        Mins_20,
        Mins_21,
        Mins_22,
        Mins_23,
        Mins_24,
        Mins_25,
        Mins_26,
        Mins_27,
        Mins_28,
        Mins_29,
        Mins_30,
        Mins_31,
        Mins_32,
        Mins_33,
        Mins_34,
        Mins_35,
        Mins_36,
        Mins_37,
        Mins_38,
        Mins_39,
        Mins_40,
        Mins_41,
        Mins_42,
        Mins_43,
        Mins_44,
        Mins_45,
        Mins_46,
        Mins_47,
        Mins_48,
        Mins_49,
        Mins_50,
        Mins_51,
        Mins_52,
        Mins_53,
        Mins_54,
        Mins_55,
        Mins_56,
        Mins_57,
        Mins_58,
        Mins_59
    }

    public enum DefineSeconds
    {
        Secs_00 = 0,
        Secs_01,
        Secs_02,
        Secs_03,
        Secs_04,
        Secs_05,
        Secs_06,
        Secs_07,
        Secs_08,
        Secs_09,
        Secs_10,
        Secs_11,
        Secs_12,
        Secs_13,
        Secs_14,
        Secs_15,
        Secs_16,
        Secs_17,
        Secs_18,
        Secs_19,
        Secs_20,
        Secs_21,
        Secs_22,
        Secs_23,
        Secs_24,
        Secs_25,
        Secs_26,
        Secs_27,
        Secs_28,
        Secs_29,
        Secs_30,
        Secs_31,
        Secs_32,
        Secs_33,
        Secs_34,
        Secs_35,
        Secs_36,
        Secs_37,
        Secs_38,
        Secs_39,
        Secs_40,
        Secs_41,
        Secs_42,
        Secs_43,
        Secs_44,
        Secs_45,
        Secs_46,
        Secs_47,
        Secs_48,
        Secs_49,
        Secs_50,
        Secs_51,
        Secs_52,
        Secs_53,
        Secs_54,
        Secs_55,
        Secs_56,
        Secs_57,
        Secs_58,
        Secs_59
    }

    public enum LogLv
    {
        Debug = 1,
        Info,
        Warn,
        Error,
        Fatal
    }
}
