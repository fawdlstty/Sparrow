using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sparrow.WSProtocol {
	public class WSObserver {
		public virtual void OnRecv (byte [] _data) { }
		public virtual void OnRecv (string _data) { }

		public bool send (byte [] _data) {
			return _send (_data, 2);
		}

		public bool send (string content) {
			return _send (content.to_bytes (), 1);
		}

		private bool _send (byte [] _data, int _opcode) {
			try {
				long _length = _data?.Length ?? 0;
				var _send_data = new List<byte> ();
				var _mask = new byte [4] { 255, 255, 255, 255 };
				//byte [] _data_send = new byte [_data?.Length ?? 0];
				//for (int i = 0; i < _data_send.Length; ++i)
				//	_data_send [i] = (byte) (_data [i] ^ _data_send [i % 4]);
				if (_length < 126) {
					_send_data.Add ((byte) (0x80 | _opcode));
					_send_data.Add ((byte) (/*0x80 |*/ _length));
					//_send_data.AddRange (_mask);
					if (_length > 0)
						_send_data.AddRange (_data);
				} else if (_length <= 0xffff) {
					_send_data.Add ((byte) (0x80 | _opcode));
					_send_data.Add ((byte) (/*0x80 |*/ 126));
					_send_data.Add ((byte) ((_length >> 8) & 0xff));
					_send_data.Add ((byte) (_length & 0xff));
					_send_data.AddRange (_mask);
					_send_data.AddRange (_data);
				} else {
					_send_data.Add ((byte) (0x80 | _opcode));
					_send_data.Add ((byte) (/*0x80 |*/ 127));
					_send_data.Add ((byte) ((_length >> 56) & 0xff));
					_send_data.Add ((byte) ((_length >> 48) & 0xff));
					_send_data.Add ((byte) ((_length >> 40) & 0xff));
					_send_data.Add ((byte) ((_length >> 32) & 0xff));
					_send_data.Add ((byte) ((_length >> 24) & 0xff));
					_send_data.Add ((byte) ((_length >> 16) & 0xff));
					_send_data.Add ((byte) ((_length >> 8) & 0xff));
					_send_data.Add ((byte) (_length & 0xff));
					_send_data.AddRange (_mask);
					_send_data.AddRange (_data);
				}
				lock (m_stream)
					m_stream.Write (_send_data.ToArray ());
				return true;
			} catch (IOException) {
				return false;
			}
		}

		internal void _main_loop (Stream _stream) {
			m_stream = _stream;
			var _send_ping_time = DateTime.Now.AddSeconds (20);
			bool _wait_ping = false;
			//var _t = new Timer ((_state) => { }, null, 0, 20000);
			while (true) {
				//var _buf = new byte [] { 0, 0 };
				//AsyncCallback _callback = (_ar) => {
				//	try {
				//		int _read = _stream.EndRead (ar);
				//	}
				//};
				//_stream.BeginRead (_buf, 0, _buf.Length, _callback, null);
				//
				//
				int _byte1 = -1;
				try {
					_byte1 = _stream.ReadByte ();
				} catch (IOException) {
					if (!_stream.CanWrite)
						break;
				}
				if (_byte1 == -1) {
					if (DateTime.Now < _send_ping_time) {
						Thread.Sleep (100);
						continue;
					} else if (!_wait_ping) {
						_send_ping_time = DateTime.Now.AddSeconds (5);
						if (!_send (null, 9))
							break;
						_wait_ping = true;
						continue;
					}
					break;
				}
				////bool _is_eof = (_buf [0] & 0x80) > 0;
				////if ((_buf [0] & 0x70) > 0)
				////	throw new Exception ("RSV1~RSV3 is not 0");
				int _byte2 = _stream.ReadByte ();
				if (_byte2 == -1)
					break;
				var _buf = new byte [] { (byte) _byte1, (byte) _byte2 };
				//
				//
				//
				_wait_ping = false;
				_send_ping_time = DateTime.Now.AddSeconds (20);
				//
				bool _is_eof = (_buf [0] & 0x80) > 0;
				int _opcode = _buf [0] & 0xf;
				if (_opcode == 8 || (_buf [1] & 0x80) == 0) {
					// close connection
					break;
				} else {
					long _payload_length = 0;
					if (_opcode != 9 && _opcode != 10) {
						_payload_length = (_buf [1] & 0x7f);
						if (_payload_length == 126) {
							_payload_length = 0;
							for (int i = 0; i < 2; ++i) {
								if ((_byte2 = _stream.ReadByte ()) == -1)
									break;
								_payload_length = (_payload_length << 8) + _byte2;
							}
						} else if (_payload_length == 127) {
							_payload_length = 0;
							for (int i = 0; i < 8; ++i) {
								if ((_byte2 = _stream.ReadByte ()) == -1)
									break;
								_payload_length = (_payload_length << 8) + _byte2;
							}
						}
					}
					//
					var _mask = new byte [4];
					for (int i = 0; i < 4; ++i) {
						if ((_byte2 = _stream.ReadByte ()) == -1)
							break;
						_mask [i] = (byte) _byte2;
					}
					//
					if (_opcode == 9) {
						// ping
						if (!_send (null, 10))
							break;
					} else if (_opcode == 10) {
						// pong
					} else {
						var _content = new byte [_payload_length];
						int _readed = _stream.Read (_content);
						while (_readed < _payload_length) {
							Thread.Sleep (10);
							_readed += _stream.Read (_content, _readed, _content.Length - _readed);
						}
						for (int i = 0; i < _payload_length; ++i) {
							_content [i] ^= _mask [i % 4];
						}
						if (_opcode == 1) {
							OnRecv (_content.to_str ());
						} else if (_opcode == 2) {
							OnRecv (_content);
						}
					}
				}
			}
		}

		private Stream m_stream = null;
		public long m_id { get; private set; } = ++s_last_id;
		private static long s_last_id = -1;
	}
}
