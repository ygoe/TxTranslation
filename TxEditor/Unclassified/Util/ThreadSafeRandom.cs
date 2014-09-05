using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Unclassified.Util
{
	public static class ThreadSafeRandom
	{
		[ThreadStatic]
		private static Random rnd;

		public static Random Random
		{
			get
			{
				return rnd ?? (rnd = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));
			}
		}

		public static int Next()
		{
			return Random.Next();
		}

		public static int Next(int maxValue)
		{
			return Random.Next(maxValue);
		}

		public static int Next(int minValue, int maxValue)
		{
			return Random.Next(minValue, maxValue);
		}

		public static void NextBytes(byte[] buffer)
		{
			Random.NextBytes(buffer);
		}

		public static double NextDouble()
		{
			return Random.NextDouble();
		}
	}
}
