# Popcorn

An application which aims to provide a simple interface to watch any movie.

![Screenshot1](https://cloud.githubusercontent.com/assets/8962802/9643432/2d85a3a4-51c2-11e5-87bf-55ee81d5cc7a.jpg)

![Screenshot2](https://cloud.githubusercontent.com/assets/8962802/9288484/e6f0a25e-4348-11e5-9317-f8c0dee12729.jpg)

![Screenshot3](https://cloud.githubusercontent.com/assets/8962802/9288481/dee26ee4-4348-11e5-972e-b7609bd07ca8.jpg)

## What does it use?
.NET Framework 4.6.2 and C# for the backend. WPF/XAML for the interface.

### Dependencies
* MVVM framework: [MVVM Light](https://mvvmlight.codeplex.com) 
* UI framework: [MahApps](https://github.com/MahApps/MahApps.Metro)
* Media Player: [Meta.Vlc](https://github.com/higankanshi/Meta.Vlc)
* libtorrent wrapper: [Libtorrent.NET](https://github.com/bbougot/Libtorrent.NET)
* ORM: [Entity Framework](https://github.com/aspnet/EntityFramework)
* Database storage: [SqlServer Compact](https://www.nuget.org/packages/Microsoft.SqlServer.Compact/)
* JSON Deserialization: [Json.NET](https://github.com/JamesNK/Newtonsoft.Json)
* REST management: [RestSharp](https://github.com/restsharp/RestSharp)
* Logging: [NLog](https://github.com/NLog/NLog)
* Unit testing: [NUnit](https://github.com/nunit/nunit) & [AutoFixture](https://github.com/AutoFixture/AutoFixture)
* IMDb data: [TMDbLib](https://github.com/LordMike/TMDbLib/)
* Downloadable Youtube videos: [YoutubeExtractor](https://github.com/flagbug/YoutubeExtractor)
* Localization: [WpfLocalizeExtension](https://github.com/SeriousM/WPFLocalizationExtension)

### API
* Movies' torrent source from [YTS](http://yts.ag)
* Movies' data from [TMDb](http://tmdb.org)

## Supported platforms
At this time, only Windows 7+ is supported (Windows 7, 8, 8.1, 10).

## Can I help you?
Of course yes! Any pull-request will be considered.

## Tasks
- [ ] Fork the repo and make it universal (UWP)
- [ ] Seekable movie stream
- [ ] In-memory movie downloading
- [ ] Add series/anime
- [ ] Add cast page (actor, director,...)
- [ ] VPN
- [ ] DLNA broadcasting
- [ ] User account with movie chatrooms (like Jabbr)
- [x] Subtitles support
- [x] List movies (Popular, Greatest, Recent)
- [x] Search (Movie Title/IMDb Code, Actor Name/IMDb Code, Director Name/IMDb Code)
- [x] Trailer support
- [x] Streaming movies
- [x] Quality selection (720p/1080p)
- [x] Favorites/Seen movies history
- [x] Player features : Play, Pause, Stop, Volume, Fullscreen
- [x] Settings features : Language (English/French), Broadband limit

## Installer
Download full installer [here](https://github.com/bbougot/Popcorn/releases/download/1.5.0/Setup.exe)
