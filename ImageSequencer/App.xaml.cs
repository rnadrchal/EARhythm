﻿using ImageSequencer.Views;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using System.Windows;
using Egami.Rhythm.Midi;
using ImageSequencer.Models;
using ImageSequencer.ViewModels;

namespace ImageSequencer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly IConfigurationRoot _config;
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            MidiDevices.Initialize(dawName);
            MidiDevices.Input.StartEventsListening();

        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ApplicationSettings>();
            containerRegistry.RegisterSingleton<ImageViewer>();
            containerRegistry.RegisterSingleton<VisitViewer>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            var visitViewer = Container.Resolve<VisitViewer>();
            if (visitViewer != null)
            {
                visitViewer.Dispose();
            }
        }
    }
}
