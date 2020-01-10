﻿using EluneBot.Interfaces;
using EluneBot.Services;
using EluneBot.Services.Interfaces;
using EluneBot.Statics;
using EluneBot.Utilities;
using EluneBot.Utilities.Interfaces;
using EluneBot.ViewModels.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using MessageBox = System.Windows.Forms.MessageBox;

namespace EluneBot.ViewModels
{
    internal class MainViewModel : BaseViewModel
    {
        readonly ILoggerService loggingService;
        readonly IObjectManagerService objectManager;
        readonly IMemoryService memoryService;

        public MainViewModel()
        {
            Services = ConfigureServicesAsync().GetAwaiter().GetResult();
            loggingService = Services.GetRequiredService<ILoggerService>();
            objectManager = Services.GetRequiredService<IObjectManagerService>();
            memoryService = Services.GetRequiredService<IMemoryService>();
            ReloadBasesAsync();
            ReloadBasesAsyncCommand = new AsyncCommand(ReloadBasesAsync);
            StartBaseAsyncCommand = new AsyncCommand(StartBaseAsync, CanStartBase);
            StopBaseAsyncCommand = new AsyncCommand(StopBaseAsync, CanStopBase);
            ToggleGUIAsyncCommand = new AsyncCommand(ToggleGUIAsync);
            AsyncCommands = new IAsyncCommand[]
            {
                ReloadBasesAsyncCommand,
                StartBaseAsyncCommand,
                StopBaseAsyncCommand,
                ToggleGUIAsyncCommand
            };
        }

        IServiceProvider Services { get; }

        public IAsyncCommand ReloadBasesAsyncCommand { get; }
        public IAsyncCommand StartBaseAsyncCommand { get; }
        public IAsyncCommand StopBaseAsyncCommand { get; }
        public IAsyncCommand ToggleGUIAsyncCommand { get; }

        bool running;
        public bool Running
        {
            get => running;
            set
            {
                running = value;
                OnPropertyChanged();
            }
        }

        IBase selectedBase;
        public IBase SelectedBase
        {
            get => selectedBase;
            set
            {
                selectedBase = value;
                OnPropertyChanged();
            }
        }

        ObservableCollection<IBase> availableBases;
        [ImportMany(typeof(IBase), AllowRecomposition = true)]
        public ObservableCollection<IBase> AvailableBases
        {
            get => availableBases;
            set
            {
                availableBases = value;
                OnPropertyChanged();
            }
        }

        Task ReloadBasesAsync()
        {
            if (AvailableBases != null)
            {
                foreach (var @base in AvailableBases)
                {
                    @base.Stop();
                    @base.Dispose();
                }
            }
            var catalog = new AggregateCatalog();
            foreach (var file in Directory.GetFiles(Paths.Bases))
            {
                if (!file.EndsWith(".dll")) continue;
                catalog.Catalogs.Add(new AssemblyCatalog(Assembly.Load(File.ReadAllBytes(file))));
            }
            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue(loggingService);
            container.ComposeExportedValue(objectManager);
            container.ComposeExportedValue(memoryService);
            container.ComposeParts(this);
            if (AvailableBases.Count > 0)
                SelectedBase = AvailableBases[0];
            return Task.CompletedTask;
        }

        bool CanStartBase() =>
            !Running && SelectedBase != null;

        async Task StartBaseAsync()
        {
            if (await memoryService.IsInGameAsync())
            {
                Running = true;
                SelectedBase.Start();
            }
            else
                MessageBox.Show("must be in game to start");
        }

        bool CanStopBase() =>
            Running && SelectedBase != null;

        Task StopBaseAsync()
        {
            Running = false;
            SelectedBase.Stop();
            return Task.CompletedTask;
        }

        Task ToggleGUIAsync()
        {
            if (SelectedBase != null)
                SelectedBase.ToggleGUI();
            return Task.CompletedTask;
        }

        Task<IServiceProvider> ConfigureServicesAsync()
        {
            return Task.FromResult<IServiceProvider>(
                new ServiceCollection()
                .AddSingleton<ILoggerService, LoggerService>()
                .AddSingleton<IObjectManagerService, ObjectManagerService>()
                .AddSingleton<IMemoryService, MemoryService>()
                .BuildServiceProvider());
        }
    }
}
