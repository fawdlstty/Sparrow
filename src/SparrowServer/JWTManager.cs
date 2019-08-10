using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer {
	class JWTManager {
		public static string Generate (JObject _o, DateTime _exp) {
			//IJwtAlgorithm algorithm = new HMACSHA256Algorithm ();
			//IJsonSerializer serializer = new JsonNetSerializer ();
			//IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder ();
			//IJwtEncoder encoder = new JwtEncoder (algorithm, serializer, urlEncoder);
			//return encoder.Encode (_o, m_secret);
			var _builder = new JwtBuilder ().WithAlgorithm (new HMACSHA256Algorithm ()).WithSecret (m_secret);
			_builder = _builder.AddClaim ("exp", new DateTimeOffset (_exp).ToUnixTimeSeconds ());
			foreach (var (_key, _val) in _o) {
				if (_key == "exp")
					throw new Exception ("field name \"exp\" is invalid");
				_builder = _builder.AddClaim (_key, _val.to_str ());
			}
			return _builder.Build ();
		}

		public static JObject Check (string _token) {
			try {
				var _json = new JwtBuilder ().WithSecret (m_secret).MustVerifySignature ().Decode (_token);
				Console.WriteLine (_json);
				var _o = JObject.Parse (_json);
				_o.Remove ("exp");
				return _o;
			} catch (TokenExpiredException ex) {
				throw new Exception ($"JWT Error: {ex.Message}");
			} catch (SignatureVerificationException ex) {
				throw new Exception ($"JWT Error: {ex.Message}");
			}
		}

		// modify the secret to your private data
		private static string m_secret = "{337EB857-A793-4EEE-80FB-2D28DF18AD12}";
	}
}
