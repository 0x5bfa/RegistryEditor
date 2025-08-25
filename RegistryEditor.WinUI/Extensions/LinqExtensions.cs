using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistryEditor.WinUI.Extensions
{
	public static class LinqExtensions
	{
		public static TOut? Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut? defaultValue = default)
		{
			if (dictionary is null || key is null)
				return defaultValue;

			if (!dictionary.ContainsKey(key))
			{
				if (defaultValue is TValue value)
					dictionary.Add(key, value);

				return defaultValue;
			}

			if (dictionary[key] is TOut o)
				return o;

			return defaultValue;
		}

		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> getChildren)
		{
			var stack = new Stack<T>();
			foreach (var item in items)
				stack.Push(item);

			while (stack.Count > 0)
			{
				var current = stack.Pop();
				yield return current;

				var children = getChildren(current);
				if (children == null) continue;

				foreach (var child in children)
					stack.Push(child);
			}
		}
	}
}