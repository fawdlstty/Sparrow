using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SparrowServer.HttpProtocol {
	public class FawResponse {
		public FawResponse () {
			m_headers ["Cache-Control"] = "no-store";
			m_headers ["Content-Type"] = "text/plain; charset=utf-8";
			m_headers ["Server"] = "Sparrow/0.1";
			m_headers ["Access-Control-Allow-Origin"] = "*";
			m_headers ["Access-Control-Allow-Headers"] = "X-Requested-With,Content-Type,Accept";
			m_headers ["Access-Control-Allow-Methods"] = "HEAD,GET,POST,PUT,DELETE";
		}

		public void clear () { m_data.Clear (); }
		public void write (byte _data) { m_data.Add (_data); }
		public void write (byte [] _data) { m_data.AddRange (_data); }
		public void write (string _data) { m_data.AddRange (Encoding.UTF8.GetBytes (_data)); }
		public void write_line (string _data) { m_data.AddRange (Encoding.UTF8.GetBytes ($"{_data}\r\n")); }
		public void write_file (string _path) {
			m_data.AddRange (File.ReadAllBytes (_path));
			string _file = _path.mid (_path.last_index_of ('/', '\\'));
			set_content_from_filename (_file);
		}
		public void download_file (string _path) {
			m_data.AddRange (File.ReadAllBytes (_path));
			string _file = _path.mid (_path.last_index_of ('/', '\\'));
			set_content_from_filename (_file);
			m_headers ["Content-Disposition"] = $"attachment; filename=\"{_path.mid_last ("/")}\"";
		}
		public void redirect (string _path) {
			m_status_code = 302;
			m_headers ["location"] = _path;
		}
		public void set_content_from_filename (string _file) {
			string _content_type = _get_content_type (_file.Contains (".") ? _file.mid_last (".") : "");
			m_headers ["Content-Type"] = $"{_content_type}; charset=utf-8";
			m_headers ["Cache-Control"] = "private";
		}
		public byte [] _get_writes () { return m_data.ToArray (); }
		//public string _get_header (string _key) { return m_headers.ContainsKey (_key) ? m_headers [_key] : null; }
		public int m_status_code = 200;
		private List<byte> m_data = new List<byte> ();
		public Dictionary<string, string> m_headers = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);

		public byte [] build_response (FawRequest _req) {
			var _data = new List<byte> ();
			_data.AddRange ($"{_req.m_version} {m_status_code} {(m_codes.ContainsKey (m_status_code) ? m_codes [m_status_code] : "Unknown Error")}\r\n".to_bytes ());
			//
			string [] _encodings = (_req.m_headers.ContainsKey ("Accept-Encoding") ? _req.m_headers ["Accept-Encoding"].Split (',') : new string [0]);
			_encodings = (from p in _encodings select p.Trim ().ToLower ()).ToArray ();
			var _cnt_data = m_data;
			if (Array.IndexOf (_encodings, "gzip") >= 0) {
				m_headers ["Content-Encoding"] = "gzip";
				m_headers ["Vary"] = "Accept-Encoding";
				_cnt_data = m_data.gzip_compress ();
			} else if (Array.IndexOf (_encodings, "deflate") >= 0) {
				m_headers ["Content-Encoding"] = "deflate";
				m_headers ["Vary"] = "Accept-Encoding";
				_cnt_data = m_data.deflate_compress ();
			}
			m_headers ["Content-Length"] = _cnt_data.Count.to_str ();
			foreach (var (_key, _val) in m_headers)
				_data.AddRange ($"{_key}: {_val}\r\n".to_bytes ());
			_data.AddRange ("\r\n".to_bytes ());
			_data.AddRange (_cnt_data);
			return _data.ToArray ();
		}

		private static Dictionary<int, string> m_codes = new Dictionary<int, string> {
			[100] = "Continue",
			[101] = "Switching Protocols",
			[102] = "Processing",
			[200] = "OK",
			[201] = "Created",
			[202] = "Accepted",
			[203] = "Non-Authoritative Information",
			[204] = "No Content",
			[205] = "Reset Content",
			[206] = "Partial Content",
			[207] = "Multi-Status",
			[300] = "Multiple Choices",
			[301] = "Moved Permanently",
			[302] = "Move Temporarily",
			[303] = "See Other",
			[304] = "Not Modified",
			[305] = "Use Proxy",
			[306] = "Switch Proxy",
			[307] = "Temporary Redirect",
			[400] = "Bad Request",
			[401] = "Unauthorized",
			[402] = "Payment Required",
			[403] = "Forbidden",
			[404] = "Not Found",
			[405] = "Method Not Allowed",
			[406] = "Not Acceptable",
			[407] = "Proxy Authentication Required",
			[408] = "Request Timeout",
			[409] = "Conflict",
			[410] = "Gone",
			[411] = "Length Required",
			[412] = "Precondition Failed",
			[413] = "Request Entity Too Large",
			[414] = "Request-URI Too Long",
			[415] = "Unsupported Media Type",
			[416] = "Requested Range Not Satisfiable",
			[417] = "Expectation Failed",
			[418] = "I'm a teapot",
			[421] = "Too Many Connections",
			[422] = "Unprocessable Entity",
			[423] = "Locked",
			[424] = "Failed Dependency",
			[425] = "Too Early",
			[426] = "Upgrade Required",
			[449] = "Retry With",
			[451] = "Unavailable For Legal Reasons",
			[500] = "Internal Server Error",
			[501] = "Not Implemented",
			[502] = "Bad Gateway",
			[503] = "Service Unavailable",
			[504] = "Gateway Timeout",
			[505] = "HTTP Version Not Supported",
			[506] = "Variant Also Negotiates",
			[507] = "Insufficient Storage",
			[509] = "Bandwidth Limit Exceeded",
			[510] = "Not Extended",
			[600] = "Unparseable Response Headers",
		};

		private static Dictionary<string, string> s_plain = new Dictionary<string, string> () {
			["323"] = "text/h323",
			["acx"] = "application/internet-property-stream",
			["ai"] = "application/postscript",
			["aif"] = "audio/x-aiff",
			["aifc"] = "audio/x-aiff",
			["aiff"] = "audio/x-aiff",
			["asf"] = "video/x-ms-asf",
			["asr"] = "video/x-ms-asf",
			["asx"] = "video/x-ms-asf",
			["au"] = "audio/basic",
			["avi"] = "video/x-msvideo",
			["axs"] = "application/olescript",
			["bas"] = "text/plain",
			["bcpio"] = "application/x-bcpio",
			["bin"] = "application/octet-stream",
			["bmp"] = "image/bmp",
			["c"] = "text/plain",
			["cat"] = "application/vnd.ms-pkiseccat",
			["cdf"] = "application/x-cdf",
			["cer"] = "application/x-x509-ca-cert",
			["class"] = "application/octet-stream",
			["clp"] = "application/x-msclip",
			["cmx"] = "image/x-cmx",
			["cod"] = "image/cis-cod",
			["cpio"] = "application/x-cpio",
			["crd"] = "application/x-mscardfile",
			["crl"] = "application/pkix-crl",
			["crt"] = "application/x-x509-ca-cert",
			["csh"] = "application/x-csh",
			["css"] = "text/css",
			["dcr"] = "application/x-director",
			["der"] = "application/x-x509-ca-cert",
			["dir"] = "application/x-director",
			["dll"] = "application/x-msdownload",
			["dms"] = "application/octet-stream",
			["doc"] = "application/msword",
			["dot"] = "application/msword",
			["dvi"] = "application/x-dvi",
			["dxr"] = "application/x-director",
			["eps"] = "application/postscript",
			["etx"] = "text/x-setext",
			["evy"] = "application/envoy",
			["exe"] = "application/octet-stream",
			["fif"] = "application/fractals",
			["flr"] = "x-world/x-vrml",
			["gif"] = "image/gif",
			["gtar"] = "application/x-gtar",
			["gz"] = "application/x-gzip",
			["h"] = "text/plain",
			["hdf"] = "application/x-hdf",
			["hlp"] = "application/winhlp",
			["hqx"] = "application/mac-binhex40",
			["hta"] = "application/hta",
			["htc"] = "text/x-component",
			["htm"] = "text/html",
			["html"] = "text/html",
			["htt"] = "text/webviewhtml",
			["ico"] = "image/x-icon",
			["ief"] = "image/ief",
			["iii"] = "application/x-iphone",
			["ins"] = "application/x-internet-signup",
			["isp"] = "application/x-internet-signup",
			["jfif"] = "image/pipeg",
			["jpe"] = "image/jpeg",
			["jpeg"] = "image/jpeg",
			["jpg"] = "image/jpeg",
			["js"] = "application/x-javascript",
			["latex"] = "application/x-latex",
			["lha"] = "application/octet-stream",
			["lsf"] = "video/x-la-asf",
			["lsx"] = "video/x-la-asf",
			["lzh"] = "application/octet-stream",
			["m13"] = "application/x-msmediaview",
			["m14"] = "application/x-msmediaview",
			["m3u"] = "audio/x-mpegurl",
			["man"] = "application/x-troff-man",
			["mdb"] = "application/x-msaccess",
			["me"] = "application/x-troff-me",
			["mht"] = "message/rfc822",
			["mhtml"] = "message/rfc822",
			["mid"] = "audio/mid",
			["mny"] = "application/x-msmoney",
			["mov"] = "video/quicktime",
			["movie"] = "video/x-sgi-movie",
			["mp2"] = "video/mpeg",
			["mp3"] = "audio/mpeg",
			["mpa"] = "video/mpeg",
			["mpe"] = "video/mpeg",
			["mpeg"] = "video/mpeg",
			["mpg"] = "video/mpeg",
			["mpp"] = "application/vnd.ms-project",
			["mpv2"] = "video/mpeg",
			["ms"] = "application/x-troff-ms",
			["mvb"] = "application/x-msmediaview",
			["nws"] = "message/rfc822",
			["oda"] = "application/oda",
			["p10"] = "application/pkcs10",
			["p12"] = "application/x-pkcs12",
			["p7b"] = "application/x-pkcs7-certificates",
			["p7c"] = "application/x-pkcs7-mime",
			["p7m"] = "application/x-pkcs7-mime",
			["p7r"] = "application/x-pkcs7-certreqresp",
			["p7s"] = "application/x-pkcs7-signature",
			["pbm"] = "image/x-portable-bitmap",
			["pdf"] = "application/pdf",
			["pfx"] = "application/x-pkcs12",
			["pgm"] = "image/x-portable-graymap",
			["pko"] = "application/ynd.ms-pkipko",
			["pma"] = "application/x-perfmon",
			["pmc"] = "application/x-perfmon",
			["pml"] = "application/x-perfmon",
			["pmr"] = "application/x-perfmon",
			["pmw"] = "application/x-perfmon",
			["pnm"] = "image/x-portable-anymap",
			["pot,"] = "application/vnd.ms-powerpoint",
			["ppm"] = "image/x-portable-pixmap",
			["pps"] = "application/vnd.ms-powerpoint",
			["ppt"] = "application/vnd.ms-powerpoint",
			["prf"] = "application/pics-rules",
			["ps"] = "application/postscript",
			["pub"] = "application/x-mspublisher",
			["qt"] = "video/quicktime",
			["ra"] = "audio/x-pn-realaudio",
			["ram"] = "audio/x-pn-realaudio",
			["ras"] = "image/x-cmu-raster",
			["rgb"] = "image/x-rgb",
			["rmi"] = "audio/mid",
			["roff"] = "application/x-troff",
			["rtf"] = "application/rtf",
			["rtx"] = "text/richtext",
			["scd"] = "application/x-msschedule",
			["sct"] = "text/scriptlet",
			["setpay"] = "application/set-payment-initiation",
			["setreg"] = "application/set-registration-initiation",
			["sh"] = "application/x-sh",
			["shar"] = "application/x-shar",
			["sit"] = "application/x-stuffit",
			["snd"] = "audio/basic",
			["spc"] = "application/x-pkcs7-certificates",
			["spl"] = "application/futuresplash",
			["src"] = "application/x-wais-source",
			["sst"] = "application/vnd.ms-pkicertstore",
			["stl"] = "application/vnd.ms-pkistl",
			["stm"] = "text/html",
			["svg"] = "image/svg+xml",
			["sv4cpio"] = "application/x-sv4cpio",
			["sv4crc"] = "application/x-sv4crc",
			["swf"] = "application/x-shockwave-flash",
			["t"] = "application/x-troff",
			["tar"] = "application/x-tar",
			["tcl"] = "application/x-tcl",
			["tex"] = "application/x-tex",
			["texi"] = "application/x-texinfo",
			["texinfo"] = "application/x-texinfo",
			["tgz"] = "application/x-compressed",
			["tif"] = "image/tiff",
			["tiff"] = "image/tiff",
			["tr"] = "application/x-troff",
			["trm"] = "application/x-msterminal",
			["tsv"] = "text/tab-separated-values",
			["txt"] = "text/plain",
			["uls"] = "text/iuls",
			["ustar"] = "application/x-ustar",
			["vcf"] = "text/x-vcard",
			["vrml"] = "x-world/x-vrml",
			["wav"] = "audio/x-wav",
			["wcm"] = "application/vnd.ms-works",
			["wdb"] = "application/vnd.ms-works",
			["wks"] = "application/vnd.ms-works",
			["wmf"] = "application/x-msmetafile",
			["wps"] = "application/vnd.ms-works",
			["wri"] = "application/x-mswrite",
			["wrl"] = "x-world/x-vrml",
			["wrz"] = "x-world/x-vrml",
			["xaf"] = "x-world/x-vrml",
			["xbm"] = "image/x-xbitmap",
			["xla"] = "application/vnd.ms-excel",
			["xlc"] = "application/vnd.ms-excel",
			["xlm"] = "application/vnd.ms-excel",
			["xls"] = "application/vnd.ms-excel",
			["xlt"] = "application/vnd.ms-excel",
			["xlw"] = "application/vnd.ms-excel",
			["xof"] = "x-world/x-vrml",
			["xpm"] = "image/x-xpixmap",
			["xwd"] = "image/x-xwindowdump",
			["z"] = "application/x-compress",
			["zip"] = "application/zip"
		};
		public static string _get_content_type (string _ext) {
			return (s_plain.ContainsKey (_ext) ? s_plain [_ext] : "application/octet-stream");
		}
	}
}
