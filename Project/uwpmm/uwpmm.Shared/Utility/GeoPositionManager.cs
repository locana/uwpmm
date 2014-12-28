using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Kazyx.Uwpmm.Utility
{
    class GeopositionManager
    {
        private static GeopositionManager _GeopositionManager = new GeopositionManager();

        internal Geoposition LatestPosition { get; set; }
        internal event EventHandler<GeopositionEventArgs> GeopositionUpdated;

        private Geolocator _Geolocator;
        private DispatcherTimer _Timer;
        private const int AcquiringInterval = 1; // min.
        private const int MaximumAge = 15; // min.
        private const int Timeout = 20; // sec.

        private bool _LocationAllowed = true;
        internal bool LocationAllowed
        {
            set { _LocationAllowed = value; }
            get { return _LocationAllowed; }
        }

        private bool _Enabled = false;
        internal bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (!_Enabled && value)
                {
                    _Enabled = value;
                    Task.Factory.StartNew(async () => // Not to await in the set property
                    {
                        var dispatcher = SystemUtil.GetCurrentDispatcher();
                        if (dispatcher != null)
                        {
                            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Start(); // this must be called on the UI thread.
                            });
                        }
                    });
                }
                else
                {
                    _Enabled = false;
                    Stop();
                }
            }
        }

        private void Stop()
        {
            // _Geolocator.StatusChanged -= geolocator_StatusChanged;
            // _Geolocator.PositionChanged -= geolocator_PositionChanged;
            _Timer.Stop();
            LatestPosition = null;
        }

        private async void Start()
        {
            if (_Timer.IsEnabled)
            {
                await UpdateGeoposition();
                return;
            }
            _Geolocator.DesiredAccuracy = PositionAccuracy.Default;

            //_Geolocator.MovementThreshold = 10;    
            _Geolocator.ReportInterval = 3000;

            _Geolocator.PositionChanged += _Geolocator_PositionChanged;
            _Geolocator.StatusChanged += _Geolocator_StatusChanged;
            // _Geolocator.PositionChanged += geolocator_PositionChanged;
            //await UpdateGeoposition();
            //_Timer.Start();
        }

        void _Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Unauthorized });
                    break;
                case PositionStatus.Initializing:
                case PositionStatus.NoData:
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Acquiring });
                    break;
                case PositionStatus.Ready:
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
                    break;
                default:
                case PositionStatus.NotInitialized:
                case PositionStatus.NotAvailable:
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Failed });
                break;
            }
        }

        void _Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            LatestPosition = args.Position;
            OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
        }

        private async Task UpdateGeoposition()
        {
            DebugUtil.Log("Starting to acquire geo location");
            OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Acquiring });

            IAsyncOperation<Geoposition> locationTask = null;

            try
            {
                locationTask = _Geolocator.GetGeopositionAsync(
                    TimeSpan.FromMinutes(MaximumAge),
                    TimeSpan.FromSeconds(Timeout)
                    );
                LatestPosition = await locationTask;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    DebugUtil.Log("Failed due to permission problem.");
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = null, Status = GeopositiomManagerStatus.Unauthorized });
                }
                DebugUtil.Log("Caught exception from GetGeopositionAsync");
                LatestPosition = null;
            }
            finally
            {
                if (locationTask != null)
                {
                    if (locationTask.Status == AsyncStatus.Started)
                    {
                        locationTask.Cancel();
                    }
                    locationTask.Close();
                }
            }

            if (LatestPosition == null)
            {
                OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = null, Status = GeopositiomManagerStatus.Failed });
            }
            else
            {
                OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
            }
        }

        protected void OnGeopositionUpdated(GeopositionEventArgs e)
        {
            GeopositionUpdated.Invoke(this, e);
        }

        internal async Task<Geoposition> AcquireGeoPosition()
        {
            if (LatestPosition != null)
            {
                return LatestPosition;
            }
            await UpdateGeoposition();
            return LatestPosition;
        }

        private GeopositionManager()
        {
            _Geolocator = new Geolocator();
            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromMinutes(AcquiringInterval);
            //_Timer.Tick += new EventHandler<object>(OnTimerTick);
            _Timer.Tick += _Timer_Tick;
        }

        async void _Timer_Tick(object sender, object e)
        {
            //await UpdateGeoposition();
        }

        public static GeopositionManager GetInstance()
        {
            return _GeopositionManager;
        }
    }

    internal class GeopositionEventArgs : EventArgs
    {
        public Geoposition UpdatedPosition { get; set; }
        public GeopositiomManagerStatus Status { get; set; }
    }

    internal enum GeopositiomManagerStatus
    {
        Unauthorized,
        Failed,
        Acquiring,
        OK,
    };
}
