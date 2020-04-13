using Emilie.UWP.Extensions;
using Emilie.UWP.Media;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
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
    public sealed partial class SunsetView : PageBase
    {
        Visual _rootVisual { get; }
        Random _rand { get; }

        CompositionLinearGradientBrush _grad { get; }
        CompositionColorBrush _shad { get; }

        public SunsetView()
        {
            this.InitializeComponent();

            // 1. Init resources
            _rootVisual = this.GetVisual();
            _rand = new Random();
            var c = _rootVisual.Compositor;

            // 2. Create Brushes
            _grad = c.CreateLinearGradientBrush();
            _grad.ColorStops.Add(c.CreateColorGradientStop(0, MediaExtensions.FromHex("#E500780A")));
            _grad.ColorStops.Add(c.CreateColorGradientStop(0.5f, MediaExtensions.FromHex("#E55D8200")));
            _grad.ColorStops.Add(c.CreateColorGradientStop(1, MediaExtensions.FromHex("#E55D780A")));
            _grad.StartPoint = new Vector2(0, 0);
            _grad.EndPoint = new Vector2(0, 0.5f);

            _shad = c.CreateColorBrush();
            _shad.Color = Color.FromArgb(100, 0, 0, 0);
           
            // 3. Create blades
            for (int i = 0; i < 300; i++)
                PlantGrassSeed();

            // 4. Create blur for shadow
            CreateBlurLayer();
        }

        ShapeVisual CreateGrassBlade(Compositor c, CompositionBrush brush)
        {
            var e = c.CreateEllipseGeometry();
            e.Radius = new Vector2(4, 150);
            e.Center = new Vector2(4, 150);
            var shape = c.CreateSpriteShape(e);
            shape.FillBrush = brush;
            var v = c.CreateShapeVisual();
            v.Shapes.Add(shape);
            v.Size = new Vector2(8, 300);

            var cent = CompositionFactory.CreateCenteringExpression(0.5, 0.5);
            v.StartAnimation(cent);

            v.Scale = new Vector3(1f, (float)(_rand.Next(70, 300) / 100d), 1f);

            return v;
        }

        void PlantGrassSeed()
        {
            // 1. Create main grass blade
            var c = _rootVisual.Compositor;

            var shape = CreateGrassBlade(_rootVisual.Compositor, _grad);

            // 2. Set relative positions
            shape.StartAnimation(
                shape.CreateExpressionAnimation("Offset.Y")
                .SetParameter("Root", _rootVisual)
                .SetParameter("me", shape)
                .SetExpression("Root.Size.Y - (me.Size.Y * 0.5f)"));

            var percent = _rand.Next(0, 10000) / 10000d;
            var exp = shape.CreateExpressionAnimation("Offset.X")
                .SetParameter("Root", _rootVisual)
                .SetExpression($"Root.Size.X * {percent.ToString(CultureInfo.InvariantCulture)}");
            shape.StartAnimation(exp);

            // 3. Start animating
            ApplyWind(shape);

            // 4. Add to visual tree
            BladesHost.GetContainerVisual().Children.InsertAtTop(shape);

            // 5. Create the shadow for the blade, that matches it's position.
            var shadow = CreateGrassBlade(c, _shad);
            shadow.RotationAxis = Vector3.UnitZ;
            shadow.Scale = shape.Scale;
            shadow.StartAnimation(
                shadow.CreateExpressionAnimation(nameof(Visual.Offset))
                .SetParameter("Caster", shape)
                .SetExpression("Caster.Offset"));
            shadow.StartAnimation(
                shadow.CreateExpressionAnimation(nameof(Visual.RotationAngleInDegrees))
                .SetParameter("Caster", shape)
                .SetExpression("Caster.RotationAngleInDegrees"));

            // 6. Add shadow to tree.
            ShadowHost.GetContainerVisual().Children.InsertAtTop(shadow);
        }

        void ApplyWind(Visual v)
        {
            var ease = Animator.EaseInOut;
            v.SetRotationAxis(Vector3.UnitZ);

            var ani = v.CreateScalarKeyFrameAnimation(nameof(Visual.RotationAngleInDegrees))
                .AddKeyFrame(.33f, _rand.Next(0, 30) - 15, ease)
                .AddKeyFrame(.66f, _rand.Next(0, 10) - 5, ease)
                .AddKeyFrame(.9f, _rand.Next(0, 15) - 7.5f, ease)
                .SetDuration(3.414)
                .SetIterationBehavior(AnimationIterationBehavior.Forever)
                .SetDirection(Windows.UI.Composition.AnimationDirection.AlternateReverse);
            
            v.StartAnimation(ani);

            if (v.TryGetAnimationController(nameof(Visual.RotationAngleInDegrees)) is AnimationController c)
            {
                c.Progress = _rand.Next(1, 100) / 100f;
            }
        }

        void CreateBlurLayer()
        {
            var brush = CompositionFactory.CreateBlurEffectBrush(_rootVisual.Compositor).SetBlurAmount(8);

            var sprite = _rootVisual.Compositor.CreateSpriteVisual().LinkSize(this);
            sprite.Brush = brush;

            ShadowHost.GetContainerVisual().Children.InsertAtTop(sprite);
        }



    }
}
