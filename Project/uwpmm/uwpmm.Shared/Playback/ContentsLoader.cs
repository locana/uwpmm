using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.Playback
{
    public abstract class ContentsLoader
    {
        public event EventHandler<ContentsCollectedEventArgs> PartLoaded;

        protected void OnPartLoaded(IList<Thumbnail> contents)
        {
            PartLoaded.Raise(this, new ContentsCollectedEventArgs(contents));
        }

        public event EventHandler Completed;

        protected void OnCompleted()
        {
            Completed.Raise(this, null);
        }

        public event EventHandler Cancelled;

        protected void OnCancelled()
        {
            Cancelled.Raise(this, null);
        }

        public abstract Task Start(CancellationTokenSource cancel);
    }

    public class ContentsCollectedEventArgs : EventArgs
    {
        public IList<Thumbnail> Contents { get; private set; }

        public ContentsCollectedEventArgs(IList<Thumbnail> contents)
        {
            Contents = contents;
        }
    }
}
