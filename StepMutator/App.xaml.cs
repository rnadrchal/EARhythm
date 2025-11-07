using Egami.Rhythm.Midi;
using Microsoft.Extensions.Configuration;
using Prism.Events;
using Prism.Ioc;
using StepMutator.Events;
using StepMutator.Models.Evolution;
using StepMutator.Services;
using StepMutator.Views;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using StepMutator.Models;

namespace StepMutator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly IConfigurationRoot _config;
        // Key-State: verhindert doppelte Publishes
        private readonly HashSet<Key> _pressedKeys = new();
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZcc3RQR2RfUkJ0XUJWYEg=");
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var dawName = _config.GetSection("LoopMidiPorts")["Daw"];
            MidiDevices.Initialize(dawName);
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IEvolutionOptions, EvolutionOptions>();
            containerRegistry.RegisterSingleton<IMutator<ulong>, StepMutator<ulong>>();
            containerRegistry.RegisterSingleton<FitnessSettings>();
            containerRegistry.RegisterSingleton<Sequence>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // PreProcessInput wird für alle Tastatureingaben der App aufgerufen
            InputManager.Current.PreProcessInput += OnPreProcessInput;
        }

        private void OnPreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem?.Input is KeyEventArgs ke)
            {
                // Nur die "final
                // en" KeyDown / KeyUp behandeln (nicht Preview),
                // oder entferne die check auf Preview wenn du Preview bevorzugst.
                if (ke.RoutedEvent != Keyboard.KeyDownEvent && ke.RoutedEvent != Keyboard.KeyUpEvent)
                    return;

                // Optional: Wiederholung bei KeyDown ignorieren
                if (ke.RoutedEvent == Keyboard.KeyDownEvent && ke.IsRepeat)
                    return;

                bool isDown = ke.RoutedEvent == Keyboard.KeyDownEvent;

                // Deduplication: nur bei echtem Zustandswechsel publizieren
                lock (_pressedKeys)
                {
                    if (isDown)
                    {
                        // bereits gedrückt -> ignore
                        if (!_pressedKeys.Add(ke.Key))
                            return;
                    }
                    else
                    {
                        // KeyUp aber Key war nicht als gedrückt markiert -> ignore
                        if (!_pressedKeys.Remove(ke.Key))
                            return;
                    }
                }

                var payload = new GlobalKeyPayload(ke.Key, isDown, Keyboard.Modifiers, ke);

                var ea = Container.Resolve<IEventAggregator>();
                ea.GetEvent<GlobalKeyEvent>().Publish(payload);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Container.Resolve<Sequence>().AutoSave();
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            base.OnExit(e);
        }
    }
}
