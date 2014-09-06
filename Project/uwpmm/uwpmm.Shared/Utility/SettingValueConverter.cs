using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kazyx.Uwpmm.Utility
{
    public class SettingValueConverter
    {
        public static int GetSelectedIndex<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Count == 0)
            {
                return 0;
            }
            if (typeof(T) == typeof(string) || typeof(T) == typeof(int))
            {
                for (int i = 0; i < info.Candidates.Count; i++)
                {
                    if (info.Candidates[i].Equals(info.Current))
                    {
                        return i;
                    }
                }
            }
            else if (typeof(T) == typeof(StillImageSize))
            {
                var size = info as Capability<StillImageSize>;
                for (int i = 0; i < info.Candidates.Count; i++)
                {
                    if (size.Candidates[i].AspectRatio == size.Current.AspectRatio
                        && size.Candidates[i].SizeDefinition == size.Current.SizeDefinition)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        public static int GetSelectedIndex(EvCapability info)
        {
            if (info == null || info.Candidate == null)
            {
                return 0;
            }
            return info.CurrentIndex;
        }

        private delegate string NameConverter<T>(T source);

        private static Capability<string> AsDisplayNames<T>(Capability<T> info, NameConverter<T> converter)
        {
            var res = AsDisabledCapability(info);
            if (res != null)
                return res;

            var mCandidates = new List<string>();
            foreach (T val in info.Candidates)
            {
                mCandidates.Add(converter.Invoke(val));
            }
            return new Capability<string>
            {
                Current = converter.Invoke(info.Current),
                Candidates = mCandidates
            };
        }

        private static Capability<string> AsDisabledCapability<T>(Capability<T> info)
        {
            if (info == null || info.Candidates == null || info.Candidates.Count == 0)
            {
                var disabled = SystemUtil.GetStringResource("Disabled");
                var list = new List<string>();
                list.Add(disabled);
                return new Capability<string>
                {
                    Candidates = list,
                    Current = disabled
                };
            }
            return null;
        }

        public static Capability<string> FromSelfTimer(Capability<int> info)
        {
            return AsDisplayNames<int>(info, FromSelfTimer);
        }

        private static string FromSelfTimer(int val)
        {
            if (val == 0) { return SystemUtil.GetStringResource("Off"); }
            else { return val + SystemUtil.GetStringResource("Seconds"); }
        }

        public static Capability<string> FromPostViewSize(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromPostViewSize);
        }

        private static string FromPostViewSize(string val)
        {
            switch (val)
            {
                case PostviewSizeParam.Px2M:
                    return SystemUtil.GetStringResource("Size2M");
                case PostviewSizeParam.Original:
                    return SystemUtil.GetStringResource("SizeOriginal");
                default:
                    return val;
            }
        }

        public static Capability<string> FromShootMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromShootMode);
        }

        private static string FromShootMode(string val)
        {
            switch (val)
            {
                case ShootModeParam.Movie:
                    return SystemUtil.GetStringResource("ShootModeMovie");
                case ShootModeParam.Still:
                    return SystemUtil.GetStringResource("ShootModeStill");
                case ShootModeParam.Audio:
                    return SystemUtil.GetStringResource("ShootModeAudio");
                case ShootModeParam.Interval:
                    return SystemUtil.GetStringResource("ShootModeIntervalStill");
                default:
                    return val;
            }
        }

        public static Capability<string> FromExposureMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromExposureMode);
        }

        private static string FromExposureMode(string val)
        {
            switch (val)
            {
                case ExposureMode.Aperture:
                    return SystemUtil.GetStringResource("ExposureMode_A");
                case ExposureMode.SS:
                    return SystemUtil.GetStringResource("ExposureMode_S");
                case ExposureMode.Program:
                    return SystemUtil.GetStringResource("ExposureMode_P");
                case ExposureMode.Superior:
                    return SystemUtil.GetStringResource("ExposureMode_sA");
                case ExposureMode.Intelligent:
                    return SystemUtil.GetStringResource("ExposureMode_iA");
                case ExposureMode.Manual:
                    return SystemUtil.GetStringResource("ExposureMode_M");
                default:
                    return val;
            }
        }

        public static Capability<string> FromSteadyMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromSteadyMode);
        }

        private static string FromSteadyMode(string val)
        {
            switch (val)
            {
                case SteadyMode.On:
                    return SystemUtil.GetStringResource("On");
                case SteadyMode.Off:
                    return SystemUtil.GetStringResource("Off");
                default:
                    return val;
            }
        }

        public static Capability<string> FromBeepMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromBeepMode);
        }

        private static string FromBeepMode(string val)
        {
            switch (val)
            {
                case BeepMode.On:
                    return SystemUtil.GetStringResource("On");
                case BeepMode.Silent:
                    return SystemUtil.GetStringResource("Off");
                case BeepMode.Shutter:
                    return SystemUtil.GetStringResource("BeepModeShutterOnly");
                default:
                    return val;
            }
        }

        public static Capability<string> FromViewAngle(Capability<int> info)
        {
            return AsDisplayNames<int>(info, FromViewAngle);
        }

        private static string FromViewAngle(int val)
        {
            return val + SystemUtil.GetStringResource("ViewAngleUnit");
        }

        public static Capability<string> FromMovieQuality(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromMovieQuality);
        }

        private static string FromMovieQuality(string p)
        {
            return p;
        }

        public static Capability<string> FromStillImageSize(Capability<StillImageSize> info)
        {
            return AsDisplayNames<StillImageSize>(info, FromStillImageSize);
        }

        private static string FromStillImageSize(StillImageSize val)
        {
            return val.SizeDefinition + " (" + val.AspectRatio + ")";
        }

        private static readonly char[] StillImageSizeIndicators = { '(', ')' };

        public static StillImageSize ToStillImageSize(string val)
        {
            var array = val.Split(StillImageSizeIndicators);
            if (array == null || array.Length != 2)
            {
                throw new ArgumentException("Failed to convert " + val + " to StillImageSize");
            }
            return new StillImageSize
            {
                AspectRatio = array[1].Trim(),
                SizeDefinition = array[2].Trim()
            };
        }

        public static Capability<string> FromWhiteBalance(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromWhiteBalance);
        }

        private static string FromWhiteBalance(string val)
        {
            switch (val)
            {
                case WhiteBalanceMode.Fluorescent_WarmWhite:
                    return SystemUtil.GetStringResource("WB_Fluorescent_WarmWhite");
                case WhiteBalanceMode.Fluorescent_CoolWhite:
                    return SystemUtil.GetStringResource("WB_Fluorescent_CoolWhite");
                case WhiteBalanceMode.Fluorescent_DayLight:
                    return SystemUtil.GetStringResource("WB_Fluorescent_DayLight");
                case WhiteBalanceMode.Fluorescent_DayWhite:
                    return SystemUtil.GetStringResource("WB_Fluorescent_DayWhite");
                case WhiteBalanceMode.Incandescent:
                    return SystemUtil.GetStringResource("WB_Incandescent");
                case WhiteBalanceMode.Shade:
                    return SystemUtil.GetStringResource("WB_Shade");
                case WhiteBalanceMode.Auto:
                    return SystemUtil.GetStringResource("WB_Auto");
                case WhiteBalanceMode.Cloudy:
                    return SystemUtil.GetStringResource("WB_Cloudy");
                case WhiteBalanceMode.DayLight:
                    return SystemUtil.GetStringResource("WB_DayLight");
                case WhiteBalanceMode.Manual:
                    return SystemUtil.GetStringResource("WB_ColorTemperture");
            }
            return val;
        }

        public static List<string> FromExposureCompensation(EvCapability info)
        {
            if (info == null)
            {
                var disabled = SystemUtil.GetStringResource("Disabled");
                var list = new List<string>();
                list.Add(disabled);
                return list;
            }

            int num = info.Candidate.MaxIndex + Math.Abs(info.Candidate.MinIndex) + 1;
            var mCandidates = new List<string>(num);
            for (int i = 0; i < num; i++)
            {
                Debug.WriteLine("ev: " + i);
                mCandidates.Add(FromExposureCompensation(i + info.Candidate.MinIndex, info.Candidate.IndexStep));
            }

            return mCandidates;
        }

        private static string FromExposureCompensation(int index, EvStepDefinition def)
        {
            var value = EvConverter.GetEv(index, def);
            var strValue = Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");

            if (value <= 0)
            {
                return "EV " + strValue;
            }
            else
            {
                return "EV +" + strValue;
            }
        }

        public static Capability<string> FromFlashMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromFlashMode);
        }

        private static string FromFlashMode(string val)
        {
            switch (val)
            {
                case FlashMode.Auto:
                    return SystemUtil.GetStringResource("FlashMode_Auto");
                case FlashMode.On:
                    return SystemUtil.GetStringResource("On");
                case FlashMode.Off:
                    return SystemUtil.GetStringResource("Off");
                case FlashMode.RearSync:
                    return SystemUtil.GetStringResource("FlashMode_RearSync");
                case FlashMode.SlowSync:
                    return SystemUtil.GetStringResource("FlashMode_SlowSync");
                case FlashMode.Wireless:
                    return SystemUtil.GetStringResource("FlashMode_Wireless");
            }
            return val;
        }

        public static Capability<string> FromFocusMode(Capability<string> info)
        {
            return AsDisplayNames<string>(info, FromFocusMode);
        }

        private static string FromFocusMode(string val)
        {
            switch (val)
            {
                case FocusMode.Continuous:
                    return SystemUtil.GetStringResource("FocusMode_AFC");
                case FocusMode.Single:
                    return SystemUtil.GetStringResource("FocusMode_AFS");
                case FocusMode.Manual:
                    return SystemUtil.GetStringResource("FocusMode_Manual");
            }
            return val;
        }
    }
}
