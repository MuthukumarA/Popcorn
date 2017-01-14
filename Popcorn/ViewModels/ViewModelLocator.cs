using System.Diagnostics.CodeAnalysis;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.History;
using Popcorn.Services.Language;
using Popcorn.Services.Logging;
using Popcorn.Services.Movie;
using Popcorn.Services.Settings;
using Popcorn.ViewModels.DownloadMovie;
using Popcorn.ViewModels.Genres;
using Popcorn.ViewModels.Main;
using Popcorn.ViewModels.Movie;
using Popcorn.ViewModels.MovieSettings;
using Popcorn.ViewModels.Players.Movie;
using Popcorn.ViewModels.Players.Trailer;
using Popcorn.ViewModels.Search;
using Popcorn.ViewModels.Settings;
using Popcorn.ViewModels.Subtitles;
using Popcorn.ViewModels.Trailer;

namespace Popcorn.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<ILoggingService, LoggingService>();
            SimpleIoc.Default.Register<ISettingsService, SettingsService>();
            SimpleIoc.Default.Register<IMovieService, MovieService>();
            SimpleIoc.Default.Register<ILanguageService, LanguageService>();
            SimpleIoc.Default.Register<IGenresViewModel, GenresViewModel>();
            SimpleIoc.Default.Register<IMovieHistoryService, MovieHistoryService>();
            SimpleIoc.Default.Register<MoviePlayerViewModel>();
            SimpleIoc.Default.Register<IApplicationState, ApplicationState>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ISettingsViewModel, SettingsViewModel>();
            SimpleIoc.Default.Register<IDownloadMovieViewModel, DownloadMovieViewModel>();
            SimpleIoc.Default.Register<ITrailerViewModel, TrailerViewModel>();
            SimpleIoc.Default.Register<ITrailerPlayerViewModel, TrailerPlayerViewModel>();
            SimpleIoc.Default.Register<ISubtitlesViewModel, SubtitlesViewModel>();
            SimpleIoc.Default.Register<IMovieSettingsViewModel, MovieSettingsViewModel>();
            SimpleIoc.Default.Register<MovieViewModel>();
            SimpleIoc.Default.Register<SearchViewModel>();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        /// <summary>
        /// Gets the Movie property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MovieViewModel MoviePage => ServiceLocator.Current.GetInstance<MovieViewModel>();

        /// <summary>
        /// Gets the Search property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public SearchViewModel Search => ServiceLocator.Current.GetInstance<SearchViewModel>();

        /// <summary>
        /// Gets the Settings property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ISettingsViewModel Settings => ServiceLocator.Current.GetInstance<ISettingsViewModel>();

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
            if (SimpleIoc.Default.IsRegistered<ISettingsService>())
            {
                SimpleIoc.Default.Unregister<ISettingsService>();
            }

            if (SimpleIoc.Default.IsRegistered<IMovieService>())
            {
                SimpleIoc.Default.Unregister<IMovieService>();
            }

            if (SimpleIoc.Default.IsRegistered<ILanguageService>())
            {
                SimpleIoc.Default.Unregister<ILanguageService>();
            }

            if (SimpleIoc.Default.IsRegistered<IGenresViewModel>())
            {
                SimpleIoc.Default.Unregister<IGenresViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<IMovieHistoryService>())
            {
                SimpleIoc.Default.Unregister<IMovieHistoryService>();
            }

            if (SimpleIoc.Default.IsRegistered<IMovieHistoryService>())
            {
                SimpleIoc.Default.Unregister<IMovieHistoryService>();
            }

            if (SimpleIoc.Default.IsRegistered<MoviePlayerViewModel>())
            {
                SimpleIoc.Default.Unregister<MoviePlayerViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<IApplicationState>())
            {
                SimpleIoc.Default.Unregister<IApplicationState>();
            }

            if (SimpleIoc.Default.IsRegistered<ISettingsViewModel>())
            {
                SimpleIoc.Default.Unregister<ISettingsViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<IDownloadMovieViewModel>())
            {
                SimpleIoc.Default.Unregister<IDownloadMovieViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<ITrailerViewModel>())
            {
                SimpleIoc.Default.Unregister<ITrailerViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<ITrailerPlayerViewModel>())
            {
                SimpleIoc.Default.Unregister<ITrailerPlayerViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<ISubtitlesViewModel>())
            {
                SimpleIoc.Default.Unregister<ISubtitlesViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<IMovieSettingsViewModel>())
            {
                SimpleIoc.Default.Unregister<IMovieSettingsViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<MovieViewModel>())
            {
                SimpleIoc.Default.Unregister<MovieViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<SearchViewModel>())
            {
                SimpleIoc.Default.Unregister<SearchViewModel>();
            }

            if (SimpleIoc.Default.IsRegistered<MainViewModel>())
            {
                SimpleIoc.Default.Unregister<MainViewModel>();
            }
        }
    }
}