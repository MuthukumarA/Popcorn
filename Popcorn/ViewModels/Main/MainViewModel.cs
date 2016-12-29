using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Popcorn.Dialogs;
using Popcorn.Events;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.History;
using Popcorn.Services.Movie;
using Popcorn.ViewModels.Genres;
using Popcorn.ViewModels.Players.Movie;
using Popcorn.ViewModels.Tabs;
using Squirrel;

namespace Popcorn.ViewModels.Main
{
    /// <summary>
    /// Main applcation's viewmodel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Used to define the dialog context
        /// </summary>
        private readonly IDialogCoordinator _dialogCoordinator;

        /// <summary>
        /// Used to interact with movie history
        /// </summary>
        private readonly IMovieHistoryService _movieHistoryService;

        /// <summary>
        /// Used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        /// <summary>
        /// Application state
        /// </summary>
        private IApplicationState _applicationState;

        /// <summary>
        /// Manage movie's genres
        /// </summary>
        private IGenresViewModel _genresViewModel;

        /// <summary>
        /// Specify if an exception is curently managed
        /// </summary>
        private bool _isManagingException;

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        private bool _isMovieFlyoutOpen;

        /// <summary>
        /// Specify if a search is actually active
        /// </summary>
        private bool _isMovieSearchActive;

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        private bool _isSettingsFlyoutOpen;

        /// <summary>
        /// Manage the movie player
        /// </summary>
        private MoviePlayerViewModel _moviePlayerViewModel;

        /// <summary>
        /// The selected tab
        /// </summary>
        private TabsViewModel _selectedTab;

        /// <summary>
        /// The tabs
        /// </summary>
        private ObservableCollection<TabsViewModel> _tabs = new ObservableCollection<TabsViewModel>();

        /// <summary>
        /// Used to update application
        /// </summary>
        private readonly UpdateManager _updateManager;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// <param name="genresViewModel">Instance of GenresViewModel</param>
        /// <param name="movieService">Instance of MovieService</param>
        /// <param name="movieHistoryService">Instance of MovieHistoryService</param>
        /// <param name="applicationState">Instance of ApplicationState</param>
        public MainViewModel(IGenresViewModel genresViewModel, IMovieService movieService,
            IMovieHistoryService movieHistoryService, IApplicationState applicationState)
        {
            _dialogCoordinator = DialogCoordinator.Instance;
            _movieService = movieService;
            _movieHistoryService = movieHistoryService;
            ApplicationState = applicationState;
            GenresViewModel = genresViewModel;
            RegisterMessages();
            RegisterCommands();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => _updateManager.Dispose();
            _updateManager = new UpdateManager(Constants.UpdateServerUrl, Constants.ApplicationName);
        }

        /// <summary>
        /// Tabs shown into the interface
        /// </summary>
        public ObservableCollection<TabsViewModel> Tabs
        {
            get { return _tabs; }
            set { Set(() => Tabs, ref _tabs, value); }
        }

        /// <summary>
        /// The selected tab
        /// </summary>
        public TabsViewModel SelectedTab
        {
            get { return _selectedTab; }
            set { Set(() => SelectedTab, ref _selectedTab, value); }
        }

        /// <summary>
        /// Specify if a movie search is active
        /// </summary>
        public bool IsMovieSearchActive
        {
            get { return _isMovieSearchActive; }
            private set { Set(() => IsMovieSearchActive, ref _isMovieSearchActive, value); }
        }

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        public bool IsSettingsFlyoutOpen
        {
            get { return _isSettingsFlyoutOpen; }
            set { Set(() => IsSettingsFlyoutOpen, ref _isSettingsFlyoutOpen, value); }
        }

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        public bool IsMovieFlyoutOpen
        {
            get { return _isMovieFlyoutOpen; }
            set { Set(() => IsMovieFlyoutOpen, ref _isMovieFlyoutOpen, value); }
        }

