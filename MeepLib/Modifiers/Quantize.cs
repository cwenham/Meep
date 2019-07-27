using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Map a range of numeric values on a continuum to a quantified scale such as the frequency in Hz of musical notes
    /// or colours of the spectrum, Benford's distribution, and so-on
    /// </summary>
    /// <remarks>In keeping with the modular synthesizer inspiration for Meep, Quantize performs a similar function to
    /// a quantizer in a modular synth that can take an input voltage or frequency and "fix" it to the closest musical
    /// note. This can be used to correct for natural variances that would otherwise result in the instrument drifting
    /// out of tune.
    ///
    /// <para>Quantize expects Messages that derive from NumericMessage and provide a decimal/float value, and maps
    /// these to a named scale such as "Western musical" (which assumes the values are in Hz) or "Benford" and so-on.</para>
    ///
    /// <para>BaseScaleLibrary contains some built-in scales, plugins can provide other named scales with
    /// <see cref="ScaleAttribute"/></para>.</remarks>
    [Macro(Name = "Quantize", DefaultProperty = "Scale", Position = MacroPosition.Downstream)]
    public class Quantize : AMessageModule
    {
        /// <summary>
        /// Name of the scale to use, E.G. "Western Musical"
        /// </summary>
        public string Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;

                var selections = GetSelections();
                if (selections.ContainsKey(_scale))
                    Points = selections[_scale];
                else
                    throw new ArgumentException($"{_scale} not found. Check for misspelling or that its containing plugin has been loaded.");
            }
        }
        private string _scale;

        /// <summary>
        /// Points on the scale as a sorted array
        /// </summary>
        public decimal[] Points { get; set; }

        /// <summary>
        /// Where to get the numeric value to be quantized
        /// </summary>
        /// <remarks>Defaults to Number from a NumericMessage.</remarks>
        public DataSelector From { get; set; } = numberSelector;
        private const string numberSelector = "{msg.Number}";

        /// <summary>
        /// True to return the ordinal position of the quantized value
        /// </summary>
        /// <remarks>After rounding to the closest value of the scale, the ordinal position is returned instead of
        /// the value. So for Western Musical this would make it return the MIDI number of the note, which begins at 0
        /// for C and goes up for each note and accidental.</remarks>
        public DataSelector Ordinal { get; set; } = falseSelector;
        private const string falseSelector = "False";

        public static Dictionary<string, decimal[]> GetSelections()
        {
            var staticClasses = from a in AppDomain.CurrentDomain.GetAssemblies()
                                from t in a.GetTypes()
                                where t.IsAbstract && t.IsSealed
                                select t;

            var staticLists = from t in staticClasses
                              from f in t.GetFields()
                              where f.FieldType == typeof(decimal[])
                                 && f.IsStatic
                              let scaleAttrib = f.GetCustomAttributes(typeof(ScaleAttribute), true).FirstOrDefault()
                              where scaleAttrib != null
                              let scale = f.GetValue(null) as decimal[]
                              where scale != null
                              select new
                              {
                                  Scale = scale,
                                  Attrib = scaleAttrib as ScaleAttribute
                              };

            return staticLists.ToDictionary(x => x.Attrib.Name, y => y.Scale);
        }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            decimal num = 0m;

            NumericMessage nMsg = msg as NumericMessage;
            if (nMsg != null && From.Value is numberSelector)
                // Skip overhead of evaluating DataSelector expression if the default was used
                num = nMsg.Number;
            else
            {
                var selected = await From.TrySelectDecimalAsync(context);
                if (selected.Parsed)
                    num = selected.Value;
                // Can't quantize a non-numeric value, so we shouldn't let this message through
                return null;
            }

            int position = Array.BinarySearch(Points, num);
            
            if (position >= 0 && Ordinal.Value is falseSelector)
                // Message's value was already on the scale
                return msg;

            // At this point we know we're going to be returning a new message with something
            var newMsg = new NumericMessage
            {
                 DerivedFrom = msg
            };

            int absPosition = Math.Abs(position);

            if (absPosition >= Points.Length)
                // Value was off the scale! Aaiiiieee! Return the highest point on the scale
                return SetValue(newMsg, await DesiredValue(context,Points.Length - 1));

            if (absPosition == 0)
                // Below the bottom of the scale. Return the lowest point
                return SetValue(newMsg, await DesiredValue(context,0));

            decimal before = Points[absPosition - 1];
            decimal after = Points[absPosition];

            // Which side is it closer to?
            if (num - before > after - num)
                return SetValue(newMsg, await DesiredValue(context, absPosition));
            else
                return SetValue(newMsg, await DesiredValue(context, absPosition - 1));
        }

        /// <summary>
        /// Return the desired value according to config
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Will be either the scale value, or its ordinal position, depending on what Ordinal is set to.</returns>
        private async Task<decimal> DesiredValue(MessageContext context, int index)
        {
            if (Ordinal.Value is falseSelector || Ordinal.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return Points[index];

            string returnOrdinal = await Ordinal.SelectStringAsync(context);
            if (returnOrdinal.Equals("true", StringComparison.OrdinalIgnoreCase))
                return index;

            return Points[index];
        }

        /// <summary>
        /// Convenience method so we can return and do an assignment in one go
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private NumericMessage SetValue(NumericMessage msg, decimal value)
        {
            msg.Number = value;
            return msg;
        }

    }

    /// <summary>
    /// Mark a list of quantized values (a scale)
    /// </summary>
    /// <remarks>Subclasses LibraryBookAttribute, so any Scale can also be used as the source of
    /// <see cref="MeepLib.Sources.Enumerate"/>.
    ///
    /// <para>When defining custom scales, always provide them as a sorted array because <see cref="Quantize"/> will
    /// perform a binary search.</para></remarks>
    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class ScaleAttribute : MeepLib.Sources.LibraryBookAttribute
    {
        readonly int offset;

        public ScaleAttribute(string name, int ordinalOffset = 0) : base(name)
        {
            this.offset = ordinalOffset;
        }
    }

    /// <summary>
    /// Built-in scales
    /// </summary>
    /// <remarks>Lists of doubles or decimals. Quantize will round to the closest value.</remarks>
    public static class BaseScaleLibrary
    {
        /// <summary>
        /// Western musical scale in Hz
        /// </summary>
        /// <remarks>Ordinal position in the array is the MIDI number for the note, up to 127.</remarks>
        [Scale("Western Musical")]
        public static decimal[] WesternMusical = 
        {
             8.176m,     // C,  MIDI 0
             8.662m,     // C#, MIDI 1
             9.117m,     // D,  MIDI 2
             9.723m,     // D#, MIDI 3
             10.301m,    // E,  MIDI 4
             10.913m,    // F,  MIDI 5
             11.562m,    // F#, MIDI 6
             12.250m,    // G,  MIDI 7
             12.978m,    // G#, MIDI 8

             // Subsubcontra
             13.75m,     // A,  MIDI 9
             14.568m,    // A#, MIDI 10
             15.434m,    // B,  MIDI 11
             16.352m,    // C,  MIDI 12
             17.324m,    // C#, MIDI 13
             18.354m,    // D,  MIDI 14
             19.445m,    // D#, MIDI 15
             20.601m,    // E,  MIDI 16
             21.826m,    // F,  MIDI 17
             23.124m,    // F#, MIDI 18
             24.499m,    // G,  MIDI 19
             25.956m,    // G#, MIDI 20

             // Sub-contra
             27.50m,     // A,  MIDI 21 (Piano lowest)
             29.135m,    // A#, MIDI 22
             30.867m,    // B,  MIDI 23
             32.703m,    // C,  MIDI 24
             34.648m,    // C#, MIDI 25
             36.708m,    // D,  MIDI 26
             38.89m,     // D#, MIDI 27
             41.203m,    // E,  MIDI 28
             43.653m,    // F,  MIDI 29
             46.249m,    // F#, MIDI 30
             48.999m,    // G,  MIDI 31
             51.913m,    // G#, MIDI 32

             // Contra
             55.0m,      // A,  MIDI 33
             58.27m,     // A#, MIDI 34
             61.735m,    // B,  MIDI 35
             65.406m,    // C,  MIDI 36
             69.296m,    // C#, MIDI 37
             73.416m,    // D,  MIDI 38
             77.782m,    // D#, MIDI 39
             82.407m,    // E,  MIDI 40
             87.307m,    // F,  MIDI 41
             92.499m,    // F#, MIDI 42
             97.999m,    // G,  MIDI 43
             103.826m,   // G#, MIDI 44

             // Great
             110m,       // A,  MIDI 45
             116.541m,   // A#, MIDI 46
             123.471m,   // B,  MIDI 47
             130.813m,   // C,  MIDI 48
             138.591m,   // C#, MIDI 49
             146.832m,   // D,  MIDI 50
             155.564m,   // D#, MIDI 51
             164.814m,   // E,  MIDI 52
             174.614m,   // F,  MIDI 53
             184.997m,   // F#, MIDI 54
             195.998m,   // G,  MIDI 55
             207.652m,   // G#, MIDI 56

             // Small
             220m,       // A,  MIDI 57
             233.082m,   // A#, MIDI 58
             246.942m,   // B,  MIDI 59
             261.626m,   // C,  MIDI 60 (Middle C)
             277.183m,   // C#, MIDI 61
             293.665m,   // D,  MIDI 62
             311.127m,   // D#, MIDI 63
             329.628m,   // E,  MIDI 64
             349.228m,   // F,  MIDI 65
             369.994m,   // F#, MIDI 66
             391.995m,   // G,  MIDI 67
             415.305m,   // G#, MIDI 68

             // One-lined
             440m,       // A,  MIDI 69
             466.164m,   // A#, MIDI 70
             493.883m,   // B,  MIDI 71
             523.251m,   // C,  MIDI 72
             554.365m,   // C#, MIDI 73
             587.33m,    // D,  MIDI 74
             622.254m,   // D#, MIDI 75
             659.255m,   // E,  MIDI 76
             698.457m,   // F,  MIDI 77
             739.989m,   // F#, MIDI 78
             783.991m,   // G,  MIDI 79
             830.609m,   // G#, MIDI 80

             // Two-lined
             880m,       // A,  MIDI 81
             932.328m,   // A#, MIDI 82
             987.767m,   // B,  MIDI 83
             1046.502m,  // C,  MIDI 84
             1108.731m,  // C#, MIDI 85
             1174.659m,  // D,  MIDI 86
             1244.508m,  // D#, MIDI 87
             1318.510m,  // E,  MIDI 88
             1396.913m,  // F,  MIDI 89
             1479.978m,  // F#, MIDI 90
             1567.982m,  // G,  MIDI 91
             1661.219m,  // G#, MIDI 92

             // Three-lined
             1760m,      // A,  MIDI 93
             1864.665m,  // A#, MIDI 94
             1975.533m,  // B,  MIDI 95
             2093.005m,  // C,  MIDI 96
             2217.461m,  // C#, MIDI 97
             2349.318m,  // D,  MIDI 98
             2489.016m,  // D#, MIDI 99
             2637.021m,  // E,  MIDI 100
             2793.826m,  // F,  MIDI 101
             2959.956m,  // F#, MIDI 102
             3135.964m,  // G,  MIDI 103
             3322.438m,  // G#, MIDI 104

             // Four-lined
             3520m,      // A,  MIDI 105
             3729.31m,   // A#, MIDI 106
             3951.066m,  // B,  MIDI 107
             4186.009m,  // C,  MIDI 108 (Piano highest)
             4434.922m,  // C#, MIDI 109
             4698.637m,  // D,  MIDI 110
             4978.032m,  // D#, MIDI 111
             5274.042m,  // E,  MIDI 112
             5587.652m,  // F,  MIDI 113
             5919.912m,  // F#, MIDI 114
             6271.928m,  // G,  MIDI 115
             6644.876m,  // G#, MIDI 116

             // Five-lined
             7040m,      // A,  MIDI 117
             7458.62m,   // A#, MIDI 118
             7902.133m,  // B,  MIDI 119
             8372.018m,  // C,  MIDI 120
             8869.844m,  // C#, MIDI 121
             9397.273m,  // D,  MIDI 122
             9956.063m,  // D#, MIDI 123
             10548.082m, // E,  MIDI 124
             11175.303m, // F,  MIDI 125
             11839.822m, // F#, MIDI 126
             12543.854m, // G,  MIDI 127
             13289.750m, // G#

             // Six-lined
             14080m,     // A
             14917.240m, // A#
             15804.266m, // B
             16744.036m, // C
             17739.688m, // C#
             18794.545m, // D
             19912.127m  // D#
        };

        /// <summary>
        /// Benford distribution
        /// </summary>
        /// <remarks>Benford's Law covers the statistical occurance of the first digit in naturally occurring numbers.
        /// So the digit '1' appears 30.1% of the time, 2 appears 17.6%, and so-on. By natural numbers, we mean things
        /// like city/country populations, lengths of rivers, DOW Jones average, and so-on. "The first million is always
        /// the hardest", so doubling it is a little easier, and tripling is a little easier than that, hence the
        /// distribution.</remarks>
        [Scale("Benford", 1)]
        public static decimal[] Benford = 
        {
             30.1m,
             17.6m,
             12.5m,
             9.7m,
             7.9m,
             6.7m,
             5.8m,
             5.1m,
             4.6m
        };

        /// <summary>
        /// Visible light frequencies, in Hz
        /// </summary>
        /// <remarks>Scale is set to the beginning of each hue's range</remarks>
        [Scale("Light Hz")]
        public static decimal[] RoygbivHz =
        {
            // C# 8's new separator support comes in handy here
            300_000_000_000_000,   // Infrared
            430_000_000_000_000,   // Red
            480_000_000_000_000,   // Orange
            510_000_000_000_000,   // Yellow
            540_000_000_000_000,   // Green
            580_000_000_000_000,   // Cyan
            610_000_000_000_000,   // Blue
            670_000_000_000_000,   // Violet
        };

        /// <summary>
        /// Visible light frequencies, in Nanometers
        /// </summary>
        /// <remarks>Scale is set to the beginning of each hue's range</remarks>
        [Scale("Light Nm")]
        public static decimal[] RoygbivNm =
        {
            1000,       // Infrared
             700,       // Red
             635,       // Orange
             590,       // Yellow
             560,       // Green
             520,       // Cyan
             490,       // Blue
             450,       // Violet
             300,       // Near ultraviolet
             200,       // Far ultraviolet
        };

        //ToDo: TV channel frequencies
    }
}
