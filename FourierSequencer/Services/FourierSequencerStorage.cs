using FourierSequencer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace FourierSequencer.Services
{
    public static class FourierSequencerStorage
    {
        public class FourierCoeffDto
        {
            public int Number { get; set; }
            public double A { get; set; }
            public double B { get; set; }
        }

        public class ModelDto
        {
            // identify which sequencer this DTO belongs to
            public SequencerTarget Target { get; set; }

            // UI/runtime state
            public bool IsActive { get; set; }

            // basic settings
            public int Harmonics { get; set; }
            public int Periods { get; set; }
            public int Steps { get; set; }
            public int PointsPerStep { get; set; }
            public double StepOffset { get; set; }

            // midi scaling and thresholds
            public double MidiMin { get; set; }
            public double MidiMax { get; set; }
            public double LowerThreshold { get; set; }
            public double UpperThreshold { get; set; }

            // runtime / performance settings
            public int Divider { get; set; }
            public int Channel { get; set; }
            public bool Legato { get; set; }

            public List<FourierCoeffDto> Coefficients { get; set; } = new();
        }

        // top-level preset containing multiple sequencer models
        public class PresetDto
        {
            public List<ModelDto> Sequencers { get; set; } = new();
        }

        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static FourierSequencerStorage()
        {
            // serialize enums as strings for readability and stability
            DefaultOptions.Converters.Add(new JsonStringEnumConverter());
        }

        // Save a single model (backwards compatible)
        public static Task SaveAsync(string filePath, FourierSequencerModel model)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            return SaveAsync(filePath, new[] { model });
        }

        // Save multiple sequencer models into a single preset file
        public static async Task SaveAsync(string filePath, IEnumerable<FourierSequencerModel> models)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (models == null) throw new ArgumentNullException(nameof(models));

            var preset = new PresetDto();

            foreach (var model in models)
            {
                var dto = new ModelDto
                {
                    Target = model.Target,
                    IsActive = model.IsActive,
                    Harmonics = model.Harmonics,
                    Periods = model.Periods,
                    Steps = model.Steps,
                    PointsPerStep = model.PointsPerStep,
                    StepOffset = model.StepOffset,
                    MidiMin = model.MidiMin,
                    MidiMax = model.MidiMax,
                    LowerThreshold = model.LowerThreshold,
                    UpperThreshold = model.UpperThreshold,
                    Divider = model.Divider,
                    Channel = model.Channel,
                    Legato = model.Legato
                };

                // coefficients
                for (int i = 0; i < model.FourierCoeffizients.Count; i++)
                {
                    var c = model.FourierCoeffizients[i];
                    dto.Coefficients.Add(new FourierCoeffDto { Number = i, A = c.A, B = c.B });
                }

                preset.Sequencers.Add(dto);
            }

            var json = JsonSerializer.Serialize(preset, DefaultOptions);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }

        // Load into a single model (backwards compatible) - will load the first matching sequencer found in file
        public static Task<bool> LoadAsync(string filePath, FourierSequencerModel model)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            return LoadAsync(filePath, new[] { model });
        }

        // Load preset and apply DTOs to the provided models (matched by Target)
        public static async Task<bool> LoadAsync(string filePath, IEnumerable<FourierSequencerModel> models)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (models == null) throw new ArgumentNullException(nameof(models));
            if (!File.Exists(filePath)) return false;

            // Read & deserialize off the UI thread
            string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            PresetDto? preset;
            try
            {
                preset = JsonSerializer.Deserialize<PresetDto>(json, DefaultOptions);
            }
            catch
            {
                return false;
            }

            if (preset == null || preset.Sequencers == null || preset.Sequencers.Count == 0) return false;

            // Apply UI-affecting changes on the Dispatcher (or directly if no dispatcher available)
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ApplyPresetToModels(preset, models);
            }
            else
            {
                var op = dispatcher.InvokeAsync(() => ApplyPresetToModels(preset, models));
                await op.Task.ConfigureAwait(false);
            }

            return true;
        }

        private static void ApplyPresetToModels(PresetDto preset, IEnumerable<FourierSequencerModel> models)
        {
            // for each dto find matching model by Target and apply
            foreach (var dto in preset.Sequencers)
            {
                FourierSequencerModel? targetModel = null;
                foreach (var m in models)
                {
                    if (m.Target == dto.Target)
                    {
                        targetModel = m;
                        break;
                    }
                }

                if (targetModel == null)
                {
                    Debug.WriteLine($"No model provided for target {dto.Target} - skipping");
                    continue;
                }

                ApplyDtoToModel(dto, targetModel);
            }
        }

        // Apply DTO to model must run on UI dispatcher (touches ObservableCollection and model properties)
        private static void ApplyDtoToModel(ModelDto dto, FourierSequencerModel model)
        {
            // Apply harmonics first so coefficient collection has correct size
            if (dto.Harmonics >= 0 && dto.Harmonics < 1000) // reasonable guard
            {
                Debug.WriteLine($"dto.Harmonics={dto.Harmonics}, model.Harmonics={model.Harmonics}");
                model.Harmonics = dto.Harmonics;
            }

            // copy coefficients (if dto contains them)
            if (dto.Coefficients != null && dto.Coefficients.Count > 0)
            {
                int need = dto.Coefficients.Count;
                if (model.FourierCoeffizients.Count < need)
                {
                    for (int i = model.FourierCoeffizients.Count; i < need; i++)
                    {
                        var c = new FourierCoeffizients(i);
                        c.PropertyChanged += (s, e) => model.Generate();
                        model.FourierCoeffizients.Add(c);
                    }
                }

                int copy = Math.Min(need, model.FourierCoeffizients.Count);
                for (int i = 0; i < copy; i++)
                {
                    var dtoC = dto.Coefficients[i];
                    var target = model.FourierCoeffizients[i];
                    target.A = dtoC.A;
                    target.B = dtoC.B;
                }
            }

            // Now apply the rest of the settings in sensible order
            // PointsPerStep before Steps to avoid Generate issues
            if (dto.PointsPerStep > 0 && dto.PointsPerStep < 100)
                model.PointsPerStep = dto.PointsPerStep;

            if (dto.Steps > 0 && dto.Steps < 10000)
                model.Steps = dto.Steps;

            model.Periods = dto.Periods;
            model.StepOffset = dto.StepOffset;

            // set midi range and thresholds (clamped by model setters)
            model.MidiMin = dto.MidiMin;
            model.MidiMax = dto.MidiMax;
            model.LowerThreshold = dto.LowerThreshold;
            model.UpperThreshold = dto.UpperThreshold;

            // runtime settings
            model.Divider = dto.Divider;
            model.Channel = dto.Channel;
            model.Legato = dto.Legato;

            // set IsActive if provided
            model.IsActive = dto.IsActive;

            // ensure final state is generated
            model.Generate();
        }
    }
}