
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;
using System.Diagnostics;
using Windows.UI.Xaml;
namespace Kazyx.Uwpmm.DataModel
{
    class LiveviewScreenViewData : ObservableBase
    {
        readonly TargetDevice Device;

        public LiveviewScreenViewData(TargetDevice d)
        {
            Device = d;
            Device.Status.PropertyChanged += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomPositionInCurrentBox");
                NotifyChangedOnUI("ZoomBoxIndex");
                NotifyChangedOnUI("ZoomBoxNum");
            };
            Device.Api.AvailiableApisUpdated += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomInterfacesVisibility");
            };
        }

        public Visibility ZoomInterfacesVisibility
        {
            get
            {
                if (Device.Api.Capability.IsAvailable("actZoom")) { return Visibility.Visible; }
                return Visibility.Collapsed;
            }
        }

        public int ZoomPositionInCurrentBox
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                DebugUtil.Log("Zoom pos " + Device.Status.ZoomInfo.PositionInCurrentBox);
                return Device.Status.ZoomInfo.PositionInCurrentBox;
            }
        }

        public int ZoomBoxIndex
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.CurrentBoxIndex;
            }
        }

        public int ZoomBoxNum
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.NumberOfBoxes;
            }
        }
    }
}
