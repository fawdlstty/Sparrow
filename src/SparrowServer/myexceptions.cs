using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer {
	class MyHttpException : Exception {
		public MyHttpException (int error_num) { m_error_num = error_num; }
		public int m_error_num = 200;
	}
}
