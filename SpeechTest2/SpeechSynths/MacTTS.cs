using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SpeechTest2.SpeechSynths {
	class MacTTS : DefaultTTS {
		public new const string name = "MacTTS";
		private HttpClient client;

		public MacTTS() {
			client = new HttpClient();
		}

		public override void Skip() { }

		public override DefaultConfig Prep(DefaultConfig cfg) {
			lock (ilock) {
				cfg.id = id++;
			}
			
			var voices = this.GetVoices();
			if (!voices.Contains(cfg.voice)) {
				cfg.voice = voices[0];
			}

			Task<MediaPlayer> tsk = new Task<MediaPlayer>(() => {
				var vars = new Dictionary<string, string>
					{
					{ "text", HttpUtility.UrlEncode(cfg.msg.Text) },
					{ "voice", cfg.voice },
					{ "rate", cfg.wpm.ToString() },
				};
				string args = string.Join("&", vars.Select((e) => $"{e.Key}={e.Value}").ToList());

				var request = (HttpWebRequest)WebRequest.Create($"http://{Program.config.tts_ip}:{Program.config.tts_port}/say?{args}");
				HttpWebResponse response;

				try {
					response = (HttpWebResponse)request.GetResponse();
				} catch (WebException) {
					Console.WriteLine("Error connecting to TTS server!");
					return null;
				}

				if (response.ContentType != "audio/x-wav") {
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

		private class Voice {
			public string Name { get; set; }
			public string Region { get; set; }
			public string Info { get; set; }
		}

		public override string[] GetVoices() {
			var request = (HttpWebRequest)WebRequest.Create($"http://{Program.config.tts_ip}:{Program.config.tts_port}/voices");
			HttpWebResponse response;

			try {
				response = (HttpWebResponse)request.GetResponse();
			} catch (WebException) {
				Console.WriteLine("Error connecting to TTS server!");
				return null;
			}
			StreamReader readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);

			string w = readStream.ReadToEnd();
			List<List<string>> myProduct = JsonConvert.DeserializeObject<List<List<string>>>(w);

			return myProduct.Select(i => i[0]).ToArray();
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
