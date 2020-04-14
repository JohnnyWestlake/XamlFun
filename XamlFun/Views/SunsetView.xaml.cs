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

        CompositionEllipseGeometry _geom { get; }
        CompositionSpriteShape _grassShape { get; }
        CompositionSpriteShape _shadowShape { get; }
        ExpressionAnimation _cent { get; }
        ExpressionAnimation _grassOffset { get; }
        ExpressionAnimation _shadowOffset { get; }
        ExpressionAnimation _shadowRotation { get; }

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

            // 3. Create grass geometry & resources
            _geom = c.CreateEllipseGeometry();
            _geom.Radius = new Vector2(4, 150);
            _geom.Center = new Vector2(4, 150);
            _cent = CompositionFactory.CreateCenteringExpression(0.5, 0.5);

            _grassShape = c.CreateSpriteShape(_geom);
            _grassShape.FillBrush = _grad;

            _shadowShape = c.CreateSpriteShape(_geom);
            _shadowShape.FillBrush = _shad;

            _grassOffset = c.CreateExpressionAnimation()
                .SetTarget("Offset")
                .SetParameter("Root", _rootVisual)
                .SetExpression("Vector3(Root.Size.X * percent, Root.Size.Y - (this.Target.Size.Y * 0.5f), 0f)");

            _shadowOffset = c.CreateExpressionAnimation()
                .SetTarget(nameof(Visual.Offset))
                .SetExpression("Caster.Offset");

            _shadowRotation = c.CreateExpressionAnimation()
                .SetTarget(nameof(Visual.RotationAngleInDegrees))
                .SetExpression("Caster.RotationAngleInDegrees");

            // 4. Create blades
            for (int i = 0; i < 250; i++)
                PlantGrassSeed();

            // 5. Create blur for shadow
            CreateBlurLayer();
        }

        ShapeVisual CreateGrassBlade(CompositionSpriteShape shape)
        {
            var v = shape.Compositor.CreateShapeVisual();
            v.Shapes.Add(shape);
            v.Size = new Vector2(8, 300);
            v.Scale = new Vector3(1f, (float)(_rand.Next(70, 300) / 100d), 1f);
            v.StartAnimation(_cent);
            return v;
        }

        void PlantGrassSeed()
        {
            // 1. Create main grass blade
            var shape = CreateGrassBlade(_grassShape);

            // 2. Set relative positions
            var percent = _rand.Next(0, 10000) / 10000d;
            shape.StartAnimation(_grassOffset.SetParameter("percent", percent)); 

            // 3. Start animating
            ApplyWind(shape);

            // 4. Add to visual tree
            BladesHost.GetContainerVisual().Children.InsertAtTop(shape);

            // 5. Create the shadow for the blade
            var shadow = CreateGrassBlade(_shadowShape);
            shadow.RotationAxis = Vector3.UnitZ;
            shadow.Scale = shape.Scale;
            shadow.StartAnimation(_shadowOffset.SetParameter("Caster", shape));
            shadow.StartAnimation(_shadowRotation.SetParameter("Caster", shape));

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
            var sprite = _rootVisual.Compositor.CreateSpriteVisual().LinkSize(this);
            sprite.Brush = CompositionFactory.CreateBlurEffectBrush(_rootVisual.Compositor, 5f);
            ShadowHost.GetContainerVisual().Children.InsertAtTop(sprite);
        }



    }
}
