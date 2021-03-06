﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sparrow.Attributes {
	// JWT
	public interface IJWTMethod { string Type { get; } }
	public class JWTGenAttribute: Attribute, IJWTMethod { public string Type { get { return "Gen"; } } }
	public class JWTConnectAttribute : Attribute, IJWTMethod { public string Type { get { return "Connect"; } } }
	public class PureConnectAttribute : Attribute, IJWTMethod { public string Type { get { return "PureConnect"; } } }
}
