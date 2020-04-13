using Emilie.UWP.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace XamlFun.Controls
{
    public class ActiveNavigationTransition
    {
        Storyboard Storyboard { get; set; }
        ICompositionAnimationBase CompositionAnimation { get; set; }
    }

    public enum NavigationAnimationEngine
    { 
        Storyboard,
        Composition
    }

    public partial class NavigationFrame : Frame
    {
        public NavigationAnimationEngine AnimationEngine { get; set; } = NavigationAnimationEngine.Composition;

        Queue<ContentPresenter> _presenters { get; } = new Queue<ContentPresenter>();

        ContentPresenter _newPresenter;
        ContentPresenter _oldPresenter;

        Grid _clientArea = null;

        bool _isForwardNavigation = false;
        Storyboard _currentTransition;


        public NavigationFrame()
        {
            this.DefaultStyleKey = typeof(NavigationFrame);
            this.Navigating += (s, e) =>
            {
                _isForwardNavigation = e.NavigationMode != NavigationMode.Back;
            };
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _clientArea = this.GetTemplateChild("ClientArea") as Grid;

            if (this.Content != null)
                OnContentChanged(null, Content);
        }

        ContentPresenter GetNewPresenter()
        {
            return new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = new Border { Height = 100, Width = 100 }
            };
        }

        public bool SkipTransition { get; internal set; }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            UIElement oldElement = oldContent as UIElement;
            UIElement newElement = newContent as UIElement;

            // Try to force ClientArea to be realized
            //this.ApplyTemplate();

            // Require the appropriate template parts plus a new element to
            // transition to.
            if (_clientArea == null)
            {
                return;
            }

            _oldPresenter = _newPresenter;
            _newPresenter = GetNewPresenter();

            _clientArea.Children.Insert(0, _newPresenter);

            SetOpacity(_newPresenter, 0);
            _newPresenter.Visibility = Visibility.Visible;
            _newPresenter.Content = newElement;

            if (_oldPresenter != null)
            {
                _oldPresenter.GetVisual();
                lock (_presenters)
                    _presenters.Enqueue(_oldPresenter);
                SetOpacity(_oldPresenter, 1);
                _oldPresenter.Visibility = Visibility.Visible;
                _oldPresenter.IsHitTestVisible = false;

                if (oldElement != null)
                    _oldPresenter.Content = oldElement;
            }

            // If we're not playing transitions, get in and out in a flash.
            if (SkipTransition)
            {
                SkipTransition = false;

                {
                    Sb_Completed(null, null);

                    if (_oldPresenter != null)
                    {
                        _clientArea.Children.Remove(_oldPresenter);
                        _oldPresenter.Content = null;
                        _oldPresenter = null;
                    }

                    SetOpacity(_newPresenter, 1);
                }

                return;
            }

            if (oldElement == null)
            {
                SetOpacity(_newPresenter, 1);
            }
            else
            {
                if (AnimationEngine == NavigationAnimationEngine.Composition)
                {
                    SetOpacity(_newPresenter, 1);
                    //  Create a composition scoped batch. This will track when the transition completes
                    Compositor comp = _oldPresenter.GetVisual().Compositor;
                    CompositionScopedBatch batch = _currentBatch = comp.CreateScopedBatch(CompositionBatchTypes.Animation);
                    batch.Completed += Batch_Completed;

                    // Create a start the transition
                    CompositionStoryboard group = _currentGroup = GenerateCompositionTransition(_isForwardNavigation ? NavigationMode.New : NavigationMode.Back, _oldPresenter, _newPresenter);
                    group.Start();

                    // Seal the batch
                    batch.End();
                }
                else
                {
                    Storyboard sb = _currentTransition = GenerateStoryboardTransition(_isForwardNavigation ? NavigationMode.New : NavigationMode.Back, _oldPresenter, _newPresenter);
                    sb.Completed += Sb_Completed;

                    var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    {
                        sb.Begin();
                    });
                }
            }
        }



        static void SetOpacity(FrameworkElement element, double value)
        {
            element.GetVisual().Opacity = (float)value;
        }

        CompositionScopedBatch _currentBatch = null;
        CompositionStoryboard _currentGroup = null;

        private void Batch_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            // 1. Remove event handler from storyboard
            CompositionScopedBatch target = sender as CompositionScopedBatch;
            if (target != null)
            {
                target.Completed -= Batch_Completed;
                target.Dispose();

                _currentGroup?.Dispose();

                // 1.1 If this is the most recent storyboard, allow us to interact with content
                if (target == _currentBatch)
                {
                    RestoreContentPresenterInteractivity(_newPresenter);
                    _newPresenter.IsHitTestVisible = true;
                }
            }
            else
            {
                // if no target, we've manually called this. Make it work.
                _newPresenter.IsHitTestVisible = true;
                RestoreContentPresenterInteractivity(_newPresenter);
            }

            _currentBatch = null;
            _currentGroup = null;

            // Remove the "old" content (i.e. the thing we've animated out) from the VisualTree
            ContentPresenter presenter;
            lock (_presenters)
                presenter = _presenters.Dequeue();

            _clientArea.Children.Remove(presenter);
            presenter.Content = null;
            if (presenter == _oldPresenter)
            {
                _oldPresenter = null;
            }

            presenter = null;
        }

        /// <summary>
        /// XAML is nuts and there is some underlying error with composition / XAML preventing us
        /// from attempting to do *any* workaround to return to cached pages via back during
        /// animation. So we don't. No crash!
        /// </summary>
        /// <returns></returns>
        internal bool ReadyToGoBack()
        {
            return _currentBatch == null;
        }

        private void Sb_Completed(object sender, object e)
        {
            // 1. Remove event handler from storyboard
            Storyboard target = sender as Storyboard;
            if (target != null)
            {
                target.Completed -= Sb_Completed;

                // 1.1 If this is the most recent storyboard, allow us to interact with content
                if (target == _currentTransition)
                {
                    RestoreContentPresenterInteractivity(_newPresenter);
                    _newPresenter.IsHitTestVisible = true;
                }
            }
            else
            {
                // if no target, we've manually called this. Make it work.
                _newPresenter.IsHitTestVisible = true;
                RestoreContentPresenterInteractivity(_newPresenter);
            }

            // Remove the "old" content (i.e. the thing we've animated out) from the VisualTree
            ContentPresenter presenter;
            lock (_presenters)
                presenter = _presenters.Dequeue();

            _clientArea.Children.Remove(presenter);
            presenter.Content = null;
            if (presenter == _oldPresenter)
            {
                _oldPresenter = null;
            }

            presenter = null;
        }



        private static void RestoreContentPresenterInteractivity(FrameworkElement presenter)
        {
            if (presenter != null)
            {
                if (presenter.Opacity != 1)
                {
                    SetOpacity(presenter, 1);
                }
            }
        }











        /// <summary>
        /// When overriden, creates custom storyboard transitions between pages
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="oldPresenter"></param>
        /// <param name="newPresenter"></param>
        /// <returns></returns>
        public virtual Storyboard GenerateStoryboardTransition(
            NavigationMode mode,
            ContentPresenter oldPresenter,
            ContentPresenter newPresenter)
        {
            return new Storyboard();
        }


        /// <summary>
        /// Generates composition navigation transitions
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="oldPresenter"></param>
        /// <param name="newPresenter"></param>
        /// <returns></returns>
        public virtual CompositionStoryboard GenerateCompositionTransition(
            NavigationMode mode,
            ContentPresenter oldPresenter,
            ContentPresenter newPresenter)
        {
            return mode == NavigationMode.Back
                    ? CreateCompositionExpoZoomBackward(oldPresenter, newPresenter)
                    : CreateCompositionExpoZoomForward(oldPresenter, newPresenter);
        }

        #region Default Composition Transitions 

        /// <summary>
        /// Creates the detault Forward composition animation
        /// </summary>
        /// <param name="outElement"></param>
        /// <param name="inElement"></param>
        /// <returns></returns>
        CompositionStoryboard CreateCompositionExpoZoomForward(FrameworkElement outElement, FrameworkElement inElement)
        {
            Compositor compositor = ElementCompositionPreview.GetElementVisual(outElement).Compositor;

            Visual outVisual = ElementCompositionPreview.GetElementVisual(outElement);
            Visual inVisual = ElementCompositionPreview.GetElementVisual(inElement);

            CompositionAnimationGroup outgroup = compositor.CreateAnimationGroup();
            CompositionAnimationGroup ingroup = compositor.CreateAnimationGroup();

            TimeSpan outDuration = TimeSpan.FromSeconds(0.3);
            TimeSpan inStart = TimeSpan.FromSeconds(0.25);
            TimeSpan inDuration = TimeSpan.FromSeconds(0.6);

            CubicBezierEasingFunction ease = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.95f, 0.05f),
                new Vector2(0.79f, 0.04f));

            CubicBezierEasingFunction easeOut = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.13f, 1.0f),
                new Vector2(0.49f, 1.0f));

            // OUT ELEMENT
            {
                outVisual.CenterPoint = outVisual.Size.X > 0
                   ? new Vector3(outVisual.Size / 2f, 0f)
                   : new Vector3((float)this.ActualWidth / 2f, (float)this.ActualHeight / 2f, 0f);

                // SCALE OUT
                outgroup.Add(
                    compositor.CreateVector3KeyFrameAnimation()
                        .AddScaleKeyFrame(1, 1.3f, ease)
                        .SetDuration(outDuration)
                        .SetTarget(nameof(outVisual.Scale)));

                // FADE OUT
                outgroup.Add(
                    compositor.CreateScalarKeyFrameAnimation()
                        .AddKeyFrame(1, 0f, ease)
                        .SetDuration(outDuration)
                        .SetTarget(nameof(Visual.Opacity)));
            }

            // IN ELEMENT
            {
                inVisual.CenterPoint = inVisual.Size.X > 0
                      ? new Vector3(inVisual.Size / 2f, 0f)
                      : new Vector3((float)this.ActualWidth / 2f, (float)this.ActualHeight / 2f, 0f);


                // SCALE IN
                var sO = inVisual.Compositor.CreateVector3KeyFrameAnimation();
                sO.Duration = inDuration;
                sO.Target = nameof(inVisual.Scale);
                sO.InsertKeyFrame(0, new Vector3(0.7f, 0.7f, 1.0f), easeOut);
                sO.InsertKeyFrame(1, new Vector3(1.0f, 1.0f, 1.0f), easeOut);
                sO.DelayTime = inStart;
                ingroup.Add(sO);

                // FADE IN
                inVisual.Opacity = 0f;
                var op = inVisual.Compositor.CreateScalarKeyFrameAnimation();
                op.DelayTime = inStart;
                op.Duration = inDuration;
                op.Target = nameof(outVisual.Opacity);
                op.InsertKeyFrame(1, 0f, easeOut);
                op.InsertKeyFrame(1, 1f, easeOut);
                ingroup.Add(op);

            }

            CompositionStoryboard group = new CompositionStoryboard();
            group.Add(new CompositionTimeline(outVisual, outgroup, ease));
            group.Add(new CompositionTimeline(inVisual, ingroup, easeOut));
            return group;
        }

        /// <summary>
        /// Creates the default backwards composition animation
        /// </summary>
        /// <param name="outElement"></param>
        /// <param name="inElement"></param>
        /// <returns></returns>
        CompositionStoryboard CreateCompositionExpoZoomBackward(FrameworkElement outElement, FrameworkElement inElement)
        {
            Compositor compositor = ElementCompositionPreview.GetElementVisual(outElement).Compositor;

            Visual outVisual = ElementCompositionPreview.GetElementVisual(outElement);
            Visual inVisual = ElementCompositionPreview.GetElementVisual(inElement);

            CompositionAnimationGroup outgroup = compositor.CreateAnimationGroup();
            CompositionAnimationGroup ingroup = compositor.CreateAnimationGroup();

            TimeSpan outDuration = TimeSpan.FromSeconds(0.3);
            TimeSpan inDuration = TimeSpan.FromSeconds(0.4);

            CubicBezierEasingFunction ease = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.95f, 0.05f),
                new Vector2(0.79f, 0.04f));

            CubicBezierEasingFunction easeOut = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.19f, 1.0f),
                new Vector2(0.22f, 1.0f));


            // OUT ELEMENT
            {
                outVisual.CenterPoint = outVisual.Size.X > 0
                    ? new Vector3(outVisual.Size / 2f, 0f)
                    : new Vector3((float)this.ActualWidth / 2f, (float)this.ActualHeight / 2f, 0f);

                // SCALE OUT
                var sO = compositor.CreateVector3KeyFrameAnimation();
                sO.Duration = outDuration;
                sO.Target = nameof(outVisual.Scale);
                sO.InsertKeyFrame(1, new Vector3(0.7f, 0.7f, 1.0f), ease);
                outgroup.Add(sO);

                // FADE OUT
                var op = compositor.CreateScalarKeyFrameAnimation();
                op.Duration = outDuration;
                op.Target = nameof(outVisual.Opacity);
                op.InsertKeyFrame(1, 0f, ease);
                outgroup.Add(op);
            }

            // IN ELEMENT
            {
                inVisual.CenterPoint = inVisual.Size.X > 0
                     ? new Vector3(inVisual.Size / 2f, 0f)
                     : new Vector3((float)this.ActualWidth / 2f, (float)this.ActualHeight / 2f, 0f);


                // SCALE IN
                ingroup.Add(
                    inVisual.CreateVector3KeyFrameAnimation(nameof(Visual.Scale))
                        .AddScaleKeyFrame(0, 1.3f)
                        .AddScaleKeyFrame(1, 1f, easeOut)
                        .SetDuration(inDuration)
                        .SetDelayTime(outDuration)
                        .SetDelayBehavior(AnimationDelayBehavior.SetInitialValueBeforeDelay));

                // FADE IN
                inVisual.Opacity = 0f;
                var op = inVisual.Compositor.CreateScalarKeyFrameAnimation();
                op.DelayTime = outDuration;
                op.Duration = inDuration;
                op.Target = nameof(outVisual.Opacity);
                op.InsertKeyFrame(1, 0f, easeOut);
                op.InsertKeyFrame(1, 1f, easeOut);
                ingroup.Add(op);

            }

            CompositionStoryboard group = new CompositionStoryboard();
            group.Add(new CompositionTimeline(outVisual, outgroup, ease));
            group.Add(new CompositionTimeline(inVisual, ingroup, easeOut));
            return group;
        }

        #endregion

    }
}
