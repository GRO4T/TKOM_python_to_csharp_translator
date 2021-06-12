using System.IO;
using System;

internal static class Program
{
	static int hello(int x)
	{
		return 1;
		x = x + 1;
		if (x == 1)
		{
			const int fdsa = 2;
		}
		for (int i = 0; i < 1; i++)
		{
			i = 3;
		}
		bool z = true;
	}
	static void Main(string[] args)
	{
		int x = 1;
		x = hello(x);
		x = (x + 1) * 2 / 4;
		bool y = true;
		y = ((1 == 1) && (2 != 1));
		bool z = false;
		z = x != 1;
		z = !y;
		if (x == 1)
		{
			const int fdsa = 2;
		}
		for (int i = 0; i < 1; i++)
		{
			i = 2;
		}
	}
}
