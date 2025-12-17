using Egami.Rhythm.Midi.Extensions;
using Melanchall.DryWetMidi.MusicTheory;
using System.Reflection;
using Melanchall.DryWetMidi.Common;

namespace Egami.Sequencer.Tests
{
    [TestClass]
    public sealed class TextSequencerTests
    {
        [TestMethod]
        [DataRow("Wortklauber", "Major", NoteName.C, 4)]
        public void SequenceIsGeneratedProperly(string text, string scaleName, NoteName rootNote, int octave)
        {
            text = text.ToUpper();

            var intervals = GetScaleIntervals(scaleName);

            var scale = intervals?.ToIndices().ToArray();
            var indices = text.Select(c => c % scale.Length).ToList();
            var pitches = indices.Select(i => scale[i] + (int)rootNote + octave * 12).ToArray();
            var lengths = pitches.Select(p => pitches.Count(x => x == p)).ToArray();
            var maxLength = lengths.Max();
            var stepIndex = 0;
            var steps = new List<SequenceStep>();
            for (int i = 0; i < pitches.Length; ++i)
            {
                var length = lengths[i];

                steps.Add(new SequenceStep(stepIndex, maxLength - length + 1, (SevenBitNumber)pitches[i], (SevenBitNumber)60));

                stepIndex += length;
            }


            var sequence = new MusicalSequence(steps);
            
        }

        private static IEnumerable<Interval>? GetScaleIntervals(string scaleName)
        {
            // scaleName = "Major"
            Type scaleType = typeof(ScaleIntervals);

            // find static property or field
            var prop = scaleType.GetProperty(scaleName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            var field = scaleType.GetField(scaleName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

            if (prop == null && field == null)
                throw new InvalidOperationException($"Scale '{scaleName}' not found on {scaleType.Name}.");

            var scaleValue =  (IEnumerable<Interval>)(prop != null ? prop.GetValue(null) : field.GetValue(null));
            return scaleValue;
        }
    }
}
