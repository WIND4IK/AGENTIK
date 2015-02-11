using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;

namespace HelperChat.Controls {
    public class AnimatedImage : Image, INotifyPropertyChanged {
        private BitmapSource[] _bitmapSources;
        private int _currentFrame;
        private bool _isAnimating;
        public static readonly RoutedEvent AnimatedBitmapChangedEvent = EventManager.RegisterRoutedEvent("AnimatedBitmapChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Bitmap>), typeof(AnimatedImage));
        public static readonly DependencyProperty AnimatedBitmapProperty = DependencyProperty.Register("AnimatedBitmap", typeof(Bitmap), typeof(AnimatedImage), new FrameworkPropertyMetadata(null, OnAnimatedBitmapChanged));

        public event PropertyChangedEventHandler PropertyChanged;

        static AnimatedImage() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedImage), new FrameworkPropertyMetadata(typeof(AnimatedImage)));
        }

        private void ChangeSource() {
            Source = _bitmapSources[_currentFrame++];
            PropertyChanged(this, new PropertyChangedEventArgs("Source"));
            _currentFrame = _currentFrame % _bitmapSources.Length;
            ImageAnimator.UpdateFrames();
        }

        protected virtual void OnAnimatedBitmapChanged(RoutedPropertyChangedEventArgs<Bitmap> args) {
            RaiseEvent(args);
        }

        private static void OnAnimatedBitmapChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var image = (AnimatedImage)obj;
            image.UpdateAnimatedBitmap();
            var values = new RoutedPropertyChangedEventArgs<Bitmap>((Bitmap)args.OldValue, (Bitmap)args.NewValue, AnimatedBitmapChangedEvent);
            image.OnAnimatedBitmapChanged(values);
        }

        private void OnFrameChanged(object o, EventArgs e) {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new VoidDelegate(ChangeSource));
        }

        protected virtual void OnSourcePropertyChanged([CallerMemberName] string propertyName = null) {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null) {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void StartAnimate() {
            if (!_isAnimating) {
                ImageAnimator.Animate(AnimatedBitmap, OnFrameChanged);
                _isAnimating = true;
            }
        }

        public void StopAnimate() {
            if (_isAnimating) {
                ImageAnimator.StopAnimate(AnimatedBitmap, OnFrameChanged);
                _isAnimating = false;
            }
        }

        private void UpdateAnimatedBitmap() {
            var frameCount = AnimatedBitmap.GetFrameCount(FrameDimension.Time);
            _currentFrame = 0;
            if (frameCount > 0) {
                _bitmapSources = new BitmapSource[frameCount];
                for (var i = 0; i < frameCount; i++) {
                    AnimatedBitmap.SelectActiveFrame(FrameDimension.Time, i);
                    var bitmap = new Bitmap(AnimatedBitmap);
                    bitmap.MakeTransparent();
                    _bitmapSources[i] = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                StartAnimate();
            }
        }

        public Bitmap AnimatedBitmap {
            get {
                return (Bitmap)GetValue(AnimatedBitmapProperty);
            }
            set {
                StopAnimate();
                SetValue(AnimatedBitmapProperty, value);
            }
        }

        public bool IsAnimating {
            get {
                return _isAnimating;
            }
        }

        private delegate void VoidDelegate();

    }
}
