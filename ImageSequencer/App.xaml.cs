using Egami.Rhythm.Midi;
using ImageSequencer.Models;
using ImageSequencer.Services;
using ImageSequencer.ViewModels;
using ImageSequencer.Views;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using System;
using System.IO;
using System.Linq;
using System.Windows;

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
            // lade zuletzt gespeicherte Settings (falls vorhanden) bevor das MainWindow erstellt wird
            TryLoadLatestSettings();
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
            // Speichere Einstellungen mit Timestamp in %UserProfile%\Documents\Egami\JacksonsDance\Recent
            try
            {
                var appSettings = Container.Resolve<ApplicationSettings>();
                var dir = GetSettingsDirectory();
                Directory.CreateDirectory(dir);
                var fileName = $"settings-{DateTime.Now:yyyyMMddHHmmss}.json";
                var path = Path.Combine(dir, fileName);
                SettingsPersistence.Save(appSettings, path);
            }
            catch
            {
                // optional: Logging
            }

            try
            {
                var visitViewer = Container.Resolve<VisitViewer>();
                visitViewer?.Dispose();
            }
            catch
            {
                // optional: Logging
            }

            base.OnExit(e);
        }

        private void TryLoadLatestSettings()
        {
            try
            {
                var dir = GetSettingsDirectory();
                if (!Directory.Exists(dir))
                    return;

                var files = Directory.GetFiles(dir, "settings-*.json")
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .ToArray();
                if (files == null || files.Length == 0)
                    return;

                // die aktuellste Datei merken
                var newest = files[0];

                // Aufräumen: alle Dateien außer der neuesten löschen, falls älter als 1 Tag
                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(1);
                foreach (var f in files.Skip(1))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(f) < cutoff)
                        {
                            File.Delete(f);
                        }
                    }
                    catch
                    {
                        // ignore Einzellausnahmen beim Löschen
                    }
                }

                var appSettings = Container.Resolve<ApplicationSettings>();
                SettingsPersistence.Load(appSettings, newest, loadBitmap: false);

                // wenn FilePath erreichbar, Bild über ImageViewer laden
                if (!string.IsNullOrWhiteSpace(appSettings.FilePath) && File.Exists(appSettings.FilePath))
                {
                    var imageViewer = Container.Resolve<ImageViewer>();
                    imageViewer.LoadBitmap(appSettings.FilePath);
                }
            }
            catch
            {
                // optional: Logging
            }
        }

        private static string GetSettingsDirectory()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Egami", "JacksonsDance", "Recent");
        }
    }
}
