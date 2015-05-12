using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class RefHashSet<T> : HashSet<T>
	{
		public RefHashSet() : base()
		{
		}

		public RefHashSet(IEnumerable<T> enumerable) : base(enumerable)
		{ 
		}

		public override bool Equals(object obj)
		{
			HashSet<T> hashSet = obj as HashSet<T>;

			if (obj != null)
			{
				if (this.SetEquals(hashSet))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
	}
}
