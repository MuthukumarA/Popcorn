using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using Popcorn.ViewModels.Players.Trailer;

namespace Popcorn.UserControls.Players.Trailer
{
    /// <summary>
    /// Interaction logic for MoviePlayer.xaml
    /// </summary>
    public partial class TrailerPlayer : IDisposable
    {
        /// <summary>
        /// Identifies the <see cref="Volume" /> dependency property.
        /// </summary>
        internal static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof (double),
            typeof (TrailerPlayer), new PropertyMetadata(0.5, OnVolumeChanged));

        private bool _isMouseActivityCaptured;

        /// <summary>
        /// Initializes a new instance of the TrailerPlayer class.
        /// </summary>
        public TrailerPlayer()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        /// <summary>
        /// Get or set the trailer volume
        /// </summary>
        public double Volume
        {
            get { return (double) GetValue(VolumeProperty); }

            set { SetValue(VolumeProperty, value); }
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Subscribe to events and play the trailer when control has been loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnLoaded(object sender, EventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                window.Closing += (s1, e1) => Dispose();

            var vm = DataContext as TrailerPlayerViewModel;
            if (vm == null) return;
            if (vm.Trailer?.Uri == null)
                return;

            // start the timer used to report time on MediaPlayerSliderProgress
            MediaPlayerTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            MediaPlayerTimer.Tick += MediaPlayerTimerTick;
            MediaPlayerTimer.Start();

            // start the activity timer used to manage visibility of the PlayerStatusBar
            ActivityTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(4)};
            ActivityTimer.Tick += OnInactivity;
            ActivityTimer.Start();

            InputManager.Current.PreProcessInput += OnActivity;

            vm.StoppedPlayingMedia += OnStoppedPlayingMedia;
            Player.MediaEnded += MediaPlayerEndReached;

            Player.Source = vm.Trailer.Uri;
            PlayMedia();
        }

        /// <summary>
        /// When trailer's volume changed, update volume
        /// </summary>
        /// <param name="e">e</param>
        /// <param name="obj">obj</param>
        private static void OnVolumeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var moviePlayer = obj as TrailerPlayer;
            if (moviePlayer == null)
                return;

            var newVolume = (double) e.NewValue;
            moviePlayer.ChangeMediaVolume(newVolume);
        }

        /// <summary>
        /// Change the trailer's volume
        /// </summary>
        /// <param name="newValue">New volume value</param>
        private void ChangeMediaVolume(double newValue) => Player.Volume = newValue;

        /// <summary>
        /// When user uses the mousewheel, update the volume
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">MouseWheelEventArgs</param>
        private void MouseWheelMediaPlayer(object sender, MouseWheelEventArgs e)
        {
            if ((Volume <= 1 && e.Delta > 0) || (Volume >= 0 && e.Delta < 0))
                Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        /// <summary>
        /// When a trailer has been fully played, stop playing the trailer
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerEndReached(object sender, EventArgs e) => DispatcherHelper.CheckBeginInvokeOnUI(() =>
        {
            var vm = DataContext as TrailerPlayerViewModel;
            if (vm == null)
                return;

            vm.StopPlayingMediaCommand.Execute(null);
        });

        /// <summary>
        /// Play the trailer
        /// </summary>
        private void PlayMedia()
        {
            Player.Play();
            MediaPlayerIsPlaying = true;

            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Pause the trailer
        /// </summary>
        private void PauseMedia()
        {
            Player.Pause();
            MediaPlayerIsPlaying = false;

            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// When trailer has finished playing, stop player and dispose control
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnStoppedPlayingMedia(object sender, EventArgs e) => Dispose();

        /// <summary>
        /// Report the playing progress on the timeline
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerTimerTick(object sender, EventArgs e)
        {
            if ((Player == null) || (UserIsDraggingMediaPlayerSlider)) return;
            MediaPlayerSliderProgress.Minimum = 0;
            MediaPlayerSliderProgress.Maximum = Player.NaturalDuration;
            MediaPlayerSliderProgress.Value = decimal.ToDouble(Player.Position);
        }

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = Player != null;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the trailer player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = MediaPlayerIsPlaying;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Report when user has finished dragging the trailer player progress
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragCompletedEventArgs</param>
        private async void MediaSliderProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            UserIsDraggingMediaPlayerSlider = false;
            Player.Position = (decimal)MediaPlayerSliderProgress.Value;
            await Task.Delay(1000);
            PlayMedia();
        }

        /// <summary>
        /// Report runtime when trailer player progress changed
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">RoutedPropertyChangedEventArgs</param>
        private void MediaSliderProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MoviePlayerTextProgressStatus.Text =
                TimeSpan.FromSeconds(MediaPlayerSliderProgress.Value)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture) + " / " +
                TimeSpan.FromSeconds(Player.NaturalDuration)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Play trailer
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">ExecutedRoutedEventArgs</param>
        private void MediaPlayerPlayExecuted(object sender, ExecutedRoutedEventArgs e) => PlayMedia();

        /// <summary>
        /// Pause trailer
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseExecuted(object sender, ExecutedRoutedEventArgs e) => PauseMedia();

        /// <summary>
        /// Hide the PlayerStatusBar on mouse inactivity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnInactivity(object sender, EventArgs e)
        {
            InactiveMousePosition = Mouse.GetPosition(Container);

            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1d), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Show the PlayerStatusBar on mouse activity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private async void OnActivity(object sender, PreProcessInputEventArgs e)
        {
            if (_isMouseActivityCaptured)
                return;

            _isMouseActivityCaptured = true;

            var inputEventArgs = e.StagingItem.Input;
            if (!(inputEventArgs is MouseEventArgs) && !(inputEventArgs is KeyboardEventArgs))
            {
                _isMouseActivityCaptured = false;
                return;
            }
            var mouseEventArgs = e.StagingItem.Input as MouseEventArgs;

            // no button is pressed and the position is still the same as the application became inactive
            if (mouseEventArgs?.LeftButton == MouseButtonState.Released &&
                mouseEventArgs.RightButton == MouseButtonState.Released &&
                mouseEventArgs.MiddleButton == MouseButtonState.Released &&
                mouseEventArgs.XButton1 == MouseButtonState.Released &&
                mouseEventArgs.XButton2 == MouseButtonState.Released &&
                InactiveMousePosition == mouseEventArgs.GetPosition(Container))
            {
                _isMouseActivityCaptured = false;
                return;
            }

            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);

            await Task.Delay(TimeSpan.FromSeconds(1));
            _isMouseActivityCaptured = false;
        }

        /// <summary>
        /// Dispose the control
        /// </summary>
        /// <param name="disposing">If a disposing is already processing</param>
        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            Loaded -= OnLoaded;

            MediaPlayerTimer.Tick -= MediaPlayerTimerTick;
            MediaPlayerTimer.Stop();

            ActivityTimer.Tick -= OnInactivity;
            ActivityTimer.Stop();

            InputManager.Current.PreProcessInput -= OnActivity;

            Player.MediaEnded -= MediaPlayerEndReached;
            MediaPlayerIsPlaying = false;
            Player.Stop();
            Player.Close();
            Player.Dispose();

            var vm = DataContext as TrailerPlayerViewModel;
            if (vm != null)
                vm.StoppedPlayingMedia -= OnStoppedPlayingMedia;

            Disposed = true;

            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}