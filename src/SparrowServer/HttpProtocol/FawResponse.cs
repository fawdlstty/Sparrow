using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SparrowServer.HttpProtocol {
	public class FawResponse {
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
			add_header ("Content-Disposition", $"attachment; filename=\"{_path.mid_last ("/")}\"");
		}
		public void add_header (string _key, string _value) {
			if (!m_headers.ContainsKey (_key))
				m_headers.Add (_key, _value);
		}
		public void redirect (string _path) {
			m_status_code = 302;
			add_header ("location", _path);
		}
		public void set_content_from_filename (string _file) {
			string _content_type = FawHttpServer._get_content_type (_file.Contains (".") ? _file.mid_last (".") : "");
			add_header ("Content-Type", $"{_content_type}; charset=utf-8");
		}
		public byte [] _get_writes () { return m_data.ToArray (); }
		//public string _get_header (string _key) { return m_headers.ContainsKey (_key) ? m_headers [_key] : null; }
		public int m_status_code = 200;
		private List<byte> m_data = new List<byte> ();
		public Dictionary<string, string> m_headers = new Dictionary<string, string> ();
	}
}
