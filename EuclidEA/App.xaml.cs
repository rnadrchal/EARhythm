using System.Windows;
using Egami.EA.Metrics;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Midi;
using Egami.Rhythm.Midi.Generation;
using Egami.Rhythm.Pattern;
using EuclidEA.Models;
using EuclidEA.Services;
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
        private OutputDevice _dawDevice;
        private Evolution<Egami.Rhythm.Pattern.Sequence> _evolution;
        private readonly IMutator<Egami.Rhythm.Pattern.Sequence> _mutator;

        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _mutator = new RhythmPatternMutator();
            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            MidiDevices.Initialize(dawName);

        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_mutator);
            containerRegistry.RegisterInstance(_config);
            containerRegistry.RegisterSingleton<Services.MidiClock>();
            containerRegistry.RegisterSingleton<IFitnessServiceOptions, FitnessServiceOptions>();
            containerRegistry.RegisterSingleton<IFitnessService, FastBundleFitnessService>();
            containerRegistry.RegisterSingleton<IEvolutionOptions, EvolutionOptions>();
            containerRegistry.RegisterSingleton<Evolution<Egami.Rhythm.Pattern.Sequence>>();
        }
    }
}
