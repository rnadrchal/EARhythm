using System.Windows;
using Egami.EA.Metrics;
using Egami.EA.Metrics.Metrics;
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
        private Evolution<Sequence> _evolution;
        private readonly IMutator<Sequence> _mutator;
        private readonly IFitnessService _fitnessService;
        private readonly IFitnessServiceOptions _fitnessOptions;

        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _mutator = new SequenceMutator();
            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            MidiDevices.Initialize(dawName);

            _fitnessOptions = new FitnessServiceOptions();
            _fitnessService = new FastBundleFitnessService(_fitnessOptions,
                new CombinedBinarySimilarity());
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
            containerRegistry.RegisterSingleton<IEvolutionOptions, EvolutionOptions>();
            containerRegistry.RegisterSingleton<Evolution<Sequence>>();
            containerRegistry.RegisterInstance(_fitnessOptions);
            containerRegistry.RegisterInstance(_fitnessService);
        }
    }
}
