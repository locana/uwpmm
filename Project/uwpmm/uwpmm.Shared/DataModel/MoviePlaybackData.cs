using System;

namespace Kazyx.Uwpmm.DataModel
{
    public class MoviePlaybackData : ImageDataSource
    {
        public MoviePlaybackData() { }

        private TimeSpan _CurrentPosition;
        public TimeSpan CurrentPosition
        {
            get { return _CurrentPosition; }
            set
            {
                _CurrentPosition = value;
                NotifyChanged("CurrentPosition");
            }
        }

        private TimeSpan _Duration;
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
    }
}
