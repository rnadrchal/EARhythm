using ChemSequencer.Views;
using Egami.Rhythm.Midi;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using System.Windows;
using ChemSequencer.ViewModels;
using Egami.Chemistry.PubChem;

namespace ChemSequencer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly IConfigurationRoot _config;
        private readonly PubChemClient _pubChemClient;

        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _config = builder.Build();

            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            MidiDevices.Initialize(dawName);
            MidiDevices.Input.StartEventsListening();

            _pubChemClient = new PubChemClient(new System.Net.Http.HttpClient());
        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_config);
            containerRegistry.RegisterInstance(_pubChemClient);

            containerRegistry.RegisterDialog<MoleculeSelectionDialog, MoleculeSelectionViewModel>("SelectMolecule");
        }
    }
}
