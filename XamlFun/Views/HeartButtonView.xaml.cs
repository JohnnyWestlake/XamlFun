using Emilie.Core.Numerics;
using Emilie.UWP.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace XamlFun.Views
{
    public sealed partial class HeartButtonView : Page
    {

        Random _rand { get; } = new Random();
        CompositionEasingFunction _easeInOut { get; }
        CompositionEasingFunction _linear { get; }

        public HeartButtonView()
        {
            this.InitializeComponent();
            var c = this.GetVisual().Compositor;
            _easeInOut = c.CreateCubicBezierEasingFunction(0.45f, 0f, 0.55f, 1f);
            _linear = c.CreateLinearEasingFunction();
        }




        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 11; i++)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };


                var heartLeft = new Image
                {
                    Height = 50,
                    Width = 24,
                    Source = (SvgImageSource)this.Resources["LeftHeart"]
                };

                var heartRight = new Image
                {
                    Height = 50,
                    Width = 24,
                    Source = (SvgImageSource)this.Resources["LeftHeart"],
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new CompositeTransform {  ScaleX = -1, ScaleY = 1 },
                    Margin = new Thickness(-1, 0, 0, 0)
                };

                panel.Children.Add(heartLeft);
                panel.Children.Add(heartRight);
                Root.Children.Add(panel);

                var c = CompositionFactory.CreateCenteringExpression(1, 0.5);

                //var hl = heartLeft.EnableCompositionTranslation().GetVisual();
                //hl.StartAnimation(c);
                //hl.StartAnimation(CreateFlap(heartLeft));

                //var hr = heartRight.EnableCompositionTranslation().GetVisual();
                //hr.StartAnimation(c);
                //hr.StartAnimation(CreateFlap(heartRight));

                panel.EnableCompositionTranslation();
                PlayDie(panel, _rand.Next(2, 30) / 100d);

                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await Task.Delay(1000);
                    Root.Children.Remove(panel);
                });
            }


        }



        public void PlayDie(FrameworkElement e, double duration)
        {
            var v = e.GetVisual();

            var group = v.Compositor.CreateAnimationGroup();
            group.Add(
                v.CreateVector3KeyFrameAnimation(nameof(Visual.Scale))
                    .SetDuration(duration)
                    .AddKeyFrame(0f, 0f)
                    .AddKeyFrame(0.33f, 1f));

            group.Add(CreateTranslationX(e));
            group.Add(CreateTranslationY(e));

            v.StartAnimation(group);
        }

        public ScalarKeyFrameAnimation CreateTranslationX(FrameworkElement e)
        {
            var v = e.EnableCompositionTranslation().GetVisual();
            var cin = CompositionFactory.GetEasingFunction(v.Compositor, PennerType.Exponential, PennerVariation.EaseOut);

            var ani =
                v.CreateScalarKeyFrameAnimation("Translation.X")
                    .AddKeyFrame(0f, 0)
                    .AddKeyFrame(1f, _rand.Next(-200, 200), cin)
                    .SetDuration(1);

            return ani;
        }

        public ScalarKeyFrameAnimation CreateTranslationY(FrameworkElement e)
        {
            var v = e.EnableCompositionTranslation().GetVisual();

            var t = _rand.Next(-100, 0);
            var cin = CompositionFactory.GetEasingFunction(v.Compositor, PennerType.Circle, PennerVariation.EaseIn);
            var cout = CompositionFactory.GetEasingFunction(v.Compositor, PennerType.Circle, PennerVariation.EaseIn);

            var ani =
                v.CreateScalarKeyFrameAnimation("Translation.Y")
                    .AddKeyFrame(0f, 0)
                    .AddKeyFrame(0.33f, t, cin)
                    .AddKeyFrame(1f, _rand.Next(t, 10), cout)
                    .SetDuration(1);

            return ani;
        }

        public ScalarKeyFrameAnimation CreateFlap(FrameworkElement e)
        {
            var v = e.GetVisual();

            var ani =
                v.CreateScalarKeyFrameAnimation(nameof(Visual.RotationAngleInDegrees))
                    .AddKeyFrame(0f, 0f)
                    .AddKeyFrame(.5f, 60f)
                    .AddKeyFrame(1f, 0f)
                    .SetIterationBehavior(AnimationIterationBehavior.Forever)
                    .SetDuration(0.3);

            return ani;
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            // Prepare hand-off visual
            ((Button)sender).GetVisual();
        }
    }
}
