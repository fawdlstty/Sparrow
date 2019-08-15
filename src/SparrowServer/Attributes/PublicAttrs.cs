﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Attributes {
	// JWT
	public interface IJWTMethod { string Type { get; } }
	public class JWTGenAttribute: Attribute, IJWTMethod { public string Type { get { return "Gen"; } } }
	public class JWTRequestAttribute : Attribute, IJWTMethod { public string Type { get { return "Request"; } } }
}