using System.Windows;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Midi.Generation;
using Egami.Rhythm.Pattern;
using EuclidEA.Views;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;

namespace EuclidEA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private IConfigurationRoot _config;
        private InputDevice _clockDevice;
        private OutputDevice _dawDevice;
        private Evolution<RhythmPattern> _evolution;
        private readonly IMutator<RhythmPattern> _mutator;

        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _evolution = new Evolution<RhythmPattern>();
            _mutator = new RhythmPatternMutator();
            var clockName = _config.GetSection("LoopMidiPorts")["Clock"];
            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            _clockDevice = InputDevice.GetByName(clockName);
            _dawDevice = OutputDevice.GetByName(dawName);
            if (_clockDevice != null)
            {
                _clockDevice.StartEventsListening();
            }

            var inputDevice = InputDevice.GetByName(dawName);

        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_evolution);
            containerRegistry.RegisterInstance(_mutator);
            containerRegistry.RegisterInstance(_config);
            containerRegistry.RegisterInstance(_clockDevice);
            containerRegistry.RegisterInstance(_dawDevice);
            containerRegistry.RegisterSingleton<Services.MidiClock>();
        }
    }
}
