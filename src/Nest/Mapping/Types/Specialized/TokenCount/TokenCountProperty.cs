﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nest
{
	[JsonObject(MemberSerialization.OptIn)]
	public interface ITokenCountProperty : INumberProperty
	{
		string Analyzer { get; set; }
	}

	public class TokenCountProperty : NumberProperty, ITokenCountProperty
	{
		public TokenCountProperty() : base("token_count") { }

		public string Analyzer { get; set; }
	}

	public class TokenCountPropertyDescriptor<T> 
		: NumberPropertyDescriptorBase<TokenCountPropertyDescriptor<T>, ITokenCountProperty, T>, ITokenCountProperty
		where T : class
	{
		public TokenCountPropertyDescriptor() : base("token_count") { }

		string ITokenCountProperty.Analyzer { get; set; }

		public TokenCountPropertyDescriptor<T> Analyzer(string analyzer) => Assign(a => a.Analyzer = analyzer);
	}
}
