using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StepMutator.Models.Evolution;

namespace StepMutator.Services
{
    public static class SequencePersistence
    {
        private static JsonSerializerOptions Options => new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // DTO: vollständiger Snapshot
        public sealed class SequenceSnapshot
        {
            public ulong[][] OriginalSteps { get; set; } = Array.Empty<ulong[]>();
            public ulong[] OriginalAttractor { get; set; } = Array.Empty<ulong>();
            public int Divider { get; set; }
            public byte Channel { get; set; }

            // Neu: ob Pitchbend gesendet werden soll
            public bool SendPitchbend { get; set; }

            public FitnessSettingsDto FitnessSettings { get; set; } = new();
            public EvolutionOptionsDto EvolutionOptions { get; set; } = new();
            // Optional: Timestamp beim Speichern
            public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        }

        public sealed class FitnessSettingsDto
        {
            public double WeightPitch { get; set; }
            public double WeightVelocity { get; set; }
            public double WeightHit { get; set; }
            public double WeightTie { get; set; }
            public double WeightPitchbend { get; set; }
            public double WeightModulation { get; set; }
        }

        public sealed class EvolutionOptionsDto
        {
            // möglichst vollständig gemäß deiner IEvolutionOptions
            public int GenerationLength { get; set; }
            public int PopulationSize { get; set; }
            public double DeletionRate { get; set; }
            public double InsertionRate { get; set; }
            public double SwapRate { get; set; }
            public double InversionRate { get; set; }
            public double TranspositionRate { get; set; }
            public double CrossoverRate { get; set; }
            public int TournamentSize { get; set; }
            public double ExtinctionRate { get; set; }
            public double ExtinctionThreshold { get; set; }
            public int MaxOffsprings { get; set; }
            public int? Seed { get; set; }
        }

        // Create DTO from primitive inputs (caller provides arrays / settings)
        public static SequenceSnapshot CreateSnapshot(
            ulong[][] originalSteps,
            ulong[] originalAttractor,
            int divider,
            byte channel,
            bool sendPitchbend = false,
            FitnessSettings? fitnessSettings = null,
            EvolutionOptions? evolutionOptions = null)
        {
            var snap = new SequenceSnapshot
            {
                OriginalSteps = originalSteps?.Select(a => a?.ToArray() ?? Array.Empty<ulong>()).ToArray() ?? Array.Empty<ulong[]>(),
                OriginalAttractor = originalAttractor?.ToArray() ?? Array.Empty<ulong>(),
                Divider = divider,
                Channel = channel,
                SendPitchbend = sendPitchbend,
                FitnessSettings = fitnessSettings == null ? new FitnessSettingsDto() : new FitnessSettingsDto
                {
                    WeightPitch = fitnessSettings.WeightPitch,
                    WeightVelocity = fitnessSettings.WeightVelocity,
                    WeightHit = fitnessSettings.WeightHit,
                    WeightTie = fitnessSettings.WeightTie,
                    WeightPitchbend = fitnessSettings.WeightPitchbend,
                    WeightModulation = fitnessSettings.WeightModulation
                }
            };

            if (evolutionOptions != null)
            {
                snap.EvolutionOptions = new EvolutionOptionsDto
                {
                    GenerationLength = evolutionOptions.GenerationLength,
                    PopulationSize = evolutionOptions.PopulationSize,
                    DeletionRate = evolutionOptions.DeletionRate,
                    InsertionRate = evolutionOptions.InsertionRate,
                    SwapRate = evolutionOptions.SwapRate,
                    InversionRate = evolutionOptions.InversionRate,
                    TranspositionRate = evolutionOptions.TranspositionRate,
                    CrossoverRate = evolutionOptions.CrossoverRate,
                    TournamentSize = evolutionOptions.TournamentSize,
                    ExtinctionRate = evolutionOptions.ExtinctionRate,
                    ExtinctionThreshold = evolutionOptions.ExtinctionThreshold,
                    MaxOffsprings = evolutionOptions.MaxOffsprings,
                    Seed = evolutionOptions.Seed
                };
            }

            return snap;
        }

        public static void Save(SequenceSnapshot snapshot, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(snapshot, Options);
            File.WriteAllText(path, json);
        }

        public static async Task SaveAsync(SequenceSnapshot snapshot, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, snapshot, Options).ConfigureAwait(false);
        }

        public static SequenceSnapshot Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var json = File.ReadAllText(path);
            var snap = JsonSerializer.Deserialize<SequenceSnapshot>(json, Options)
                       ?? throw new InvalidOperationException("Ungültige Snapshot-Datei");
            return snap;
        }

        public static async Task<SequenceSnapshot> LoadAsync(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            using var fs = File.OpenRead(path);
            var snap = await JsonSerializer.DeserializeAsync<SequenceSnapshot>(fs, Options).ConfigureAwait(false)
                       ?? throw new InvalidOperationException("Ungültige Snapshot-Datei");
            return snap;
        }

        public static string GetSettingsDirectory()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = Path.Combine(docs, "Egami", "Helix", "Recent");
            if (!Directory.Exists(result))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }

        public static string GetUserDirectory()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = Path.Combine(docs, "Egami", "Helix", "User");
            if (!Directory.Exists(result))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }


    }
}