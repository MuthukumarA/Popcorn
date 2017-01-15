using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.Genre;
using Popcorn.Models.Localization;
using Popcorn.Models.Movie.Full;
using Popcorn.Models.Movie.Short;
using Popcorn.Models.Subtitle;
using RestSharp;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace Popcorn.Services.Movie
{
    /// <summary>
    /// Services used to interact with movies
    /// </summary>
    public class MovieService : IMovieService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initialize a new instance of MovieService class
        /// </summary>
        public MovieService()
        {
            TmdbClient = new TMDbClient(Constants.TmDbClientId)
            {
                MaxRetryCount = 10
            };
        }

        /// <summary>
        /// TMDb client
        /// </summary>
        private TMDbClient TmdbClient { get; }

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public void ChangeTmdbLanguage(ILanguage language) => TmdbClient.DefaultLanguage = language.Culture;

        /// <summary>
        /// Get all movie's genres
        /// </summary>
        /// <param name="ct">Used to cancel loading genres</param>
        /// <returns>Genres</returns>
        public async Task<List<MovieGenre>> GetGenresAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var genres = new List<MovieGenre>();

            try
            {
                await Task.Run(async () =>
                {
                    var englishGenre = await TmdbClient.GetMovieGenresAsync(new EnglishLanguage().Culture);
                    genres.AddRange((await TmdbClient.GetMovieGenresAsync()).Select(genre => new MovieGenre
                    {
                        EnglishName = englishGenre.FirstOrDefault(p => p.Id == genre.Id)?.Name,
                        TmdbGenre = genre
                    }));
                }, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetGenresAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetGenresAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetGenresAsync in {elapsedMs} milliseconds.");
            }

            return genres;
        }

        /// <summary>
        /// Get popular movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular movies and the number of movies found</returns>
        public async Task<Tuple<IEnumerable<MovieShort>, int>> GetPopularMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            MovieGenre genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new WrapperMovieShort();

            if (limit < 1 || limit > 50)
                limit = 20;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "list_movies.json");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "like_count");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<WrapperMovieShort>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetPopularMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetPopularMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetPopularMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var movies = GetMoviesListFromWrapper(wrapper) ?? new List<MovieShort>();
            var nbMovies = wrapper?.Data?.MovieCount ?? 0;

            return new Tuple<IEnumerable<MovieShort>, int>(movies, nbMovies);
        }

        /// <summary>
        /// Get greatest movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Top rated movies and the number of movies found</returns>
        public async Task<Tuple<IEnumerable<MovieShort>, int>> GetGreatestMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            MovieGenre genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new WrapperMovieShort();

            if (limit < 1 || limit > 50)
                limit = 20;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "list_movies.json");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "download_count");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<WrapperMovieShort>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetGreatestMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetGreatestMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetGreatestMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var movies = GetMoviesListFromWrapper(wrapper) ?? new List<MovieShort>();
            var nbMovies = wrapper?.Data?.MovieCount ?? 0;

            return new Tuple<IEnumerable<MovieShort>, int>(movies, nbMovies);
        }

        /// <summary>
        /// Get recent movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Recent movies and the number of movies found</returns>
        public async Task<Tuple<IEnumerable<MovieShort>, int>> GetRecentMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            MovieGenre genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new WrapperMovieShort();

            if (limit < 1 || limit > 50)
                limit = 20;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "list_movies.json");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "seeds");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<WrapperMovieShort>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetRecentMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetRecentMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetRecentMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var movies = GetMoviesListFromWrapper(wrapper) ?? new List<MovieShort>();
            var nbMovies = wrapper?.Data?.MovieCount ?? 0;

            return new Tuple<IEnumerable<MovieShort>, int>(movies, nbMovies);
        }

        /// <summary>
        /// Search movies by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched movies and the number of movies found</returns>
        public async Task<Tuple<IEnumerable<MovieShort>, int>> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            MovieGenre genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new WrapperMovieShort();

            if (limit < 1 || limit > 50)
                limit = 20;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "list_movies.json");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("query_term", criteria);

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<WrapperMovieShort>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "SearchMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SearchMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SearchMoviesAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var movies = GetMoviesListFromWrapper(wrapper) ?? new List<MovieShort>();
            var nbMovies = wrapper?.Data?.MovieCount ?? 0;

            return new Tuple<IEnumerable<MovieShort>, int>(movies, nbMovies);
        }

        /// <summary>
        /// Get TMDb movie informations
        /// </summary>
        /// <param name="movieToLoad">Movie to load</param>
        /// <param name="ct">Used to cancel loading</param>
        /// <returns>Movie's full details</returns>
        public async Task<MovieFull> GetMovieFullDetailsAsync(MovieShort movieToLoad, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var movie = new MovieFull();

            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movie_details.json");
            request.AddParameter("movie_id", movieToLoad.Id);
            request.AddParameter("with_images", "true");
            request.AddParameter("with_cast", "true");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<WrapperMovieFull>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                await Task.Run(async () =>
                {
                    var tmdbInfos = await TmdbClient.GetMovieAsync(response.Data.Data.Movie.ImdbCode,
                        MovieMethods.Credits);

                    movie = new MovieFull
                    {
                        Id = response.Data.Data.Movie.Id,
                        Cast = response.Data.Data.Movie.Cast,
                        BackgroundImagePath = string.Empty,
                        DateUploaded = response.Data.Data.Movie.DateUploaded,
                        DateUploadedUnix = response.Data.Data.Movie.DateUploadedUnix,
                        DescriptionFull = tmdbInfos.Overview,
                        DescriptionIntro = response.Data.Data.Movie.DescriptionIntro,
                        DownloadCount = response.Data.Data.Movie.DownloadCount,
                        FullHdAvailable = response.Data.Data.Movie.Torrents.Any(torrent => torrent.Quality == "1080p"),
                        Genres = tmdbInfos.Genres.Select(a => a.Name).ToList(),
                        ImdbCode = response.Data.Data.Movie.ImdbCode,
                        Language = response.Data.Data.Movie.Language,
                        LikeCount = response.Data.Data.Movie.LikeCount,
                        MpaRating = response.Data.Data.Movie.MpaRating,
                        LargeCoverImage = response.Data.Data.Movie.LargeCoverImage,
                        PosterImagePath = string.Empty,
                        RatingValue = response.Data.Data.Movie.Rating,
                        RtAudienceRating = response.Data.Data.Movie.RtAudienceRating,
                        RtAudienceScore = response.Data.Data.Movie.RtAudienceScore,
                        RtCriticsRating = response.Data.Data.Movie.RtCriticsRating,
                        RtCrtiticsScore = response.Data.Data.Movie.RtCrtiticsScore,
                        Runtime = response.Data.Data.Movie.Runtime,
                        Title = tmdbInfos.Title,
                        TitleLong = response.Data.Data.Movie.TitleLong,
                        Torrents = response.Data.Data.Movie.Torrents,
                        Url = response.Data.Data.Movie.Url,
                        WatchInFullHdQuality = false,
                        Year = response.Data.Data.Movie.Year,
                        YtTrailerCode = response.Data.Data.Movie.YtTrailerCode
                    };
                }, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieFullDetailsAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieFullDetailsAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieFullDetailsAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }

            return movie;
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <param name="ct">Used to cancel translation</param>
        /// <returns>Task</returns>
        public async Task TranslateMovieShortAsync(MovieShort movieToTranslate, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                await Task.Run(async () =>
                {
                    var movie = await TmdbClient.GetMovieAsync(movieToTranslate.ImdbCode,
                        MovieMethods.Credits);
                    movieToTranslate.Title = movie?.Title;
                    movieToTranslate.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                }, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "TranslateMovieShortAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"TranslateMovieShortAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"TranslateMovieShortAsync ({movieToTranslate.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <param name="ct">Used to cancel translation</param>
        /// <returns>Task</returns>
        public async Task TranslateMovieFullAsync(MovieFull movieToTranslate, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                await Task.Run(async () =>
                {
                    var movie = await TmdbClient.GetMovieAsync(movieToTranslate.ImdbCode,
                        MovieMethods.Credits);
                    movieToTranslate.Title = movie?.Title;
                    movieToTranslate.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                    movieToTranslate.DescriptionFull = movie?.Overview;
                }, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "TranslateMovieFull cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"TranslateMovieFull: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"TranslateMovieFull ({movieToTranslate.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get the link to the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<ResultContainer<Video>> GetMovieTrailerAsync(MovieFull movie, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var trailers = new ResultContainer<Video>();
            try
            {
                await Task.Run(async () => trailers = (await TmdbClient.GetMovieAsync(movie.ImdbCode, MovieMethods.Videos))?.Videos, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieTrailerAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieTrailerAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieTrailerAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }

            return trailers;
        }

        /// <summary>
        /// Get the movie's subtitles according to a language
        /// </summary>
        /// <param name="movie">The movie of which to retrieve its subtitles</param>
        /// <param name="ct">Cancellation token</param>
        public async Task LoadSubtitlesAsync(MovieFull movie,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var restClient = new RestClient(Constants.YifySubtitlesApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", movie.ImdbCode);

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<SubtitlesWrapper>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                var wrapper = response.Data;

                var subtitles = new ObservableCollection<Subtitle>();
                Dictionary<string, List<Subtitle>> movieSubtitles;
                if (wrapper.Subtitles == null)
                {
                    await Task.CompletedTask;
                    return;
                }
                if (wrapper.Subtitles.TryGetValue(movie.ImdbCode, out movieSubtitles))
                {
                    foreach (var subtitle in movieSubtitles)
                    {
                        var sub = subtitle.Value.Aggregate((sub1, sub2) => sub1.Rating > sub2.Rating ? sub1 : sub2);
                        subtitles.Add(new Subtitle
                        {
                            Id = sub.Id,
                            Language = new CustomLanguage
                            {
                                Culture = string.Empty,
                                EnglishName = subtitle.Key,
                                LocalizedName = string.Empty
                            },
                            Hi = sub.Hi,
                            Rating = sub.Rating,
                            Url = sub.Url
                        });
                    }
                }

                subtitles.Sort();
                movie.AvailableSubtitles = subtitles;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "LoadSubtitlesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"LoadSubtitlesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"LoadSubtitlesAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Download a subtitle
        /// </summary>
        /// <param name="movie">The movie of which to retrieve its subtitles</param>
        /// <param name="progress">Report the progress of the download</param>
        /// <param name="ct">Cancellation token</param>
        public async Task DownloadSubtitleAsync(MovieFull movie, IProgress<long> progress, CancellationTokenSource ct)
        {
            if (movie.SelectedSubtitle == null)
                return;

            var watch = Stopwatch.StartNew();

            var filePath = Constants.Subtitles + movie.ImdbCode + "\\" + movie.SelectedSubtitle.Language.EnglishName +
                           ".zip";

            try
            {
                var result = await
                    DownloadFileHelper.DownloadFileTaskAsync(
                        Constants.YifySubtitles + movie.SelectedSubtitle.Url, filePath, progress: progress, ct: ct);

                if (result.Item3 == null && !string.IsNullOrEmpty(result.Item2))
                {
                    using (var archive = ZipFile.OpenRead(result.Item2))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.StartsWith("_") ||
                                !entry.FullName.EndsWith(".srt", StringComparison.OrdinalIgnoreCase)) continue;
                            var subtitlePath = Path.Combine(Constants.Subtitles + movie.ImdbCode,
                                entry.FullName);
                            if (!File.Exists(subtitlePath))
                                entry.ExtractToFile(subtitlePath);

                            movie.SelectedSubtitle.FilePath = subtitlePath;
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "DownloadSubtitleAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"DownloadSubtitleAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"DownloadSubtitleAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Download the movie's background image
        /// </summary>
        /// <param name="movie">The movie to process</param>
        /// <param name="ct">Used to cancel downloading background image</param>
        public async Task DownloadBackgroundImageAsync(MovieFull movie, CancellationTokenSource ct)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                await Task.Run(async () =>
                {
                    TmdbClient.GetConfig();
                    var tmdbMovie = await TmdbClient.GetMovieAsync(movie.ImdbCode, MovieMethods.Images);
                    var remotePath = new List<string>
                    {
                        TmdbClient.GetImageUrl(Constants.BackgroundImageSizeTmDb,
                            tmdbMovie.BackdropPath).AbsoluteUri
                    };

                    await
                        remotePath.ForEachAsync(
                            background =>
                                DownloadFileHelper.DownloadFileTaskAsync(background,
                                    Constants.BackgroundMovieDirectory + movie.ImdbCode + Constants.ImageFileExtension,
                                    ct: ct),
                            (background, t) =>
                            {
                                if (t.Item3 == null && !string.IsNullOrEmpty(t.Item2))
                                    movie.BackgroundImagePath = t.Item2;
                            });
                }, ct.Token);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "DownloadBackgroundImageAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"DownloadBackgroundImageAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"DownloadBackgroundImageAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Download cover image for each of the movies provided
        /// </summary>
        /// <param name="movies">The movies to process</param>
        /// <param name="ct">Used to cancel task</param>
        public async Task DownloadCoverImageAsync(IEnumerable<MovieShort> movies, CancellationTokenSource ct)
        {
            var watch = Stopwatch.StartNew();

            var moviesToProcess = movies.ToList();
            try
            {
                await
                    moviesToProcess.ForEachAsync(
                        movie =>
                            DownloadFileHelper.DownloadFileTaskAsync(movie.MediumCoverImage,
                                Constants.CoverMoviesDirectory + movie.ImdbCode + Constants.ImageFileExtension, ct: ct),
                        (movie, t) =>
                        {
                            if (t.Item3 == null && !string.IsNullOrEmpty(t.Item2))
                                movie.CoverImagePath = t.Item2;
                        });
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "DownloadCoverImageAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"DownloadCoverImageAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"DownloadCoverImageAsync ({string.Join(";", moviesToProcess.Select(movie => movie.ImdbCode))}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Download the movie's poster image
        /// </summary>
        /// <param name="movie">The movie to process</param>
        /// <param name="ct">Used to cancel downloading poster image</param>
        public async Task DownloadPosterImageAsync(MovieFull movie, CancellationTokenSource ct)
        {

            var watch = Stopwatch.StartNew();

            var posterPath = new List<string>
            {
                movie.LargeCoverImage
            };

            try
            {
                await
                    posterPath.ForEachAsync(
                        poster =>
                            DownloadFileHelper.DownloadFileTaskAsync(poster,
                                Constants.PosterMovieDirectory + movie.ImdbCode + Constants.ImageFileExtension, ct: ct),
                        (poster, t) =>
                        {
                            if (t.Item3 == null && !string.IsNullOrEmpty(t.Item2))
                                movie.LargeCoverImage = t.Item2;
                        });
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "DownloadPosterImageAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"DownloadPosterImageAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"DownloadPosterImageAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Download actors' image for a movie
        /// </summary>
        /// <param name="movie">The movie to process</param>
        /// <param name="ct">Used to cancel downloading actor image</param>
        public async Task DownloadCastImageAsync(MovieFull movie, CancellationTokenSource ct)
        {
            if (movie.Cast == null)
                return;

            var watch = Stopwatch.StartNew();

            try
            {
                await
                    movie.Cast.ForEachAsync(
                        cast =>
                            DownloadFileHelper.DownloadFileTaskAsync(cast.SmallImage,
                                Constants.CastMovieDirectory + cast.Name + Constants.ImageFileExtension, ct: ct),
                        (cast, t) =>
                        {
                            if (t.Item3 == null && !string.IsNullOrEmpty(t.Item2))
                                cast.SmallImagePath = t.Item2;
                        });
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "DownloadCastImageAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"DownloadCastImageAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"DownloadCastImageAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get movies as a list from wrapped movies
        /// </summary>
        /// <param name="wrapper">Wrapped movies</param>
        /// <returns>List of movies</returns>
        private static IEnumerable<MovieShort> GetMoviesListFromWrapper(WrapperMovieShort wrapper)
            => wrapper?.Data?.Movies?.Select(movie => new MovieShort
            {
                ApiVersion = movie.ApiVersion,
                DateUploaded = movie.DateUploaded,
                DateUploadedUnix = movie.DateUploadedUnix,
                ExecutionTime = movie.ExecutionTime,
                Genres = movie.Genres,
                Id = movie.Id,
                ImdbCode = movie.ImdbCode,
                IsFavorite = false,
                HasBeenSeen = false,
                Language = movie.Language,
                MediumCoverImage = movie.MediumCoverImage,
                CoverImagePath = string.Empty,
                MpaRating = movie.MpaRating,
                RatingValue = movie.Rating,
                Runtime = movie.Runtime,
                ServerTime = movie.ServerTime,
                ServerTimezone = movie.ServerTimezone,
                SmallCoverImage = movie.SmallCoverImage,
                State = movie.State,
                Title = movie.Title,
                TitleLong = movie.TitleLong,
                Torrents = movie.Torrents,
                Url = movie.Url,
                Year = movie.Year
            }).ToList();
    }
}