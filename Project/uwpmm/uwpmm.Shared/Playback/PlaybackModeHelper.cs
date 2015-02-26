﻿using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.Playback
{
    public class PlaybackModeHelper
    {
        public static async Task<bool> MoveToShootingModeAsync(TargetDevice device, int timeoutMsec = 10000)
        {
            return await MoveToSpecifiedModeAsync(device, CameraFunction.RemoteShooting, EventParam.Idle, timeoutMsec);
        }

        public static async Task<bool> MoveToContentTransferModeAsync(TargetDevice device, int timeoutMsec = 10000)
        {
            return await MoveToSpecifiedModeAsync(device, CameraFunction.ContentTransfer, EventParam.ContentsTransfer, timeoutMsec);
        }

        private static async Task<bool> MoveToSpecifiedModeAsync(TargetDevice device, string nextFunction, string nextState, int timeoutMsec, bool isFirst = true)
        {
            var tcs = new TaskCompletionSource<bool>();
            var ct = new CancellationTokenSource(timeoutMsec); // State change timeout 10 sec.
            ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            PropertyChangedEventHandler status_observer = (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "Status":
                        var current = (sender as CameraStatus).Status;
                        if (nextState == current)
                        {
                            DebugUtil.Log("Camera state changed to " + nextState + " successfully.");
                            tcs.TrySetResult(true);
                            return;
                        }
                        else if (EventParam.NotReady != current)
                        {
                            DebugUtil.Log("Unfortunately camera state changed to " + current);
                            tcs.TrySetResult(false);
                            return;
                        }
                        DebugUtil.Log("It might be in transitioning state...");
                        break;
                    default:
                        break;
                }
            };

            DebugUtil.Log("Check current function at first...");
            var getCurrentFailed = false;
            try
            {
                var already = await CheckCurrentFunction(device.Api.Camera, nextFunction).ConfigureAwait(false);
                if (already)
                {
                    DebugUtil.Log("Already in specified mode: " + nextFunction);
                    return true;
                }
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to get current state: " + e.code);
                if (isFirst)
                {
                    getCurrentFailed = true;
                }
                else
                {
                    DebugUtil.Log("No recovery for second trial");
                    return false;
                }
            }

            if (getCurrentFailed)
            {
                DebugUtil.Log("Maybe in movie streaming mode...");
                try
                {
                    await device.Api.AvContent.StopStreamingAsync().ConfigureAwait(false);
                    DebugUtil.Log("Successfully stopped movie streaming mode");
                }
                catch { }
                DebugUtil.Log("Let's retry state transition");
                return await MoveToSpecifiedModeAsync(device, nextFunction, nextState, timeoutMsec, false);
            }

            try
            {
                device.Status.PropertyChanged += status_observer;
                await device.Api.Camera.SetCameraFunctionAsync(nextFunction).ConfigureAwait(false);
                return await tcs.Task;
            }
            catch (RemoteApiException e)
            {
                if (e.code == StatusCode.IllegalState)
                {
                    DebugUtil.Log("SetCameraFunction IllegalState: Already in specified mode");
                    return true;
                }
                DebugUtil.Log("Failed to change camera state: " + e.code);
            }
            finally
            {
                device.Status.PropertyChanged -= status_observer;
            }

            DebugUtil.Log("Check current function again...");
            try
            {
                return await CheckCurrentFunction(device.Api.Camera, nextFunction).ConfigureAwait(false); ;
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to get current state: " + e.code);
                return false;
            }
        }

        private static async Task<bool> CheckCurrentFunction(CameraApiClient camera, string nextFunction)
        {
            var current = await camera.GetCameraFunctionAsync().ConfigureAwait(false);
            DebugUtil.Log("Current state is : " + current);
            return nextFunction == current;
        }

        public static async Task<string> PrepareMovieStreamingAsync(AvContentApiClient av, string contentUri)
        {
            var uri = await av.SetStreamingContentAsync(new PlaybackContent
                {
                    Uri = contentUri,
                    RemotePlayType = RemotePlayMode.SimpleStreaming
                }).ConfigureAwait(false);
            await av.StartStreamingAsync().ConfigureAwait(false);
            return uri.Url;
        }

        public static async Task PauseMovieStreamingAsync(AvContentApiClient av, MoviePlaybackData status)
        {
            await av.PauseStreamingAsync().ConfigureAwait(false);
        }

        public static async Task StartMovieStreamingASync(AvContentApiClient av, MoviePlaybackData status)
        {
            await av.StartStreamingAsync().ConfigureAwait(false);
        }

        public static async Task SeekMovieStreamingAsync(AvContentApiClient av, MoviePlaybackData status, TimeSpan seekTarget)
        {
            var originalStatus = status.StreamingStatus;

            if (status.StreamingStatus == StreamStatus.Error || status.StreamingStatus == StreamStatus.Invalid) { return; }

            if (status.StreamingStatus == StreamStatus.Started)
            {
                await PauseMovieStreamingAsync(av, status).ConfigureAwait(false);
            }

            await av.SeekStreamingPositionAsync(new PlaybackPosition() { PositionMSec = (int)seekTarget.TotalMilliseconds }).ConfigureAwait(false);

            if (originalStatus == StreamStatus.Started || originalStatus == StreamStatus.PausedByEdge)
            {
                await StartMovieStreamingASync(av, status).ConfigureAwait(false);
            }
        }
    }
}
