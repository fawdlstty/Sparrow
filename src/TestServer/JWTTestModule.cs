using Newtonsoft.Json.Linq;
using Sparrow.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestServer {
	[HTTPModule ("Test for JWT")]
	public class JWTTestModule {
		[HTTP.GET ("generate jwt token"), JWTGen]
		public static (object, DateTime) generate () {
			return (new { test = "hello" }, DateTime.Now.AddMinutes (2));
		}

		[JWTConnect]
		public static JWTTestModule _check_auth (JObject _jwt) {
			return new JWTTestModule { n = 100 };
		}

		[HTTP.GET ("get the 'n' value")]
		public int get_value () {
			return n;
		}

		private int n = 0;
	}
}
