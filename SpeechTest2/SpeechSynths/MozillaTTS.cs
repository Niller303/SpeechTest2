using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SpeechTest2.SpeechSynths {
	class MozillaTTS : DefaultTTS {
		public new const string name = "MozillaTTS";
		private HttpClient client = new HttpClient();

		public MozillaTTS() {
			client = new HttpClient();
		}

		public override string[] GetVoices() {
			return new string[] {};
		}

		public override void Skip() {}

		public override DefaultConfig Prep(DefaultConfig cfg) {
			lock (ilock) {
				cfg.id = id++;
			}

			Task<MediaPlayer> tsk = new Task<MediaPlayer>(() => {
				var request = (HttpWebRequest)WebRequest.Create($"http://{Program.config.tts_ip}:{Program.config.tts_port}/api/tts?text={HttpUtility.UrlEncode(cfg.msg.Text)}");
				HttpWebResponse response;

				try {
					response = (HttpWebResponse)request.GetResponse();
				} catch (WebException) {
					Console.WriteLine("Error connecting to TTS server!");
					return null;
				}

				if (response.ContentType != "audio/wav") {
					Console.WriteLine("Invalid content type!");
					return null;
				}

				using (var stream = response.GetResponseStream()) {
					return new MediaPlayer(stream);
				}
			});
			tsk.Start();
			cfg.obj = tsk;

			return cfg;
		}

		public override void Speak(DefaultConfig cfg) {
			Task<MediaPlayer> text = cfg.obj;

			text.Wait();
			if (text.Result != null) {
				text.Result.Play();
			} else {
				Console.WriteLine("Failed to write text!");
			}
		}
	}
}
