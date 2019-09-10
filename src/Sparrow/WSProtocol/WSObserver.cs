using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sparrow.WSProtocol {
	public class WSObserver {
		public virtual void OnConnect (JObject _obj_jwt) { }
		public virtual void OnPong () {
			Console.WriteLine ("OnPong");
		}
		public virtual void OnRecv (byte [] _data) {
			Console.WriteLine ($"recv byte []: {_data.to_str ()}");
			send (_data);
		}
		public virtual void OnRecv (string _data) {
			Console.WriteLine ($"recv string: {_data}");
			send (_data);
		}
		public virtual void OnError () { }
		public virtual void OnClose () { }

		public void send (byte [] _data) {
			_send (_data, 2);
		}

		public void send (string content) {
			_send (content.to_bytes (), 1);
		}

		//public void send_jwt_token (object _obj_jwt, DateTime _exp) {
		//	_send (new { type = "jwt_token", content = JWTManager.Generate (_obj_jwt, _exp) }.to_json ().to_bytes (), 1);
		//}

		private void _send (byte [] _data, int _opcode) {
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
			m_stream.Write (_send_data.ToArray ());
		}

		internal void _main_loop (Stream _stream) {
			m_stream = _stream;
			var _send_ping_time = DateTime.Now.AddSeconds (20);
			bool _wait_ping = false;
			while (true) {
				//////var _buf = new byte [] { 0, 0 };
				//////// https://www.jianshu.com/p/f666da1b1835
				//////CancellationTokenSource _source = new CancellationTokenSource (m_alive_websocket_ms);
				////////int _ret = _stream.Read (_buf);
				//////int _ret = _stream.ReadAsync (_buf, _source.Token).Result;
				//////if (_ret < 2)
				//////	break;
				//////_source.CancelAfter (m_alive_websocket_ms);
				//////bool _is_eof = (_buf [0] & 0x80) > 0;
				//////if ((_buf [0] & 0x70) > 0)
				//////	throw new Exception ("RSV1~RSV3 is not 0");
				//////// TODO: 处理连接
				int _byte1 = _stream.ReadByte ();
				if (_byte1 == -1) {
					if (DateTime.Now < _send_ping_time) {
						Thread.Sleep (100);
						continue;
					} else if (!_wait_ping) {
						_send_ping_time = DateTime.Now.AddSeconds (5);
						_send (null, 9);
						_wait_ping = true;
						continue;
					}
					break;
				}
				int _byte2 = _stream.ReadByte ();
				if (_byte2 == -1)
					break;
				//
				_wait_ping = false;
				_send_ping_time = DateTime.Now.AddSeconds (20);
				//
				bool _is_eof = (_byte1 & 0x80) > 0;
				int _opcode = _byte1 & 0xf;
				if (_opcode == 8 || (_byte2 & 0x80) == 0) {
					// close connection
					break;
				} else if (_opcode == 9) {
					// ping
					_send (null, 10);
				} else if (_opcode == 10) {
					// pong
					OnPong ();
				} else {
					long _payload_length = (_byte2 & 0x7f);
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
					//
					var _mask = new byte [4];
					for (int i = 0; i < 4; ++i) {
						if ((_byte2 = _stream.ReadByte ()) == -1)
							break;
						_mask [i] = (byte) _byte2;
					}
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

		private Stream m_stream = null;
		public long m_id { get; private set; } = ++s_last_id;
		private static long s_last_id = -1;
	}
}
