using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer {
	public class JWTManager {
		public static string Generate (object _o, DateTime _exp) {
			var _builder = new JwtBuilder ().WithAlgorithm (new HMACSHA256Algorithm ()).WithSecret (m_secret);
			_builder = _builder.AddClaim ("exp", new DateTimeOffset (_exp).ToUnixTimeSeconds ());
			foreach (var (_key, _val) in _o.json ()) {
				if (_key == "exp")
					throw new Exception ("field name \"exp\" is invalid");
				_builder = _builder.AddClaim (_key, _val.to_str ());
			}
			return _builder.Build ();
		}

		public static JObject Check (string _api_key) {
			try {
				var _json = new JwtBuilder ().WithSecret (m_secret).MustVerifySignature ().Decode (_api_key);
				var _o = JObject.Parse (_json);
				_o.Remove ("exp");
				return _o;
			} catch (Exception) {
				throw new MyHttpException (401);
			}
		}

		// modify the secret to your private data
		public static string m_secret = "{337EB857-A793-4EEE-80FB-2D28DF18AD12}";
	}
}
