using System.Diagnostics;
using System.Text;

namespace Egami.Phonetics.IPA;

public sealed class IpaTranscriber
{
    private readonly string _espeakPath;

    public IpaTranscriber()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var eSpeakFolder = Path.Combine(programFiles, "eSpeak NG");
        if (!Path.Exists(eSpeakFolder))
        {
            throw new FileNotFoundException("eSpeak NG folder not found in Program Files.");
        }

        _espeakPath = Path.Combine(eSpeakFolder, "espeak-ng.exe");
        if (!File.Exists(_espeakPath))
        {
            throw new FileNotFoundException("eSpeak NG executable not found.");
        }
    }

    /// <summary>
    /// Transkribiert deutschen Text nach IPA mittels eSpeak-NG.
    /// </summary>
    public async Task<string[]> ToIpaAsync(
        string text,
        string language = "de",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var startInfo = new ProcessStartInfo
        {
            FileName = _espeakPath,
            // --ipa   : IPA-Ausgabe
            // -v de   : deutsche Stimme/Regeln
            // --stdin : Text über Standard Input einlesen
            // --sep=  : Trennzeichen zwischen Phonemen (hier Leerzeichen)
            Arguments = $"--ipa -v{language} --stdin --sep= ",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException("Konnte espeak-ng Prozess nicht starten.");

        // Text in stdin schreiben
        await process.StandardInput.WriteAsync(text);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        // Ausgabe lesen
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(outputTask, errorTask);

        await process.WaitForExitAsync(cancellationToken);

        var output = outputTask.Result.Trim();
        var error = errorTask.Result.Trim();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                $"espeak-ng Fehler (ExitCode {process.ExitCode}): {error}");
        }

        return output.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
    }
}