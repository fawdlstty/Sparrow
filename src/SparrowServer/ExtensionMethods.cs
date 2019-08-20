using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SparrowServer {
	public static class ExtensionMethods {
		// 字符串是否为空
		public static bool is_null (this string s) { return string.IsNullOrEmpty (s); }

		// 字符串是否为不可见
		public static bool is_null_or_space (this string s) { return string.IsNullOrWhiteSpace (s); }

		// url 编码
		public static string url_encode (this string s) { return HttpUtility.UrlEncode (s); }

		// url 解码
		public static string url_decode (this string s) { return HttpUtility.UrlDecode (s); }

		// xor 0xff
		public static byte [] xor_0xff (this byte [] b) { for (int i = 0; i < (b?.Length ?? 0); ++i) b [i] ^= 0xff; return b; }

		// 获取 md5
		public static string md5 (this byte [] s) { using (var _md5 = MD5.Create ()) return BitConverter.ToString (_md5.ComputeHash (s)).Replace ("-", ""); }

		// 对象转字符串
		public static string to_str (this object o) {
            if (o == null) {
                return "";
            } else if (o is JObject jo) {
                return jo.ToString (Newtonsoft.Json.Formatting.None);
            } else if (o is JArray ja) {
                return ja.ToString (Newtonsoft.Json.Formatting.None);
            } else if (o is DateTime dt) {
                return dt.ToString ("yyyy-MM-dd HH:mm:ss");
            } else if (o is byte [] b) {
                return Encoding.UTF8.GetString (b);
            } else if (o is List<byte> l) {
                return Encoding.UTF8.GetString (l.ToArray ());
            } else if (o is float f) {
				return (f + 0.000001).ToString ("0.00");
            } else if (o is double d) {
				return (d + 0.000001).ToString ("0.00");
            } else if (o is string s) {
				return s;
            } else {
				return o.ToString ();
			}
        }

		// 字符串转 utf8 字节数组
		public static byte [] to_bytes (this string s) { return Encoding.UTF8.GetBytes (s ?? ""); }

		// 字符串转时间类型
		public static DateTime to_datetime (this string s) {
			if (s.Length == 6) {
				return DateTime.ParseExact (s, "HHmmss", System.Globalization.CultureInfo.CurrentCulture);
			} else if (s.Length == 8) {
				return DateTime.ParseExact (s, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
			} else if (s.Length == 15) {
				return DateTime.ParseExact (s, "yyyyMMdd HHmmss", System.Globalization.CultureInfo.CurrentCulture);
			} else {
				return DateTime.Parse (s);
			}
		}

		// 时间类型转字符串
		public static string to_date (this DateTime dt) {
			return dt.ToString ("yyyyMMdd");
		}
		public static string to_time (this DateTime dt) {
			return dt.ToString ("HHmmss");
		}
		public static string to_datetime (this DateTime dt) {
			return dt.ToString ("yyyyMMdd HHmmss");
		}

		// 对象转长整型
		public static long to_long (this object o) {
			try {
				if (o is double d) {
					return (long) (d + 0.50000001);
				} else if (o is float f) {
					return (long) (f + 0.50000001);
				} else if (o is string s) {
					if (s.right_is_nocase ("b"))
						s = s.left (s.Length - 1);
					switch (s.right (1).ToLower ()) {
						case "%":
							return (s.left (s.Length - 1).to_double () / 100).to_long ();
						case "k":
							return (s.left (s.Length - 1).to_double () * 1024).to_long ();
						case "m":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024).to_long ();
						case "g":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024).to_long ();
						case "t":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024).to_long ();
						case "p":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024).to_long ();
						case "e":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024 * 1024).to_long ();
						case "b":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024).to_long ();
					}
				}
				return Convert.ToInt64 (o);
			} catch (Exception) {
				return 0;
			}
		}

		// 对象转整型
		public static int to_int (this object o) {
			try {
				if (o is double d) {
					return (int) (d + 0.50000001);
				} else if (o is float f) {
					return (int) (f + 0.50000001);
				} else if (o is string s) {
					if (s.right_is_nocase ("b"))
						s = s.left (s.Length - 1);
					switch (s.right (1)) {
						case "%":
							return (s.left (s.Length - 1).to_double () / 100).to_int ();
						case "k":
							return (s.left (s.Length - 1).to_double () * 1024).to_int ();
						case "m":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024).to_int ();
						case "g":
							return (s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024).to_int ();
					}
				}
				return Convert.ToInt32 (o);
			} catch (Exception) {
				return 0;
			}
		}

		// 对象转短整型
		public static short to_short (this object o) {
			try {
				if (o is string s) {
					if (s.right (1) == "%")
						return (short) (s.left (s.Length - 1).to_short () / 100);
				}
				return Convert.ToInt16 (o);
			} catch (Exception) {
				return 0;
			}
		}

		// 对象转双精度浮点型
		public static double to_double (this object o) {
			try {
				if (o is string s) {
					if (s.right_is_nocase ("b"))
						s = s.left (s.Length - 1);
					switch (s.right (1).ToLower ()) {
						case "%":
							return s.left (s.Length - 1).to_double () / 100;
						case "k":
							return s.left (s.Length - 1).to_double () * 1024;
						case "m":
							return s.left (s.Length - 1).to_double () * 1024 * 1024;
						case "g":
							return s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024;
						case "t":
							return s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024;
						case "p":
							return s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024;
						case "e":
							return s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024 * 1024;
						case "b":
							return s.left (s.Length - 1).to_double () * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024;
					}
				}
				return Convert.ToDouble (o);
			} catch (Exception) {
				return 0.0;
			}
		}

		// 对象转布尔型
		public static bool to_bool (this object o) {
			try {
				if (o is string s) {
					s = s.ToLower ();
					if (s == "on" || s == "true" || s == "yes" || s == "1" || s == "active" || s == "accept" || s == "启用")
						return true;
					else if (s == "off" || s == "false" || s == "no" || s == "0" || s == "inactive" || s == "refuse" || s == "禁用")
						return false;
				}
				return Convert.ToBoolean (o);
			} catch (Exception) {
				return false;
			}
		}

		// 对象转json
		public static string to_json (this object o) { if (o == null) return ""; return JToken.FromObject (o).to_str (); }

		// json数组转数组
		public static long [] json_array_int64 (this string s) { return (from _item in JArray.FromObject (s) select _item.to_long ()).ToArray (); }

		//// json数组转二维数组
		//public static long [] [] json_array2_int64 (this string s) { return (from _items in JArray.FromObject (s) select (from _item in _items select _item.to_long ()).ToArray ()).ToArray (); }

		// 对象转json对象
		public static JObject json (this object o) { if (o == null) return null; return JObject.FromObject (o); }

		// 对象转json对象
		public static JArray json (this object [] o) { if (o == null) return null; return JArray.FromObject (o); }

		// 反序列化
		public static T from_json<T> (this string s) { return JsonConvert.DeserializeObject<T> (s); }

		// 判断字节数组是否相同
		public static bool equ (this byte [] b1, byte [] b2) {
			if (b1 == null && b2 == null)
				return true;
			else if (b1 == null || b2 == null)
				return false;
			else if (b1.Length != b2.Length)
				return false;
			for (int i = 0; i < b1.Length; ++i) {
				if (b1 [i] != b2 [i])
					return false;
			}
			return true;
		}

		// 获取字符串左侧多少个字符
		public static string left (this string s, int length) {
			if (length < 1 || s.is_null ())
				return "";
			else if (s.Length <= length)
				return s;
			else
				return s.Substring (0, length);
		}

		// 判断字符串是否以什么开始
		public static bool left_is (this string s, string s2) {
			if (s.is_null ())
				return s2.is_null ();
			if (s.Length < s2.Length)
				return false;
			return s.left (s2.Length) == s2;
		}

		// 判断字符串是否以什么开始
		public static bool left_is_nocase (this string s, string s2) {
			if (s.is_null ())
				return s2.is_null ();
			if (s.Length < s2.Length)
				return false;
			return s.left (s2.Length).ToLower () == s2.ToLower ();
		}

		// 获取字符串右侧多少个字符
		public static string right (this string s, int length) {
			if (length < 1 || s.is_null ())
				return "";
			else if (s.Length <= length)
				return s;
			else
				return s.Substring (s.Length - length);
		}

		// 判断字符串是否以什么结束
		public static bool right_is (this string s, string s2) {
			if (s.is_null ())
				return s2.is_null ();
			if (s.Length < s2.Length)
				return false;
			return s.right (s2.Length) == s2;
		}

		// 判断字符串是否以什么结束
		public static bool right_is_nocase (this string s, string s2) {
			if (s.is_null ())
				return s2.is_null ();
			if (s.Length < s2.Length)
				return false;
			return s.right (s2.Length).ToLower () == s2.ToLower ();
		}

		// 截取中部字符串
		public static string mid (this string s, int start, int end = -1) {
			if ((end != -1 && end <= start) || start >= (s?.Length ?? 0))
				return "";
			else if (end == -1)
				return s.Substring (start);
			else
				return s.Substring (start, end - start);
		}

		// 截取中部字符串
		public static string mid (this string s, string begin, string end = "") {
			if (s.is_null () || begin.is_null ())
				return "";
			int p = s.IndexOf (begin);
			if (p == -1)
				return "";
			s = s.mid (p + begin.Length);
			if (!end.is_null ()) {
				p = s.IndexOf (end);
				if (p >= 0)
					s = s.left (p);
			}
			return s;
		}

		// 截取中部字符串
		public static string mid_last (this string s, string begin, string end = "") {
			if (s.is_null () || begin.is_null ())
				return "";
			int p = s.LastIndexOf (begin);
			if (p == -1)
				return "";
			s = s.mid (p + begin.Length);
			if (!end.is_null ()) {
				p = s.IndexOf (end);
				if (p >= 0)
					s = s.left (p);
			}
			return s;
		}

		// pythonic 选择范围
		public static string r (this string s, int start) {
			if (start >= (s?.Length ?? 0))
				return "";
			else
				return s.Substring (start);
		}

		// pythonic 选择范围
		public static string r (this string s, int start, int end) {
			if (start >= (s?.Length ?? 0))
				return "";
			if (end >= s.Length)
				end = s.Length - 1;
			else if (end < 0)
				end = s.Length + end;
			return s.Substring (start, end - start);
		}

		// pythonic 选择范围
		public static T [] r<T> (this T [] a, int start) {
			if (start >= (a?.Length ?? 0))
				return new T [0];
			T [] _a = new T [a.Length - start];
			for (int i = 0; i < _a.Length; ++i)
				_a [i] = a [i + start];
			return _a;
		}

		// pythonic 选择范围
		public static T [] r<T> (this T [] a, int start, int end) {
			if (start >= (a?.Length ?? 0))
				return new T [0];
			if (end >= a.Length)
				end = a.Length - 1;
			else if (end < 0)
				end = a.Length + end;
			T [] _a = new T [a.Length - start];
			for (int i = 0; i < _a.Length; ++i)
				_a [i] = a [i + start];
			return _a;
		}

		// gzip压缩数据
		public static byte [] gzip_compress (this byte [] data) {
			using (MemoryStream ms = new MemoryStream ()) {
				using (GZipStream gzip = new GZipStream (ms, CompressionMode.Compress))
					gzip.Write (data, 0, data.Length);
				return ms.ToArray ();
			}
		}
		public static List<byte> gzip_compress (this List<byte> data) {
			return data.ToArray ().gzip_compress ().ToList ();
		}

		// gzip解压数据
		public static byte [] gzip_decompress (this byte [] data, int _max_len = -1) {
			using (MemoryStream ms = new MemoryStream (data)) {
				using (GZipStream gzip = new GZipStream (ms, CompressionMode.Decompress)) {
					using (MemoryStream ms2 = new MemoryStream ()) {
						data = new byte [1024];
						int nRead;
						while ((nRead = gzip.Read (data, 0, data.Length)) > 0) {
							if (_max_len >= 0) {
								if (_max_len < 1024)
									throw new Exception ("Decompress out of range");
								_max_len -= 1024;
							}
							ms2.Write (data, 0, nRead);
						}
						return ms2.ToArray ();
					}
				}
			}
		}
		public static List<byte> gzip_decompress (this List<byte> data, int _max_len = -1) {
			//return data.ToArray ().gzip_decompress (_max_len).ToList ();
			using (MemoryStream ms = new MemoryStream (data.ToArray ())) {
				using (GZipStream gzip = new GZipStream (ms, CompressionMode.Decompress)) {
					data = new List<byte> ();
					var _data_block = new byte [1024];
					int nRead;
					while ((nRead = gzip.Read (_data_block, 0, _data_block.Length)) > 0) {
						if (_max_len >= 0) {
							if (_max_len < 1024)
								throw new MyHttpException (413);
							_max_len -= 1024;
						}
						data.AddRange (_data_block);
					}
					return data;
				}
			}
		}

		// deflate压缩数据
		public static byte [] deflate_compress (this byte [] data) {
			using (MemoryStream ms = new MemoryStream ()) {
				using (DeflateStream deflate = new DeflateStream (ms, CompressionMode.Compress))
					deflate.Write (data, 0, data.Length);
				return ms.ToArray ();
			}
		}
		public static List<byte> deflate_compress (this List<byte> data) {
			using (MemoryStream ms = new MemoryStream (data.ToArray ())) {
				using (DeflateStream gzip = new DeflateStream (ms, CompressionMode.Decompress)) {
					var _list = new List<byte> ();
					var _data_block = new byte [1024];
					int nRead;
					while ((nRead = gzip.Read (_data_block, 0, _data_block.Length)) > 0) {
						_list.AddRange (_data_block);
					}
					return _list;
				}
			}
		}

		// gzip解压数据
		public static byte [] deflate_decompress (this byte [] data, int _max_len = -1) {
			using (MemoryStream ms = new MemoryStream (data)) {
				using (DeflateStream deflate = new DeflateStream (ms, CompressionMode.Decompress)) {
					using (MemoryStream ms2 = new MemoryStream ()) {
						data = new byte [1024];
						int nRead;
						while ((nRead = deflate.Read (data, 0, data.Length)) > 0) {
							if (_max_len >= 0) {
								if (_max_len < 1024)
									throw new MyHttpException (413);
								_max_len -= 1024;
							}
							ms2.Write (data, 0, nRead);
						}
						return ms2.ToArray ();
					}
				}
			}
		}
		public static List<byte> deflate_decompress (this List<byte> data, int _max_len = -1) {
			//return data.ToArray ().deflate_decompress (_max_len).ToList ();
			using (MemoryStream ms = new MemoryStream (data.ToArray ())) {
				using (DeflateStream gzip = new DeflateStream (ms, CompressionMode.Decompress)) {
					data = new List<byte> ();
					var _data_block = new byte [1024];
					int nRead;
					while ((nRead = gzip.Read (_data_block, 0, _data_block.Length)) > 0) {
						if (_max_len >= 0) {
							if (_max_len < 1024)
								throw new Exception ("Decompress out of range");
							_max_len -= 1024;
						}
						data.AddRange (_data_block);
					}
					return data;
				}
			}
		}

		// 切割字符串
		public static string [] split (this string s, bool remove_empty, params char [] sp) {
			return s?.Split (sp, (remove_empty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None)) ?? new string [0];
		}
		public static List<string> split_list (this string s, bool remove_empty, params char [] sp) {
			return new List<string> (s.split (remove_empty, sp));
		}
		public static (string, string) split2 (this string s, params char [] sp) {
			int _p = s.index_of (sp);
			if (_p == -1)
				return (s, "");
			return (s.left (_p), s.mid (_p + 1));
		}

		// 切割字符串并转新类型
		public static T [] split<T> (this string s, bool remove_empty, params char [] sp) {
			string [] tmp = split (s, remove_empty, sp);
			return (from p in tmp select (T) Convert.ChangeType (p, typeof (T))).ToArray ();
		}
		public static List<T> split_list<T> (this string s, bool remove_empty, params char [] sp) {
			return new List<T> (s.split<T> (remove_empty, sp));
		}

		// 合并字符串
		public static string join (this IEnumerable<object> arr, string _sp) {
			StringBuilder sb = new StringBuilder ();
			bool _first = true;
			foreach (object _item in arr) {
				if (!_first)
					sb.Append (_sp);
				_first = false;
				sb.Append (_item.to_str ());
			}
			return sb.to_str ();
		}

		// base64 转码
		public static string base64_encode (this string s) { return s.to_bytes ().base64_encode (); }

		// base64 转码
		public static string base64_encode (this byte [] b) { return Convert.ToBase64String (b); }

		// base64 解码
		public static string base64_decode (this string s) { return s.base64_decode_arr ().to_str (); }

		// base64 解码 arr
		public static byte [] base64_decode_arr (this string s) { return Convert.FromBase64String (s); }

		// sha1 转码
		public static byte [] sha1_encode (this byte [] b) {
			using (SHA1 _sha1 = new SHA1CryptoServiceProvider ()) {
				return _sha1.ComputeHash (b);
			}
		}

		// 显示对象数组
		public static string join (this long [] o, string rep = ",") {
			if (o == null || o.Length == 0)
				return "";
			StringBuilder sb = new StringBuilder ();
			foreach (object item in o) {
				sb.Append (item.to_str ()).Append (",");
			}
			sb.Remove (sb.Length - 1, 1);
			return sb.ToString ();
		}

		// 字典转json
		public static string to_json (this Dictionary<string, string> dic) {
			JObject o = new JObject ();
			foreach (var (key, value) in dic)
				o [key] = value;
			return o.to_str ();
		}

		// aes加密，秘钥长度=128、192、256
		public static byte [] aes_encode (this byte [] b, byte [] key) {
			try {
				using (var ms = new MemoryStream ()) {
					using (var aes = Rijndael.Create ()) {
						aes.Mode = CipherMode.ECB;
						aes.Padding = PaddingMode.PKCS7;
						aes.KeySize = key.Length * 8;
						aes.Key = key;
						using (var cs = new CryptoStream (ms, aes.CreateEncryptor (), CryptoStreamMode.Write)) {
							cs.Write (b, 0, b.Length);
							return ms.ToArray ();
						}
					}
				}
			} catch (Exception) {
				return null;
			}
		}

        // aes解密，秘钥长度=128、192、256
        public static byte [] aes_decode (this byte [] b, byte [] key) {
			try {
				using (var ms = new MemoryStream (b)) {
					using (var aes = Rijndael.Create ()) {
						aes.Mode = CipherMode.ECB;
						aes.Padding = PaddingMode.PKCS7;
						aes.KeySize = key.Length * 8;
						aes.Key = key;
						using (var cs = new CryptoStream (ms, aes.CreateDecryptor (), CryptoStreamMode.Read)) {
							//cs.write (b);
							//return ms.ToArray ();
							using (MemoryStream ms2 = new MemoryStream ()) {
								byte [] data = new byte [1024];
								int nRead;
								while ((nRead = cs.Read (data, 0, data.Length)) > 0)
									ms2.Write (data, 0, nRead);
								return ms2.ToArray ();
							}
						}
					}
				}
			} catch (Exception) {
				return null;
			}
		}

		public static int index_of (this string _s, params char [] _chs) {
			if (_chs.Length == 1) {
				return _s.IndexOf (_chs [0]);
			} else if (_chs.Length > 1) {
				return _s.IndexOfAny (_chs);
			} else {
				return -1;
			}
		}

		public static int index_of (this string _s, int _begin, params char [] _chs) {
			if (_chs.Length == 1) {
				return _s.IndexOf (_chs [0], _begin);
			} else if (_chs.Length > 1) {
				return _s.IndexOfAny (_chs, _begin);
			} else {
				return -1;
			}
		}

		public static int last_index_of (this string _s, params char [] _chs) {
			if (_chs.Length == 1) {
				return _s.LastIndexOf (_chs [0]);
			} else if (_chs.Length > 1) {
				return _s.LastIndexOfAny (_chs);
			} else {
				return -1;
			}
		}

		public static int last_index_of (this string _s, int _begin, params char [] _chs) {
			if (_chs.Length == 1) {
				return _s.LastIndexOf (_chs [0], _begin);
			} else if (_chs.Length > 1) {
				return _s.LastIndexOfAny (_chs, _begin);
			} else {
				return -1;
			}
		}

		// .Net Core 获取映射文件名
		public static string map_of_path (this string _path) {
			string _src = System.Reflection.Assembly.GetEntryAssembly ().Location;
			char _sch = _src.First ((_ch) => { return _ch == '/' || _ch == '\\'; });
			return $"{ _src.left (_src.LastIndexOf (_sch) + 1) }{ _path.Replace (_sch == '/' ? '\\' : '/', _sch) }";
		}

		// 字典容器移除所有匹配项
		public static void remove_if<A,B> (this Dictionary<A,B> _d, Func<B,bool> _cond) {
			lock (_d) {
				var _va = (from p in _d where _cond (p.Value) select p.Key).ToList ();
				while (_va.Count > 0) {
					_d.Remove (_va [0]);
					_va.RemoveAt (0);
				}
			}
			
		}

		// 字典容器移除所有匹配项
		public static void remove_if<A,B> (this Dictionary<A,B> _d, Func<A,B,bool> _cond) {
			lock (_d) {
				var _va = (from p in _d where _cond (p.Key, p.Value) select p.Key).ToList ();
				while (_va.Count > 0) {
					_d.Remove (_va [0]);
					_va.RemoveAt (0);
				}
			}
			
		}

		// 格式化计量大小
		public static string format_size (this object _o) {
			double _d = _o.to_double ();
			string [] _limits = new string [] { "Byte", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB" };
			for (int i = 0; i < _limits.Length; ++i) {
				if (_d < 1024 || i == _limits.Length - 1) {
					return $"{_d.ToString ("0.00")} {_limits [i]}";
				} else {
					_d /= 1024;
				}
			}
			return _o.to_double ().ToString ("0.00");
		}

		// 生成token
		private static string _token_hex = "0123456789abcdef";
		private static string _token_base = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/";
		public static string make_token (this string _s, long _n) {
			if (_s.Length != 32)
				throw new Exception ("token格式错误");
			_s = _s.ToLower ();
			string _ret = "";
			for (int i = _s.Length - 1; i >= 0; --i) {
				int x = _token_hex.IndexOf (_s[i]);
				if (x == -1)
					throw new Exception ("token格式错误");
				x += (int) ((_n & 0x3) * 0x10);
				_n >>= 2;
				_ret = $"{_token_base [x]}{_ret}";
			}
			return _ret;
		}
		public static (string, long) make_token (this string _s) {
			if (_s.Length != 32)
				throw new Exception ("token格式错误");
			string _ret = "";
			long _n = 0;
			for (int i = 0; i < _s.Length; ++i) {
				int x = _token_base.IndexOf (_s[i]);
				if (x == -1)
					throw new Exception ("token格式错误");
				_ret += _token_hex [x & 0xf];
				_n = (_n << 2) + (x >> 4);
			}
			return (_ret, _n);
		}

		// 保存图片
		public static void save (this Image _img, string _path) {
			var _dic = new Dictionary<string, ImageFormat> {
				[".jpg"] = ImageFormat.Jpeg, [".png"] = ImageFormat.Png, [".gif"] = ImageFormat.Gif, [".tif"] = ImageFormat.Tiff,
				[".bmp"] = ImageFormat.Bmp, [".emf"] = ImageFormat.Emf, [".ico"] = ImageFormat.Icon, [".wmf"] = ImageFormat.Wmf
			};
			string _ext = _path.right (4).ToLower ();
			if (!_dic.ContainsKey (_ext))
				throw new Exception ("不支持的图片格式");
			_img.Save (_path, _dic [_ext]);
		}
		public static byte [] save (this Image _img, ImageFormat _fmt) {
			using (var _stream = new MemoryStream ()) {
				_img.Save (_stream, _fmt);
				return _stream.ToArray ();
			}
		}

		// 简化网站路径（去除.、..这种路径）
		public static string simplify_path (this string _path) {
			// 处理参数
			int _sp = _path.index_of ('?', '#');
			// 拆分
			var _items = (from p in (_sp == -1 ? _path : _path.left (_sp)).split (false, '/', '\\') select (p == "" ? "." : p)).ToList ();
			if (_items [0] == ".")
				_items.RemoveAt (0);
			// 去除当前路径
			for (int i = 0; i < _items.Count; ++i) {
				if (_items [i] == ".") {
					_items.RemoveAt (i);
					--i;
				}
			}
			// 处理相对路径
			for (int i = 0; i < _items.Count; ++i) {
				if (_items [i] == "..") {
					_items.RemoveAt (i--);
					if (i >= 0)
						_items.RemoveAt (i--);
				}
			}
			// 拼接
			_items.Insert (0, "");
			if (_items.Count == 1)
				_items.Insert (0, "");
			return $"{_items.join ("/")}{(_sp == -1 ? "" : _path.mid (_sp))}";
		}
	}
}
