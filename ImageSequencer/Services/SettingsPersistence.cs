using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Egami.Imaging.Extensions;
using Egami.Imaging.Midi;
using Egami.Imaging.Visiting;
using ImageSequencer.Models;

namespace ImageSequencer.Services;

public static class SettingsPersistence
{
    private static JsonSerializerOptions Options => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static Task SaveAsync(ApplicationSettings settings, string path)
    {
        var dto = ToDto(settings);
        var json = JsonSerializer.Serialize(dto, Options);
        File.WriteAllText(path, json);
        return Task.CompletedTask;
    }

    public static void Save(ApplicationSettings settings, string path) => SaveAsync(settings, path).GetAwaiter().GetResult();

    public static Task LoadAsync(ApplicationSettings settings, string path, bool loadBitmap = false)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Settings file not found", path);

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<SettingsDto>(json, Options)
                  ?? throw new InvalidOperationException("Ungültige Einstellungsdatei");

        ApplyDto(settings, dto, loadBitmap);
        return Task.CompletedTask;
    }

    public static void Load(ApplicationSettings settings, string path, bool loadBitmap = false) => LoadAsync(settings, path, loadBitmap).GetAwaiter().GetResult();

    private static SettingsDto ToDto(ApplicationSettings s) => new()
    {
        FilePath = s.FilePath,
        IsVisiting = s.IsVisiting,
        Channel = s.Channel,
        Divider = s.Divider,
        VisitorType = s.VisitorType,
        GridCols = s.GridCols,
        GridRows = s.GridRows,
        PitchColorToCvType = s.PitchColorToCvType,
        PitchBaseColor = s.PitchBaseColor,
        VelocityColorToCvType = s.VelocityColorToCvType,
        VelocityBaseColor = s.VelocityBaseColor,
        PitchbendColorToCvType = s.PitchbendColorToCvType,
        PitchbendBaseColor = s.PitchbendBaseColor,
        ControlChangeColorToCvType = s.ControlChangeColorToCvType,
        ControlChangeBaseColor = s.ControlChangeBaseColor,
        Legato = s.Legato,
        SendNoteOn = s.SendNoteOn,
        SendPitchbendOn = s.SendPitchbendOn,
        SendControlChangeOn = s.SendControlChangeOn,
        ControlChangeNumber = s.ControlChangeNumber,
        TonalRangeLower = s.TonalRangeLower,
        TonalRangeUpper = s.TonalRangeUpper,
        // TransformSettings (nur relevante, serialisierbare Felder)
        Transform = s.TransformSettings is null ? null : new TransformDto
        {
            Scale = s.TransformSettings.Scale,
            ColorModel = s.TransformSettings.ColorModel,
            Stretch = s.TransformSettings.Stretch
        }
    };

    private static void ApplyDto(ApplicationSettings s, SettingsDto dto, bool loadBitmap)
    {
        s.FilePath = dto.FilePath;
        s.IsVisiting = dto.IsVisiting;
        s.Channel = dto.Channel;
        s.Divider = dto.Divider;
        s.VisitorType = dto.VisitorType;
        s.GridCols = dto.GridCols;
        s.GridRows = dto.GridRows;
        s.PitchColorToCvType = dto.PitchColorToCvType;
        s.PitchBaseColor = dto.PitchBaseColor;
        s.VelocityColorToCvType = dto.VelocityColorToCvType;
        s.VelocityBaseColor = dto.VelocityBaseColor;
        s.PitchbendColorToCvType = dto.PitchbendColorToCvType;
        s.PitchbendBaseColor = dto.PitchbendBaseColor;
        s.ControlChangeColorToCvType = dto.ControlChangeColorToCvType;
        s.ControlChangeBaseColor = dto.ControlChangeBaseColor;
        s.Legato = dto.Legato;
        s.SendNoteOn = dto.SendNoteOn;
        s.SendPitchbendOn = dto.SendPitchbendOn;
        s.SendControlChangeOn = dto.SendControlChangeOn;
        s.ControlChangeNumber = dto.ControlChangeNumber;
        s.TonalRangeLower = dto.TonalRangeLower;
        s.TonalRangeUpper = dto.TonalRangeUpper;

        if (dto.Transform != null)
        {
            if (s.TransformSettings == null)
                s.TransformSettings = new TransformSettings(s);

            s.TransformSettings.Scale = dto.Transform.Scale;
            s.TransformSettings.ColorModel = dto.Transform.ColorModel;
            s.TransformSettings.Stretch = dto.Transform.Stretch;
        }

        if (loadBitmap && !string.IsNullOrWhiteSpace(dto.FilePath) && File.Exists(dto.FilePath))
        {
            // gleiche Logik wie in ImageViewer.OpenBitmap
            s.FilePath = dto.FilePath;
            var wb = new WriteableBitmap(new BitmapImage(new Uri(dto.FilePath)));
            s.Bitmap = wb;
            s.Original = wb.Clone();
            s.RenderTarget = new WriteableBitmap(wb.PixelWidth, wb.PixelHeight, wb.DpiX, wb.DpiY, wb.Format, null);
            if (s.TransformSettings == null)
                s.TransformSettings = new TransformSettings(s);
            s.RequestReset();
        }
    }

    private record SettingsDto
    {
        public string? FilePath { get; init; }
        public bool IsVisiting { get; init; }
        public int Channel { get; init; }
        public int Divider { get; init; }
        public BitmapVisitorType VisitorType { get; init; }
        public int GridCols { get; init; }
        public int GridRows { get; init; }
        public ColorToCvType PitchColorToCvType { get; init; }
        public BaseColor PitchBaseColor { get; init; }
        public ColorToCvType VelocityColorToCvType { get; init; }
        public BaseColor VelocityBaseColor { get; init; }
        public ColorToCvType PitchbendColorToCvType { get; init; }
        public BaseColor PitchbendBaseColor { get; init; }
        public ColorToCvType ControlChangeColorToCvType { get; init; }
        public BaseColor ControlChangeBaseColor { get; init; }
        public bool Legato { get; init; }
        public bool SendNoteOn { get; init; }
        public bool SendPitchbendOn { get; init; }
        public bool SendControlChangeOn { get; init; }
        public byte ControlChangeNumber { get; init; }
        public byte TonalRangeLower { get; init; }
        public byte TonalRangeUpper { get; init; }
        public TransformDto? Transform { get; init; }
    }

    private record TransformDto
    {
        public double Scale { get; init; }
        public ColorModel ColorModel { get; init; }
        public Stretch Stretch { get; init; }
    }
}