using Emilie.UWP.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using XamlFun.Common;

namespace XamlFun.Views
{
    public sealed partial class DepthTest : PageBase
    {
        public DepthTest()
        {
            this.InitializeComponent();
        }

        private void AnimateWrapper(double depth = -600, TimeSpan? stagger = null)
        {
            wrapper.Children.Clear();

            wrapper.Width = 170 * 3;

            foreach (var i in Enumerable.Range(0, 9))
            {
                wrapper.Children.Add(new Rectangle
                {
                    Margin = new Thickness(8),
                    Height = 150,
                    Width = 150,
                    Fill = new SolidColorBrush(Utils.GetRandomColor())
                });
            }

            StoryboardFactory.CreateDepth3DIn(wrapper.Children.Cast<FrameworkElement>().OrderBy(f => Guid.NewGuid()), wrapper, depth, customStagger: stagger).Begin();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AnimateWrapper(stagger: TimeSpan.FromMilliseconds(50));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AnimateWrapper(600, TimeSpan.FromMilliseconds(25));
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            StoryboardFactory.CreateDepth3DOut(wrapper.Children.Cast<FrameworkElement>().OrderBy(f => Guid.NewGuid()), wrapper, 300).Begin();
        }
    }
}
