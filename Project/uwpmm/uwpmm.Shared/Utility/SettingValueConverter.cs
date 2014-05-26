using Kazyx.RemoteApi;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Resources;

namespace Kazyx.Uwpmm.Utility
{
    public class SettingValueConverter
    {
        public static int GetSelectedIndex<T>(Capability<T> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return 0;
            }
            for (int i = 0; i < info.candidates.Length; i++)
            {
                if (info.candidates[i].Equals(info.current))
                {
                    return i;
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

        public static Capability<string> FromSelfTimer(Capability<int> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromSelfTimer(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromSelfTimer(info.current),
                candidates = mCandidates
            };
        }

        private static string FromSelfTimer(int val)
        {
            if (val == 0) { return ResourceLoader.GetForCurrentView().GetString("Off"); }
            else { return val + ResourceLoader.GetForCurrentView().GetString("Seconds"); }
        }

        public static Capability<string> FromPostViewSize(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromPostViewSize(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromPostViewSize(info.current),
                candidates = mCandidates
            };
        }

        private static string FromPostViewSize(string val)
        {
            switch (val)
            {
                case PostviewSizeParam.Px2M:
                    return ResourceLoader.GetForCurrentView().GetString("Size2M");
                case PostviewSizeParam.Original:
                    return ResourceLoader.GetForCurrentView().GetString("SizeOriginal");
                default:
                    return val;
            }
        }

        public static Capability<string> FromShootMode(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromShootMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromShootMode(info.current),
                candidates = mCandidates
            };
        }

        private static string FromShootMode(string val)
        {
            switch (val)
            {
                case ShootModeParam.Movie:
                    return ResourceLoader.GetForCurrentView().GetString("ShootModeMovie");
                case ShootModeParam.Still:
                    return ResourceLoader.GetForCurrentView().GetString("ShootModeStill");
                case ShootModeParam.Audio:
                    return ResourceLoader.GetForCurrentView().GetString("ShootModeAudio");
                case ShootModeParam.Interval:
                    return ResourceLoader.GetForCurrentView().GetString("ShootModeIntervalStill");
                default:
                    return val;
            }
        }

        public static Capability<string> FromExposureMode(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }

            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromExposureMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromExposureMode(info.current),
                candidates = mCandidates
            };
        }

        private static string FromExposureMode(string val)
        {
            switch (val)
            {
                case ExposureMode.Aperture:
                    return ResourceLoader.GetForCurrentView().GetString("ExposureMode_A");
                case ExposureMode.SS:
                    return ResourceLoader.GetForCurrentView().GetString("ExposureMode_S");
                case ExposureMode.Program:
                    return ResourceLoader.GetForCurrentView().GetString("ExposureMode_P");
                case ExposureMode.Superior:
                    return ResourceLoader.GetForCurrentView().GetString("ExposureMode_sA");
                case ExposureMode.Intelligent:
                    return ResourceLoader.GetForCurrentView().GetString("ExposureMode_iA");
                default:
                    return val;
            }
        }

        public static Capability<string> FromSteadyMode(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }

            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromSteadyMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromSteadyMode(info.current),
                candidates = mCandidates
            };
        }

        private static string FromSteadyMode(string val)
        {
            switch (val)
            {
                case SteadyMode.On:
                    return ResourceLoader.GetForCurrentView().GetString("On");
                case SteadyMode.Off:
                    return ResourceLoader.GetForCurrentView().GetString("Off");
                default:
                    return val;
            }
        }

        public static Capability<string> FromBeepMode(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }

            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromBeepMode(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromBeepMode(info.current),
                candidates = mCandidates
            };
        }

        private static string FromBeepMode(string val)
        {
            switch (val)
            {
                case BeepMode.On:
                    return ResourceLoader.GetForCurrentView().GetString("On");
                case BeepMode.Silent:
                    return ResourceLoader.GetForCurrentView().GetString("Off");
                case BeepMode.Shutter:
                    return ResourceLoader.GetForCurrentView().GetString("BeepModeShutterOnly");
                default:
                    return val;
            }
        }

        public static Capability<string> FromViewAngle(Capability<int> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }
            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromViewAngle(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromViewAngle(info.current),
                candidates = mCandidates
            };
        }

        private static string FromViewAngle(int val)
        {
            return val + ResourceLoader.GetForCurrentView().GetString("ViewAngleUnit");
        }

        public static Capability<string> FromMovieQuality(Capability<string> info)
        {
            if (info == null || info.candidates == null || info.candidates.Length == 0)
            {
                return new Capability<string>
                {
                    candidates = new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") },
                    current = ResourceLoader.GetForCurrentView().GetString("Disabled")
                };
            }

            var mCandidates = new string[info.candidates.Length];
            for (int i = 0; i < info.candidates.Length; i++)
            {
                mCandidates[i] = FromMovieQuality(info.candidates[i]);
            }
            return new Capability<string>
            {
                current = FromMovieQuality(info.current),
                candidates = mCandidates
            };
        }

        private static string FromMovieQuality(string p)
        {
            return p;
        }

        public static string[] FromExposureCompensation(EvCapability info)
        {
            if (info == null)
            {
                return new string[] { ResourceLoader.GetForCurrentView().GetString("Disabled") };
            }

            int num = info.Candidate.MaxIndex + Math.Abs(info.Candidate.MinIndex) + 1;
            var mCandidates = new string[num];
            for (int i = 0; i < num; i++)
            {
                Debug.WriteLine("ev: " + i);
                mCandidates[i] = FromExposureCompensation(i + info.Candidate.MinIndex, info.Candidate.IndexStep);
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
    }
}
