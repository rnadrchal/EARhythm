using FourierSequencer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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

        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task SaveAsync(string filePath, FourierSequencerModel model)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var dto = new ModelDto
            {
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

            var json = JsonSerializer.Serialize(dto, DefaultOptions);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }

        public static async Task<bool> LoadAsync(string filePath, FourierSequencerModel model)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (!File.Exists(filePath)) return false;

            // Read & deserialize off the UI thread
            string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            ModelDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<ModelDto>(json, DefaultOptions);
            }
            catch
            {
                return false;
            }

            if (dto == null) return false;

            // Apply UI-affecting changes on the Dispatcher (or directly if no dispatcher available)
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ApplyDtoToModel(dto, model);
            }
            else
            {
                var op = dispatcher.InvokeAsync(() => ApplyDtoToModel(dto, model));
                await op.Task.ConfigureAwait(false);
            }

            return true;
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

            // ensure final state is generated
            model.Generate();
        }
    }
}