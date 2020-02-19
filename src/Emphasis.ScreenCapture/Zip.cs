using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Emphasis.ScreenCapture
{
	public static class Helpers
	{
		public static async IAsyncEnumerable<TSource> Zip<TSource>(
			this IEnumerable<IAsyncEnumerable<TSource>> sources,
			[EnumeratorCancellation]CancellationToken cancellationToken = default)
		{
			var enumerators = sources.Select(x => x.GetAsyncEnumerator(cancellationToken)).ToArray();

			try
			{
				while (true)
				{
					foreach (var enumerator in enumerators)
					{
						if (cancellationToken.IsCancellationRequested)
							yield break;

						if (!await enumerator.MoveNextAsync(cancellationToken))
							continue;

						var current = enumerator.Current;
						yield return current;
					}
				}
			}
			finally
			{
				foreach (var enumerator in enumerators)
				{
					await enumerator.DisposeAsync();
				}
			}
		}
}
}
