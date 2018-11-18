using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOSMainframe.Utils
{
	public class FourArgsSuffix<TReturn, TParam, TParam2, TParam3, TParam4> : SuffixBase
		where TReturn : Structure where TParam : Structure where TParam2 : Structure where TParam3 : Structure where TParam4 : Structure
    {
		private readonly Del<TReturn, TParam, TParam2, TParam3, TParam4> del;

		public delegate TInnerReturn Del<out TInnerReturn, in TInnerParam, in TInnerParam2, in TInnerParam3, in TInnerParam4>(TInnerParam one, TInnerParam2 two, TInnerParam3 three, TInnerParam4 four);

		public FourArgsSuffix(Del<TReturn, TParam, TParam2, TParam3, TParam4> del, string description = "") : base(description)
        {
			this.del = del;
        }

		protected override object Call(object[] args)
		{
			return (TReturn)del((TParam)args[0], (TParam2)args[1], (TParam3)args[2], (TParam4)args[3]);
		}

		protected override Delegate Delegate
        {
            get
            {
                return del;
            }
        }
	}
}
