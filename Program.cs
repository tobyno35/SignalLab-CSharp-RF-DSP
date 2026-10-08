
using System;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

const int sampleRate = 48000;
const int carrierHz = 2000;
const int samplesPerBit = 480;
const string transmitted = "10110010";

var random = new Random(42);
var samples = new double[transmitted.Length * samplesPerBit];

for (int i = 0; i < samples.Length; i++)
{
    int bitIndex = i / samplesPerBit;
    double amplitude = transmitted[bitIndex] == '1' ? 1.0 : 0.0;
    double time = (double)i / sampleRate;

    double carrier = amplitude *
        Math.Cos(2 * Math.PI * carrierHz * time);

    double noise = (random.NextDouble() - 0.5) * 0.1;
    samples[i] = carrier + noise;
}

// FFT analysis
var spectrum = samples
    .Select(x => new Complex(x, 0))
    .ToArray();

Fourier.Forward(spectrum, FourierOptions.Matlab);

int peakBin = Enumerable.Range(1, spectrum.Length / 2 - 1)
    .OrderByDescending(i => spectrum[i].Magnitude)
    .First();

double peakHz = (double)peakBin * sampleRate / spectrum.Length;

// OOK demodulation using RMS energy
string recovered = "";

for (int bit = 0; bit < transmitted.Length; bit++)
{
    var segment = samples
        .Skip(bit * samplesPerBit)
        .Take(samplesPerBit);

    double rms = Math.Sqrt(segment.Average(x => x * x));
    recovered += rms > 0.35 ? "1" : "0";
}

Console.WriteLine("=== SignalLab RF/DSP ===");
Console.WriteLine($"Sample rate: {sampleRate} Hz");
Console.WriteLine($"Carrier: {carrierHz} Hz");
Console.WriteLine($"FFT peak: {peakHz:F1} Hz");
Console.WriteLine($"Transmitted: {transmitted}");
Console.WriteLine($"Recovered:   {recovered}");
Console.WriteLine($"Match: {transmitted == recovered}");

string wavPath = @"C:\RF-Lab\captures\ook_test.wav";

using (var writer = new BinaryWriter(File.Create(wavPath)))
{
    int dataSize = samples.Length * sizeof(short);

    writer.Write("RIFF"u8.ToArray());
    writer.Write(36 + dataSize);
    writer.Write("WAVE"u8.ToArray());

    writer.Write("fmt "u8.ToArray());
    writer.Write(16);
    writer.Write((short)1); // PCM
    writer.Write((short)1); // Mono
    writer.Write(sampleRate);
    writer.Write(sampleRate * sizeof(short));
    writer.Write((short)sizeof(short));
    writer.Write((short)16);

    writer.Write("data"u8.ToArray());
    writer.Write(dataSize);

    foreach (double sample in samples)
    {
        short pcm = (short)(
            Math.Clamp(sample, -1.0, 1.0) * short.MaxValue);
        writer.Write(pcm);
    }
}

Console.WriteLine($"WAV saved: {wavPath}");
