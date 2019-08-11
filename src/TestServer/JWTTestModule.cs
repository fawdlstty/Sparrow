using Newtonsoft.Json.Linq;
using SparrowServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestServer {
	[HTTPModule ("Test for JWT")]
	public class JWTTestModule {
		[HTTP.GET ("generate jwt token"), JWTGen]
		public static (JObject, DateTime) generate () {
			return (new JObject { ["test"] = "hello" }, DateTime.Now.AddMinutes (2));
		}

		[JWTReconnect]
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
