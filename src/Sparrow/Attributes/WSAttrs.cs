using System;
using System.Collections.Generic;
using System.Text;

namespace Sparrow.Attributes {
	// Module
	public class WSModuleAttribute : Attribute {
		public WSModuleAttribute (string description) { m_description = description; }
		public string m_description { get; private set; } = "";
	}

	// Method
	public class WSMethodAttribute : Attribute {
		public WSMethodAttribute (string summary = "") { Summary = summary; }
		public string Summary { get; private set; } = "";
	}
}
