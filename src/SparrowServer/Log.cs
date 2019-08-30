using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SparrowServer {
	public class Log {
		// 打印信息
		public static void show (string str) {
			//Console.WriteLine (str);
			if (m_path.is_null ()) {
				Console.WriteLine (str);
			} else {
				byte [] bytes = Encoding.UTF8.GetBytes (str + '\n');
				lock (m_locker) {
					using (FileStream fs = File.Open ($"{m_path}/{DateTime.Now.ToString ("yyyyMMdd")}.log", FileMode.Append))
						fs.Write (bytes, 0, bytes.Length);
				}
			}
		}

		// 打印信息
		public static void show_info (string str, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
			int p = file.LastIndexOfAny (new char [] { '/', '\\' });
			if (p >= 0)
				file = file.Substring (p + 1);
			show ($"[{log_datetime}][{file}][{line}] {str}");
		}

		// 显示错误
		public static void show_error (Exception ex, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
			show_info (ex.ToString (), file, line);
		}

		public static string m_path {
			set {
				_path = $"{value}log";
				if (!Directory.Exists (m_path))
					Directory.CreateDirectory (m_path);
			}
			get { return _path; }
		}
		private static string _path = "";

		private static object m_locker = new object ();
		private static string log_datetime { get { return DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss.fff"); } }
		public static object g_locker = new object ();
	}
}
