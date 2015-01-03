using System;

namespace Kazyx.Uwpmm.DataModel
{
    public class MoviePlaybackData : ImageDataSource
    {
        public MoviePlaybackData() { }

        private TimeSpan _CurrentPosition = TimeSpan.FromMilliseconds(0);
        public TimeSpan CurrentPosition
        {
            get { return _CurrentPosition; }
            set
            {
                _CurrentPosition = value;
                NotifyChanged("CurrentPosition");
            }
        }

        private TimeSpan _Duration = TimeSpan.FromMilliseconds(0);
        public TimeSpan Duration
        {
            get { return _Duration; }
            set
            {
                _Duration = value;
                NotifyChanged("Duration");
            }
        }

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set
            {
                if (_FileName != value)
                {
                    _FileName = value;
                    NotifyChanged("FileName");
                }
            }
        }

        private bool _SeekAvailable = false;
        public bool SeekAvailable
        {
            get { return _SeekAvailable; }
            set
            {
                if (_SeekAvailable != value)
                {
                    _SeekAvailable = value;
                    NotifyChanged("SeekAvailable");
                }
            }
        }
    }
}
