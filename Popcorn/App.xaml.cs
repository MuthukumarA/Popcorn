﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
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

#if !DEBUG
            SquirrelAwareApp.HandleEvents(
                onInitialInstall: OnInitialInstall,
                onAppUpdate: OnAppUpdate,
                onAppUninstall: OnAppUninstall,
                onFirstRun: OnFirstRun);
#endif
        }

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        public App()
        {
            DispatcherUnhandledException += AppDispatcherUnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// On startup, register synchronization context
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AsyncSynchronizationContext.Register();
        }

        /// <summary>
        /// Observe unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// Handle unhandled expceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">DispatcherUnhandledExceptionEventArgs args</param>
        private void AppDispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception domain has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">UnhandledExceptionEventArgs args</param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Logger.Fatal(ex);
                Messenger.Default.Send(new UnhandledExceptionMessage(new Exception(LocalizationProviderHelper.GetLocalizedValue<string>("FatalError"))));
            }
        }

        /// <summary>
        /// Execute when app is uninstalling
        /// </summary>
        /// <param name="version"><see cref="Version"/> version</param>
        private static async void OnAppUninstall(Version version)
        {
            using (var manager = await UpdateManager.GitHubUpdateManager(Constants.GithubRepository))
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
        private static async void OnAppUpdate(Version version)
        {
            using (var manager = await UpdateManager.GitHubUpdateManager(Constants.GithubRepository))
            {
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.Desktop, true);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.StartMenu, true);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.AppRoot, true);

                manager.RemoveUninstallerRegistryEntry();
                await manager.CreateUninstallerRegistryEntry();
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
        private static async void OnInitialInstall(Version version)
        {
            using (var manager = await UpdateManager.GitHubUpdateManager(Constants.GithubRepository))
            {
                manager.CreateShortcutForThisExe();

                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.Desktop, false);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.StartMenu, false);
                manager.CreateShortcutsForExecutable("Popcorn.exe", ShortcutLocation.AppRoot, false);

                await manager.CreateUninstallerRegistryEntry();
            }
        }
    }
}