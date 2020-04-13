using Emilie.UWP.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml;

namespace XamlFun.Common
{
    public class Animator
    {
        public static CompositionEasingFunction EaseInOut { get; }

        static Animator()
        {
            EaseInOut = Window.Current.Compositor.CreateCubicBezierEasingFunction(0.45f, 0f, 0.55f, 1f);
        }

    }
}
