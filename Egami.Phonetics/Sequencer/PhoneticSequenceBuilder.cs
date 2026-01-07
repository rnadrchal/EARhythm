using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Egami.Phonetics.IPA;
using Egami.Sequencer;
using Egami.Sequencer.Common;
using Melanchall.DryWetMidi.Common;

namespace Egami.Phonetics.Sequencer;

public sealed class PhoneticSequenceBuilder
{
    private readonly PhoneticSequencerSettings _settings;

    public PhoneticSequenceBuilder(PhoneticSequencerSettings? settings = null)
    {
        _settings = settings ?? new PhoneticSequencerSettings();
    }

    public MusicalSequence BuildFromIpa(string ipa)
    {
        if (string.IsNullOrWhiteSpace(ipa))
            return MusicalSequence.Empty;

        // Normalisieren (sicherer Umgang mit kombinierten Zeichen)
        ipa = ipa.Normalize(NormalizationForm.FormC);

        var tokens = ipa
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var steps = new List<SequenceStep>();
        var currentStepIndex = 0;

        var nextStressed = false;
        var lastVowelIndex = -1;

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            // Stress-Marker entfernen + Flag setzen
            if (token.Contains('ˈ'))
            {
                nextStressed = true;
                token = token.Replace("ˈ", string.Empty);
            }

            if (token.Contains('ˌ'))
            {
                token = token.Replace("ˌ", string.Empty);
            }

            if (token.Length == 0) continue;

            // Zerlege Token in bekannte Phonem‑Symbole (longest-match)
            var symbols = TokenizeToken(token);

            foreach (var sym in symbols)
            {
                if (!TryGetDefinition(sym, out var def))
                    continue;

                // Stress-Flag nur beim ersten Vokal/Diphthong konsumieren
                if (def.Kind == PhonemeKind.Vowel || def.Kind == PhonemeKind.Diphthong)
                {
                    var stressDur = nextStressed ? _settings.StressDurationFactor : 1.0;
                    var stressLoud = nextStressed ? _settings.StressLoudnessFactor : 1.0;
                    nextStressed = false;

                    switch (def.Kind)
                    {
                        case PhonemeKind.Vowel:
                            HandleVowel(def, stressDur, stressLoud, steps, ref currentStepIndex, ref lastVowelIndex);
                            break;

                        case PhonemeKind.Diphthong:
                            HandleDiphthong(def, stressDur, stressLoud, steps, ref currentStepIndex, ref lastVowelIndex);
                            break;
                    }
                }
                else
                {
                    // Konsonanten verarbeiten; Stress bleibt erhalten bis ein Vokal kommt
                    HandleConsonant(def, steps, ref currentStepIndex, ref lastVowelIndex);
                }
            }
        }

