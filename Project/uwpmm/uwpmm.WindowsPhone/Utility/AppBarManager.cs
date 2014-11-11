using Kazyx.Uwpmm.Control;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.Utility
{
    public class CommandBarManager
    {

        public CommandBarManager()
        {
            this.Init();
        }

        public CommandBar bar = new CommandBar();

        void Init()
        {
            bar = new CommandBar()
            {
                Background = Application.Current.Resources["AppBarBackgroundThemeBrush"] as Brush,
            };
            bar.SecondaryCommands.Add(new AppBarButton() { Label = "Control" });
        }

        enum Item
        {
            ControlPanel,
        }
    }
}
