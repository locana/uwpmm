using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class ZoomOperation
    {
        public static async Task ZoomOut(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.Action1Shot);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static async Task StartZoomOut(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStart);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static async Task StopZoomOut(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStop);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static async Task ZoomIn(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.Action1Shot);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static async Task StartZoomIn(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStart);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }

        public static async Task StopZoomIn(CameraApiClient camera)
        {
            try
            {
                await camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStop);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }
    }
}