        /// <summary>
        /// Manage movie's genres
        /// </summary>
        public IGenresViewModel GenresViewModel
        {
            get { return _genresViewModel; }
            set { Set(() => GenresViewModel, ref _genresViewModel, value); }
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationState ApplicationState
        {
            get { return _applicationState; }
            set { Set(() => ApplicationState, ref _applicationState, value); }
        }

        /// <summary>
        /// Command used to select the greatest movies tab
        /// </summary>
        public RelayCommand SelectGreatestTab { get; private set; }

        /// <summary>
        /// Command used to select the popular movies tab
        /// </summary>
        public RelayCommand SelectPopularTab { get; private set; }

        /// <summary>
        /// Command used to select the recent movies tab
        /// </summary>
        public RelayCommand SelectRecentTab { get; private set; }

        /// <summary>
        /// Command used to select the search movies tab
        /// </summary>
        public RelayCommand SelectSearchTab { get; private set; }

        /// <summary>
        /// Command used to select the seen movies tab
        /// </summary>
        public RelayCommand SelectSeenTab { get; private set; }

        /// <summary>
        /// Command used to select the favorites movies tab
        /// </summary>
        public RelayCommand SelectFavoritesTab { get; private set; }

        /// <summary>
        /// Command used to close movie page
        /// </summary>
        public RelayCommand CloseMoviePageCommand { get; private set; }

        /// <summary>
        /// Command used to close the application
        /// </summary>
        public RelayCommand MainWindowClosingCommand { get; private set; }

        /// <summary>
        /// Command used to open application settings
        /// </summary>
        public RelayCommand OpenSettingsCommand { get; private set; }

        /// <summary>
        /// Command used to load tabs
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Event raised when window state has changed
        /// </summary>
        public event EventHandler<WindowStateChangedEventArgs> WindowStateChanged;

        /// <summary>
        /// Load tabs asynchronously
        /// </summary>
        private async Task LoadTabsAsync()
        {
            Tabs.Add(new PopularTabViewModel(ApplicationState, _movieService, _movieHistoryService));
            Tabs.Add(new GreatestTabViewModel(ApplicationState, _movieService, _movieHistoryService));
            Tabs.Add(new RecentTabViewModel(ApplicationState, _movieService, _movieHistoryService));
            Tabs.Add(new FavoritesTabViewModel(ApplicationState, _movieService, _movieHistoryService));
            Tabs.Add(new SeenTabViewModel(ApplicationState, _movieService, _movieHistoryService));
            SelectedTab = Tabs.First();
            foreach (var tab in Tabs)
                await tab.LoadMoviesAsync();

            await GenresViewModel.LoadGenresAsync();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ManageExceptionMessage>(this, e => ManageException(e.UnHandledException));

            Messenger.Default.Register<WindowStateChangeMessage>(this,
                e => OnWindowStateChanged(new WindowStateChangedEventArgs(e.IsMoviePlaying)));

            Messenger.Default.Register<LoadMovieMessage>(this, e => IsMovieFlyoutOpen = true);

            Messenger.Default.Register<PlayMovieMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                _moviePlayerViewModel = new MoviePlayerViewModel(ApplicationState, _movieService,
                    _movieHistoryService);
                _moviePlayerViewModel.LoadMovie(message.Movie);
                Tabs.Add(_moviePlayerViewModel);
                SelectedTab = Tabs.Last();
                IsMovieFlyoutOpen = false;
                ApplicationState.IsMoviePlaying = true;
            }));

            Messenger.Default.Register<StopPlayingMovieMessage>(
                this,
                message =>
                {
                    // Remove the movie tab
                    MoviePlayerViewModel moviePlayer = null;
                    foreach (var mediaViewModel in Tabs.OfType<MoviePlayerViewModel>())
                        moviePlayer = mediaViewModel;

                    if (moviePlayer != null)
                    {
                        Tabs.Remove(moviePlayer);
                        moviePlayer.Cleanup();
                        SelectedTab = Tabs.FirstOrDefault();
                    }

                    IsMovieFlyoutOpen = true;
                    ApplicationState.IsMoviePlaying = false;
                });

            Messenger.Default.Register<SearchMovieMessage>(this,
                async message => await SearchMovies(message.Filter));
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            SelectGreatestTab = new RelayCommand(() =>
            {
                if (SelectedTab is GreatestTabViewModel)
                    return;
                foreach (var greatestTab in Tabs.OfType<GreatestTabViewModel>())
                    SelectedTab = greatestTab;
            });

            SelectPopularTab = new RelayCommand(() =>
            {
                if (SelectedTab is PopularTabViewModel)
                    return;
                foreach (var popularTab in Tabs.OfType<PopularTabViewModel>())
                    SelectedTab = popularTab;
            });

            SelectRecentTab = new RelayCommand(() =>
            {
                if (SelectedTab is RecentTabViewModel)
                    return;
                foreach (var recentTab in Tabs.OfType<RecentTabViewModel>())
                    SelectedTab = recentTab;
            });

            SelectSearchTab = new RelayCommand(() =>
            {
                if (SelectedTab is SearchTabViewModel)
                    return;
                foreach (var searchTab in Tabs.OfType<SearchTabViewModel>())
                    SelectedTab = searchTab;
            });

            SelectFavoritesTab = new RelayCommand(() =>
            {
                if (SelectedTab is FavoritesTabViewModel)
                    return;
                foreach (var favoritesTab in Tabs.OfType<FavoritesTabViewModel>())
                    SelectedTab = favoritesTab;
            });

            SelectSeenTab = new RelayCommand(() =>
            {
                if (SelectedTab is SeenTabViewModel)
                    return;
                foreach (var seenTab in Tabs.OfType<SeenTabViewModel>())
                    SelectedTab = seenTab;
            });

            CloseMoviePageCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new StopPlayingTrailerMessage());
                IsMovieFlyoutOpen = false;
            });

            MainWindowClosingCommand = new RelayCommand(() =>
            {
                foreach (var tab in Tabs)
                    tab?.Cleanup();

                ViewModelLocator.Cleanup();

                if (!Directory.Exists(Constants.MovieDownloads)) return;
                foreach (
                    var filePath in Directory.GetDirectories(Constants.MovieDownloads)
                    )
                {
                    try
                    {
                        Logger.Debug(
                            $"Deleting directory: {filePath}");
                        Directory.Delete(filePath, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error while deleting directory: {ex.Message}.");
                    }
                }

                if (!Directory.Exists(Constants.TorrentDownloads)) return;
                foreach (
                    var filePath in Directory.GetFiles(Constants.TorrentDownloads, "*.*", SearchOption.AllDirectories)
                    )
                {
                    try
                    {
                        Logger.Debug(
                            $"Deleting file: {filePath}");
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error while deleting file: {ex.Message}.");
                    }
                }
            });

            OpenSettingsCommand = new RelayCommand(() => IsSettingsFlyoutOpen = true);

            InitializeAsyncCommand = new RelayCommand(async () =>
            {
                await LoadTabsAsync();
            });
        }

        /// <summary>
        /// Display a dialog on unhandled exception
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
                ManageException(ex);
        }

        /// <summary>
        /// Manage an exception
        /// </summary>
        /// <param name="exception">The exception to manage</param>
        private void ManageException(Exception exception)
        {
            if (_isManagingException)
                return;

            _isManagingException = true;
            IsMovieFlyoutOpen = false;
            IsSettingsFlyoutOpen = false;

            if (exception is WebException || exception is SocketException)
                ApplicationState.IsConnectionInError = true;

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                var exceptionDialog =
                    new ExceptionDialog(
                        new ExceptionDialogSettings(
                            LocalizationProviderHelper.GetLocalizedValue<string>("EmbarrassingError"), exception.Message));
                await _dialogCoordinator.ShowMetroDialogAsync(this, exceptionDialog);
                await exceptionDialog.WaitForButtonPressAsync();
                _isManagingException = false;
                await _dialogCoordinator.HideMetroDialogAsync(this, exceptionDialog);
            });
        }

        /// <summary>
        /// Search for movie with a criteria
        /// </summary>
        /// <param name="criteria">The criteria used for search</param>
        private async Task SearchMovies(string criteria)
        {
            if (string.IsNullOrEmpty(criteria))
            {
                // The search filter is empty. We have to find the search tab if any
                foreach (var searchTabToRemove in Tabs.OfType<SearchTabViewModel>())
                {
                    // The search tab is currently selected in the UI, we have to pick a different selected tab prior deleting
                    if (searchTabToRemove == SelectedTab)
                        SelectedTab = Tabs.FirstOrDefault();

                    Tabs.Remove(searchTabToRemove);
                    searchTabToRemove.Cleanup();
                    IsMovieSearchActive = false;
                    return;
                }
            }
            else
            {
                IsMovieSearchActive = true;

                foreach (var searchTab in Tabs.OfType<SearchTabViewModel>())
                {
                    await searchTab.SearchMoviesAsync(criteria);
                    if (SelectedTab != searchTab)
                        SelectedTab = searchTab;

                    return;
                }

                Tabs.Add(new SearchTabViewModel(ApplicationState, _movieService, _movieHistoryService));
                SelectedTab = Tabs.Last();
                var searchMovieTab = SelectedTab as SearchTabViewModel;
                if (searchMovieTab != null)
                    await searchMovieTab.SearchMoviesAsync(criteria);
            }
        }

        /// <summary>
        /// Fire when window state has changed
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnWindowStateChanged(WindowStateChangedEventArgs e)
        {
            Logger.Debug(
                "Window state changed");

            var handler = WindowStateChanged;
            handler?.Invoke(this, e);
        }
    }
}