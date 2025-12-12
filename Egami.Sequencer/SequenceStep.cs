using Melanchall.DryWetMidi.Common;

namespace Egami.Sequencer
{
    public class SequenceStep
    {
        public int StepIndex { get; }
        public int LengthInSteps { get; }
        public SevenBitNumber Pitch { get; }
        public SevenBitNumber Velocity { get; }

        public int PitchBend { get; }

        private readonly Dictionary<int, SevenBitNumber> _ccValues;
        public IReadOnlyDictionary<int, SevenBitNumber> CcValues => _ccValues;

        public SequenceStep(
            int stepIndex,
            int lengthInSteps,
            SevenBitNumber pitch,
            SevenBitNumber velocity,
            int pitchBend = 0,
            IDictionary<int, SevenBitNumber>? ccValues = null)
        {
            StepIndex = stepIndex;
            LengthInSteps = lengthInSteps;
            Pitch = pitch;
            Velocity = velocity;
            PitchBend = pitchBend;
            _ccValues = ccValues != null
                ? new Dictionary<int, SevenBitNumber>(ccValues)
                : new Dictionary<int, SevenBitNumber>();
        }

        /// <summary>
        /// Sets step parameter
        /// </summary>
        /// <param name="stepIndex"></param>
        /// <param name="lengthInSteps"></param>
        /// <param name="pitch"></param>
        /// <param name="velocity"></param>
        /// <param name="pitchBend"></param>
        /// <param name="ccValues"></param>
        /// <returns></returns>
        public SequenceStep With(
            int? stepIndex = null,
            int? lengthInSteps = null,
            SevenBitNumber? pitch = null,
            SevenBitNumber? velocity = null,
            int? pitchBend = null,
            IDictionary<int, SevenBitNumber>? ccValues = null)
        {
            return new SequenceStep(
                stepIndex ?? StepIndex,
                lengthInSteps ?? LengthInSteps,
                pitch ?? Pitch,
                velocity ?? Velocity,
                pitchBend ?? PitchBend,
                ccValues ?? _ccValues);
        }

        /// <summary>
        /// Adds or updates single CC-Value to step
        /// </summary>
        /// <param name="ccNumber"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SequenceStep WithCc(int ccNumber, SevenBitNumber value)
        {
            if (ccNumber is < 0 or > 127)
                throw new ArgumentOutOfRangeException(nameof(ccNumber));

            var newDict = new Dictionary<int, SevenBitNumber>(_ccValues)
            {
                [ccNumber] = value
            };
            return With(ccValues: newDict);
        }

        /// <summary>
        /// Adds multiple CC-Values to step
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SequenceStep WithCcs(IDictionary<int, SevenBitNumber> values)
        {
            var newDict = new Dictionary<int, SevenBitNumber>(_ccValues);
            foreach (var kv in values)
            {
                if (kv.Key is < 0 or > 127)
                    throw new ArgumentOutOfRangeException(nameof(values), "Invalid CC-Number");
                newDict[kv.Key] = kv.Value;
            }

            return With(ccValues: newDict);
        }

        /// <summary>
        /// Removes single CC-Value from step
        /// </summary>
        /// <param name="ccNumber"></param>
        /// <returns></returns>
        public SequenceStep WithoutCc(int ccNumber)
        {
            if (!_ccValues.ContainsKey(ccNumber))
                return this;

            var newDict = new Dictionary<int, SevenBitNumber>(_ccValues);
            newDict.Remove(ccNumber);
            return With(ccValues: newDict);
        }

        /// <summary>
        /// Removes all CC-Values from step
        /// </summary>
        public SequenceStep WithoutAllCcs()
        {
            if (_ccValues.Count == 0) return this;
            return With(ccValues: new Dictionary<int, SevenBitNumber>());
        }
    }
}
