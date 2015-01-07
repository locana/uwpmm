﻿using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class StatusObserver
    {
        public StatusObserver(TargetDevice device)
        {
            this.api = device.Api;
            this.target = device.Status;
        }

        private readonly DeviceApiHolder api;

        private readonly CameraStatus target;

        private bool _IsProcessing = false;
        public bool IsProcessing
        {
            private set { _IsProcessing = value; }
            get { return _IsProcessing; }
        }

        private int failure_count = 0;

        private const int RETRY_LIMIT = 3;

        private const int RETRY_INTERVAL_SEC = 3;

        public event Action EndByError;

        private ApiVersion version = ApiVersion.V1_0;

        public async Task<bool> Start()
        {
            DebugUtil.Log("StatusObserver: Start");
            if (IsProcessing)
            {
                DebugUtil.Log("StatusObserver: Already processing");
                return false;
            }

            if (api.Capability.IsSupported("getEvent", "1.3")) { version = ApiVersion.V1_3; }
            else if (api.Capability.IsSupported("getEvent", "1.2")) { version = ApiVersion.V1_2; }
            else if (api.Capability.IsSupported("getEvent", "1.1")) { version = ApiVersion.V1_1; }
            else { version = ApiVersion.V1_0; }

            failure_count = 0;
            if (!await Refresh().ConfigureAwait(false))
            {
                DebugUtil.Log("StatusObserver: Failed to start");
                return false;
            }

            IsProcessing = true;
            PollingLoop();
            return true;
        }

        public void Stop()
        {
            DebugUtil.Log("StatusObserver: Stop");
            IsProcessing = false;
        }

        public async Task<bool> Refresh()
        {
            DebugUtil.Log("StatusObserver: Refresh");
            try
            {
                await Update(await api.Camera.GetEventAsync(false, version)).ConfigureAwait(false);
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("StatusObserver: Refresh failed - " + e.code);
                return false;
            }
            return true;
        }

        private async Task Update(Event status)
        {
            if (status.AvailableApis != null) { api.Capability.AvailableApis = status.AvailableApis; }
            target.IsLiveviewAvailable = status.LiveviewAvailable;
            if (status.ShootModeInfo != null) { target.ShootMode = status.ShootModeInfo; }
            if (status.ExposureMode != null) { target.ExposureMode = status.ExposureMode; }
            if (status.ISOSpeedRate != null) { target.ISOSpeedRate = status.ISOSpeedRate; }
            if (status.ShutterSpeed != null) { target.ShutterSpeed = status.ShutterSpeed; }
            if (status.FNumber != null) { target.FNumber = status.FNumber; }
            if (status.ZoomInfo != null) { target.ZoomInfo = status.ZoomInfo; }
            if (status.PostviewSizeInfo != null) { target.PostviewSize = status.PostviewSizeInfo; }
            if (status.SelfTimerInfo != null) { target.SelfTimer = status.SelfTimerInfo; }
            if (status.BeepMode != null) { target.BeepMode = status.BeepMode; }
            if (status.FlashMode != null) { target.FlashMode = status.FlashMode; }
            if (status.FocusMode != null) { target.FocusMode = status.FocusMode; }
            if (status.ViewAngle != null) { target.ViewAngle = status.ViewAngle; }
            if (status.SteadyMode != null) { target.SteadyMode = status.SteadyMode; }
            if (status.MovieQuality != null) { target.MovieQuality = status.MovieQuality; }
            if (status.TouchAFStatus != null) { target.TouchFocusStatus = status.TouchAFStatus; }
            if (status.ProgramShiftActivated != null) { target.ProgramShiftActivated = status.ProgramShiftActivated.Value; }
            if (status.PictureUrls != null) { target.PictureUrls = status.PictureUrls; }
            if (status.LiveviewOrientation != null) { target.LiveviewOrientation = status.LiveviewOrientation; }
            if (status.EvInfo != null) { target.EvInfo = status.EvInfo; }
            if (status.CameraStatus != null) { target.Status = status.CameraStatus; }
            if (status.StorageInfo != null) { target.Storages = status.StorageInfo; }
            if (status.BatteryInfo != null) { target.BatteryInfo = status.BatteryInfo; }
            if (status.FNumber != null) { target.FNumber = status.FNumber; }
            if (status.ShutterSpeed != null) { target.ShutterSpeed = status.ShutterSpeed; }
            if (status.EvInfo != null) { target.EvInfo = status.EvInfo; }
            if (status.ISOSpeedRate != null) { target.ISOSpeedRate = status.ISOSpeedRate; }
            if (status.RecordingTimeSec >= 0) { target.RecordingTimeSec = status.RecordingTimeSec; }
            if (status.NumberOfShots >= 0) { target.NumberOfShots = status.NumberOfShots; }
            if (status.ContShootingMode != null) { target.ContShootingMode = status.ContShootingMode; }
            if (status.ContShootingSpeed != null) { target.ContShootingSpeed = status.ContShootingSpeed; }
            if (status.ContShootingResult != null) { target.ContShootingResult = status.ContShootingResult; }
            if (status.ZoomSetting != null) { target.ZoomSetting = status.ZoomSetting; }
            if (status.SceneSelection != null) { target.SceneSelection = status.SceneSelection; }
            if (status.TrackingFocusMode != null) { target.TrackingFocus = status.TrackingFocusMode; }
            if (status.TrackingFocusStatus != null) { target.TrackingFocusStatus = status.TrackingFocusStatus; }
            if (status.MovieFormat != null) { target.MovieFormat = status.MovieFormat; }
            if (status.FlipMode != null) { target.FlipMode = status.FlipMode; }
            if (status.IntervalTime != null) { target.FlipMode = status.FlipMode; }
            if (status.ColorSetting != null) { target.ColorSetting = status.ColorSetting; }
            if (status.IrRemoteControl != null) { target.InfraredRemoteControl = status.IrRemoteControl; }
            if (status.TvColorSystem != null) { target.TvColorSystem = status.TvColorSystem; }
            if (status.AutoPowerOff != null) { target.AutoPowerOff = status.AutoPowerOff; }
            if (status.ImageQuality != null) { target.StillQuality = status.ImageQuality; }

            if (status.StillImageSize != null)
            {
                if (status.StillImageSize.CapabilityChanged)
                {
                    try
                    {
                        var size = await api.Camera.GetAvailableStillSizeAsync().ConfigureAwait(false);
                        size.Candidates.Sort(CompareStillSize);
                        target.StillImageSize = size;
                    }
                    catch (RemoteApiException)
                    {
                        DebugUtil.Log("Failed to get still image size capability");
                    }
                }
                else
                {
                    target.StillImageSize.Current = status.StillImageSize.Current;
                    target.StillImageSize = target.StillImageSize;
                }
            }

            if (status.WhiteBalance != null)
            {
                if (status.WhiteBalance.CapabilityChanged)
                {
                    try
                    {
                        var wb = await api.Camera.GetAvailableWhiteBalanceAsync().ConfigureAwait(false);
                        var candidates = new List<string>();
                        var tmpCandidates = new Dictionary<string, int[]>();
                        foreach (var mode in wb.Candidates)
                        {
                            candidates.Add(mode.WhiteBalanceMode);
                            var tmpList = new List<int>();
                            if (mode.Candidates.Count == 3)
                            {
                                for (int i = mode.Candidates[1]; i <= mode.Candidates[0]; i += mode.Candidates[2])
                                {
                                    tmpList.Add(i);
                                }
                            }
                            tmpCandidates.Add(mode.WhiteBalanceMode, tmpList.ToArray());

                            var builder = new StringBuilder();
                            foreach (var val in mode.Candidates)
                            {
                                builder.Append(val).Append(", ");
                            }
                        }

                        target.WhiteBalance = new Capability<string> { Candidates = candidates, Current = wb.Current.Mode };
                        target.ColorTempertureCandidates = tmpCandidates;
                        target.ColorTemperture = wb.Current.ColorTemperature;
                    }
                    catch (RemoteApiException)
                    {
                        DebugUtil.Log("Failed to get white balance capability");
                    }
                }
                else
                {
                    if (status.WhiteBalance != null)
                    {
                        target.WhiteBalance.Current = status.WhiteBalance.Current.Mode;
                    }
                    target.ColorTemperture = status.WhiteBalance.Current.ColorTemperature;
                }
            }
        }

        private async void PollingLoop()
        {
            if (!IsProcessing)
            {
                return;
            }

            try
            {
                OnSuccess(await api.Camera.GetEventAsync(true, version).ConfigureAwait(false));
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        private async void OnSuccess(Event @event)
        {
            failure_count = 0;
            await Update(@event).ConfigureAwait(false);
            PollingLoop();
        }

        private async void OnError(StatusCode code)
        {
            switch (code)
            {
                case StatusCode.Timeout:
                    DebugUtil.Log("GetEvent timeout without any event. Retry for the next event");
                    PollingLoop();
                    return;
                case StatusCode.NotAcceptable:
                case StatusCode.CameraNotReady:
                case StatusCode.IllegalState:
                case StatusCode.ServiceUnavailable:
                case StatusCode.Any:
                    if (failure_count++ < RETRY_LIMIT)
                    {
                        DebugUtil.Log("GetEvent failed - retry " + failure_count + ", status: " + code);
                        await Task.Delay(TimeSpan.FromSeconds(RETRY_INTERVAL_SEC)).ConfigureAwait(false);
                        PollingLoop();
                        return;
                    }
                    break;
                case StatusCode.DuplicatePolling:
                    DebugUtil.Log("GetEvent failed duplicate polling");
                    return;
                default:
                    DebugUtil.Log("GetEvent failed with code: " + code);
                    break;
            }

            DebugUtil.Log("StatusObserver Error limit");

            if (IsProcessing)
            {
                Stop();
                EndByError.Raise();
            }
        }

        private static int CompareStillSize(StillImageSize x, StillImageSize y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            if (!x.SizeDefinition.EndsWith("M") || !y.SizeDefinition.EndsWith("M"))
            {
                var comp = x.SizeDefinition.CompareTo(y.SizeDefinition);
                if (comp == 0)
                {
                    return x.AspectRatio.CompareTo(y.AspectRatio);
                }
                else
                {
                    return comp;
                }
            }

            var xv = (int)double.Parse(x.SizeDefinition.Substring(0, x.SizeDefinition.Length - 1)) * 100;
            var yv = (int)double.Parse(y.SizeDefinition.Substring(0, y.SizeDefinition.Length - 1)) * 100;

            if (xv == yv)
            {
                return x.AspectRatio.CompareTo(y.AspectRatio);
            }
            else
            {
                return xv < yv ? 1 : -1;
            }
        }
    }
}
