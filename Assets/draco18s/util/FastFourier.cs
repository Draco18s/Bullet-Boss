using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.draco18s.util
{
	public static class FastFourier
	{
		public static List<Vector2> ProcessData(List<float> i_amplitude, float sample_time)
		{
			int i, sample_size, sample_count;
			sample_count = i_amplitude.Count;
			sample_size = i_amplitude.Count;
			
			List<Vector2> inpt_sample = i_amplitude.Select(a => new Vector2(a, 0)).ToList();

			while (IsPowerOfTwo(sample_count) == false)
			{
				sample_count++;
				// Add zero at end to make it power of 2
				// As the FFT operates on inputs that contain an integer power of two number of samples, 
				// the input data length will be augmented by zero padding at the end.
				inpt_sample.Add(new Vector2(0, 0));
			}

			int FFT_output_length;
			List<Vector2> outpt_signal_shift = new List<Vector2>();
			outpt_signal_shift = FFT(inpt_sample);
			FFT_output_length = outpt_signal_shift.Count;

			List<Vector2> outpt_signal = new List<Vector2>();

			for (i = (FFT_output_length / 2); i < FFT_output_length; i++)
			{
				outpt_signal.Add(outpt_signal_shift[i]);
			}

			for (i = 0; i < (FFT_output_length / 2); i++)
			{
				outpt_signal.Add(outpt_signal_shift[i]);
			}
			/*float frequency_resoultion = ((float)sample_size / (float)(sample_time * FFT_output_length));
			List<float> outpt_freq = new List<float>();

			for (i = -(FFT_output_length / 2); i < (FFT_output_length / 2) + 1; i++)
			{
				outpt_freq.Add((i * frequency_resoultion));
			}*/

			return outpt_signal;
		}

		public static bool IsPowerOfTwo(int n)
		{
			return (n != 0) && (n & (n - 1)) == 0;
		}

		public static List<Vector2> FFT(List<Vector2> inpt_signal)
		{
			int i;
			int N = inpt_signal.Count;
			if (N == 1)
				return inpt_signal;

			// Even array
			List<Vector2> evenList = new List<Vector2>();
			for (i = 0; i < (N / 2); i++)
			{
				evenList.Add(inpt_signal[2 * i]);
			}
			evenList = FFT(evenList);

			// Odd array
			List<Vector2> oddList = new List<Vector2>();
			for (i = 0; i < (N / 2); i++)
			{
				oddList.Add(inpt_signal[(2 * i) + 1]);
			}
			oddList = FFT(oddList);

			// Result
			Vector2[] result = new Vector2[N];

			for (i = 0; i < (N / 2); i++)
			{
				float w = (-2.0f * i * Mathf.PI) / N;
				Vector2 wk = new Vector2(Mathf.Cos(w), Mathf.Sin(w));
				Vector2 even = evenList[i];
				Vector2 odd = oddList[i];

				result[i] = even + (wk * odd);
				result[i + (N / 2)] = even - (wk * odd);
			}
			return result.ToList();
		}
	}
}
