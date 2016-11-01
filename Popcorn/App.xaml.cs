using System;
using System.Diagnostics;
using System.IO;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Squirrel;
using WPFLocalizeExtension.Engine;

namespace Popcorn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        static App()
        {
            Logger.Info(
                "Popcorn starting...");
            var watchStart = Stopwatch.StartNew();

            Directory.CreateDirectory(Constants.Logging);

            DispatcherHelper.Initialize();

            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;

            watchStart.Stop();
            var elapsedStartMs = watchStart.ElapsedMilliseconds;
            Logger.Info(
                $"Popcorn started in {elapsedStartMs} milliseconds.");

            SquirrelAwareApp.HandleEvents(
                onInitialInstall: OnInitialInstall,
                onAppUpdate: OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);
        }

        /// <summary>
        /// Execute when app is uninstalling
        /// </summary>
        /// <param name="version"><see cref="Version"/> version</param>
        private static void OnAppUninstall(Version version)
        {
            using (var manager = new UpdateManager(Constants.UpdateServerUrl))
            {
                manager.RemoveShortcutsForExecutable("Popcorn.exe", ShortcutLocation.Desktop);
                manager.RemoveShortcutsForExecutable("Popcorn.exe", ShortcutLocation.StartMenu);
                manager.RemoveShortcutsForExecutable("Popcorn.exe", ShortcutLocation.AppRoot);

                manager.RemoveUninstallerRegistryEntry();
            }
        }

        /// <summary>
        /// Execute when app is updating
        /// </summary>
        /// <param name="version"><see cref="Version"/> version</param>
        private static void OnAppUpdate(Version version)
        {
            using (var manager = new UpdateManager(Constants.UpdateServerUrl))
            {
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.Desktop, true);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.StartMenu, true);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.AppRoot, true);

                manager.RemoveUninstallerRegistryEntry();
                manager.CreateUninstallerRegistryEntry();
            }
        }

        /// <summary>
        /// Execute when app has first run
        /// </summary>
        private static void OnFirstRun()
        {
        }

        /// <summary>
        /// Execute when app is installing
        /// </summary>
        /// <param name="version"><see cref="Version"/> version</param>
        private static void OnInitialInstall(Version version)
        {
            using (var manager = new UpdateManager(Constants.UpdateServerUrl))
            {
                manager.CreateShortcutForThisExe();

                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.Desktop, false);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.StartMenu, false);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.AppRoot, false);

                manager.CreateUninstallerRegistryEntry();
            }
        }
    }
}