        return new MusicalSequence(steps);
    }

    private void HandleVowel(
        PhonemeDefinition def,
        double stressDur,
        double stressLoud,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        ref int lastVowelIndex)
    {
        if (def.StartDegreeOffset is null) return;

        var relDur = def.RelativeDuration * stressDur;
        var relLoud = def.RelativeLoudness * stressLoud;

        var lenSteps = Math.Max(1, (int)Math.Round(relDur * _settings.BaseStepsPerUnit));
        var degree = def.StartDegreeOffset.Value;

        var midiNote = ComputeMidiNote(degree);
        var velocity = ComputeVelocity(relLoud);

        var step = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: lenSteps,
            pitch: (SevenBitNumber)midiNote,
            velocity: (SevenBitNumber)velocity);

        steps.Add(step);
        lastVowelIndex = steps.Count - 1;
        currentStepIndex += lenSteps;
    }

    private void HandleDiphthong(
        PhonemeDefinition def,
        double stressDur,
        double stressLoud,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        ref int lastVowelIndex)
    {
        if (def.StartDegreeOffset is null || def.EndDegreeOffset is null)
            return;

        var relDur = def.RelativeDuration * stressDur;
        var relLoud = def.RelativeLoudness * stressLoud;
        var totalSteps = Math.Max(2, (int)Math.Round(relDur * _settings.BaseStepsPerUnit));

        var half1 = totalSteps / 2;
        var half2 = totalSteps - half1;

        var vel = ComputeVelocity(relLoud);

        // 1) Start-Vokal
        var midiStart = ComputeMidiNote(def.StartDegreeOffset.Value);
        var step1 = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: half1,
            pitch: (SevenBitNumber)midiStart,
            velocity: (SevenBitNumber)vel);

        steps.Add(step1);
        lastVowelIndex = steps.Count - 1;
        currentStepIndex += half1;

        // 2) Ziel-Vokal (Glide)
        var midiEnd = ComputeMidiNote(def.EndDegreeOffset.Value);
        var step2 = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: half2,
            pitch: (SevenBitNumber)midiEnd,
            velocity: (SevenBitNumber)vel);

        steps.Add(step2);
        lastVowelIndex = steps.Count - 1;
        currentStepIndex += half2;
    }

    private void HandleConsonant(
        PhonemeDefinition def,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        ref int lastVowelIndex)
    {
        if (def.ConsonantClass is null)
            return;

        if (lastVowelIndex < 0 || lastVowelIndex >= steps.Count)
        {
            // kein Vokal bisher -> optional ignorieren
            return;
        }

        // Falls aktueller Konsonant kein Frikativ ist, neutralisiere ggf. zuvor gesetzten Filter-CC
        // So wird ein neutraler CC-Wert gesendet und Ableton empfängt die Änderung.
        if (def.ConsonantClass.Value != ConsonantClass.Fricative)
        {
            var last = steps[lastVowelIndex];
            if (last.CcValues != null && last.CcValues.ContainsKey(_settings.FilterCcNumber))
            {
                var neutralized = last.WithCc(_settings.FilterCcNumber, _settings.FilterNeutralValue);
                steps[lastVowelIndex] = neutralized;
            }
        }

        switch (def.ConsonantClass.Value)
        {
            case ConsonantClass.Plosive:
                AddPlosiveAccent(def, steps, ref currentStepIndex, lastVowelIndex);
                break;

            case ConsonantClass.Fricative:
                AddFricativeFilter(def, steps, lastVowelIndex);
                break;

            case ConsonantClass.Nasal:
                AddNasalMicroGlide(def, steps, ref currentStepIndex, lastVowelIndex);
                break;

            case ConsonantClass.Liquid:
                AddLiquidTrill(def, steps, ref currentStepIndex, lastVowelIndex);
                break;

            case ConsonantClass.Other:
                // optional später behandeln
                break;
        }
    }

    private void AddPlosiveAccent(
        PhonemeDefinition def,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        int lastVowelIndex)
    {
        var last = steps[lastVowelIndex];
        // Kurzer Akzent mit erhöhter Velocity, gleicher Tonhöhe
        var accentLen = Math.Max(1,
            (int)Math.Round(last.LengthInSteps * _settings.PlosiveAccentFraction));

        var accentVel = (int)Math.Round(last.Velocity * 1.15);
        accentVel = Math.Clamp(accentVel, 1, 127);

        var ccCopy = last.CcValues != null ? last.CcValues.ToDictionary(kv => kv.Key, kv => kv.Value) : null;

        var accentStep = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: accentLen,
            pitch: last.Pitch,
            velocity: (SevenBitNumber)accentVel,
            pitchBend: last.PitchBend,
            ccValues: ccCopy);

        steps.Add(accentStep);
        currentStepIndex += accentLen;
    }

    private void AddFricativeFilter(
        PhonemeDefinition def,
        List<SequenceStep> steps,
        int lastVowelIndex)
    {
        var last = steps[lastVowelIndex];
        // Filter-CC hochziehen (z.B. hellere Artikulation)
        var stepWithFilter = last.WithCc(
            _settings.FilterCcNumber,
            _settings.FilterFricativeValue);

        steps[lastVowelIndex] = stepWithFilter;
    }

    private void AddNasalMicroGlide(
        PhonemeDefinition def,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        int lastVowelIndex)
    {
        var last = steps[lastVowelIndex];

        var tailLen = Math.Max(1,
            (int)Math.Round(last.LengthInSteps * _settings.NasalTailFraction));

        // Microglide: gleicher Pitch, aber leichter Pitchbend
        var bendSemitones = _settings.NasalGlideSemitones;
        var bendValue = SemitonesToPitchBend(bendSemitones);

        var ccCopy = last.CcValues != null ? last.CcValues.ToDictionary(kv => kv.Key, kv => kv.Value) : null;

        var nasalStep = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: tailLen,
            pitch: last.Pitch,
            velocity: last.Velocity,
            pitchBend: bendValue,
            ccValues: ccCopy);

        steps.Add(nasalStep);
        currentStepIndex += tailLen;
    }

    private void AddLiquidTrill(
        PhonemeDefinition def,
        List<SequenceStep> steps,
        ref int currentStepIndex,
        int lastVowelIndex)
    {
        var last = steps[lastVowelIndex];

        var trillTotalLen = Math.Max(2,
            (int)Math.Round(last.LengthInSteps * _settings.TrillFraction));

        var half = trillTotalLen / 2;
        var otherHalf = trillTotalLen - half;

        var interval = _settings.TrillIntervalSemitones;

        var baseMidi = last.Pitch;
        var upMidi = (SevenBitNumber)Math.Clamp((int)baseMidi + interval, 0, 127);
        var downMidi = (SevenBitNumber)Math.Clamp((int)baseMidi - interval, 0, 127);

        var ccCopyUp = last.CcValues != null ? last.CcValues.ToDictionary(kv => kv.Key, kv => kv.Value) : null;
        var ccCopyDown = last.CcValues != null ? last.CcValues.ToDictionary(kv => kv.Key, kv => kv.Value) : null;

        // zwei Steps: hoch → runter (oder umgekehrt)
        var stepUp = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: half,
            pitch: upMidi,
            velocity: last.Velocity,
            pitchBend: 0,
            ccValues: ccCopyUp);

        steps.Add(stepUp);
        currentStepIndex += half;

        var stepDown = new SequenceStep(
            stepIndex: currentStepIndex,
            lengthInSteps: otherHalf,
            pitch: downMidi,
            velocity: last.Velocity,
            pitchBend: 0,
            ccValues: ccCopyDown);

        steps.Add(stepDown);
        currentStepIndex += otherHalf;
    }

    private bool TryGetDefinition(string symbol, out PhonemeDefinition def)
    {
        symbol = symbol.Trim();
        if (PhonemeDefinitions.Map.TryGetValue(symbol, out def))
            return true;

        def = default!;
        return false;
    }

    private IEnumerable<string> TokenizeToken(string token)
    {
        // Map abrufen
        var map = PhonemeDefinitions.Map;
        if (map == null || map.Count == 0)
        {
            // Fallback: einzelne Zeichen
            for (int i = 0; i < token.Length; i++)
                yield return token[i].ToString();
            yield break;
        }

        // maximale Schlüssel-Länge ermitteln
        var maxLen = 0;
        foreach (var k in map.Keys)
            if (!string.IsNullOrEmpty(k) && k.Length > maxLen)
                maxLen = k.Length;

        int pos = 0;
        while (pos < token.Length)
        {
            var matched = false;
            var remaining = token.Length - pos;
            var lenLimit = Math.Min(maxLen, remaining);

            // längste Übereinstimmung zuerst versuchen
            for (int len = lenLimit; len >= 1; len--)
            {
                var candidate = token.Substring(pos, len);
                if (map.ContainsKey(candidate))
                {
                    yield return candidate;
                    pos += len;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                // unbekanntes Zeichen: als Einzelzeichen weitergeben
                yield return token[pos].ToString();
                pos++;
            }
        }
    }

    private int ComputeMidiNote(int degreeOffset)
    {
        var scale = _settings.Scale.GetDegrees();

        var idx = degreeOffset;
        while (idx < 0) idx += scale.Length;
        while (idx >= scale.Length) idx -= scale.Length;

        var semitoneOffset = scale[idx];
        var root = (int)_settings.RootNote;
        var midi = root + semitoneOffset;
        return Math.Clamp(midi, 0, 127);
    }

    private int ComputeVelocity(double relLoudness)
    {
        var baseVel = _settings.BaseVelocity;
        var v = (int)Math.Round(baseVel * relLoudness);
        return Math.Clamp(v, 1, 127);
    }

    /// <summary>
    /// sehr einfache Pitchbend-Berechnung (±8192 == ±PitchBendRangeSemitones).
    /// </summary>
    private int SemitonesToPitchBend(int semitones)
    {
        // Annahme: Range = PitchBendRangeSemitones, Vollbereich +-8192.
        // 1 Halbton == 8192 / Range
        var range = Math.Max(1, _settings.PitchBendRangeSemitones);
        var perSemitone = 8192 / range;
        var value = perSemitone * semitones;
        return Math.Clamp(value, -8192, 8191);
    }
}
