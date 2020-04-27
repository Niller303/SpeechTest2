using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SpeechTest2.SpeechSynths {
	class MaryTTS : DefaultTTS {
		public new const string name = "MaryTTS";
		private HttpClient client;

		public MaryTTS() {
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
				var vars = new Dictionary<string, string>
					{
					{ "INPUT_TEXT", HttpUtility.UrlEncode(cfg.msg.Text) },
					{ "INPUT_TYPE", "TEXT" },
					{ "LOCALE", ((MaryConfig)cfg).tts_locale },
					{ "VOICE", cfg.voice },
					{ "OUTPUT_TYPE", "AUDIO" },
					{ "AUDIO", "WAVE" },
				};
				string args = string.Join("&", vars.Select((e) => $"{e.Key}={e.Value}").ToList());

				var request = (HttpWebRequest)WebRequest.Create($"http://{Program.config.tts_ip}:{Program.config.tts_port}/process?{args}");
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

		public override void Speak(DefaultConfig cfg) {
			Task<MediaPlayer> text = cfg.obj;

			text.Wait();
			if (text.Result != null) {
				text.Result.Play();
			} else {
				Console.WriteLine("Failed to write text!");
			}
		}

		public class MaryConfig : DefaultTTS.DefaultConfig {
			public string tts_locale = "en_US";

			//en_US
			//{ "VOICE", "cmu-bdl" }, //Nicer
			//{ "VOICE", "cmu-rms" }, //Nicest
			//{ "VOICE", "cmu-slt" }, //Nice
			//en_GB
			//{ "VOICE", "dfki-obadiah" },
			//{ "VOICE", "dfki-poppy" },
			//{ "VOICE", "dfki-prudence" },
			//{ "VOICE", "dfki_spike" },

			//All have -hsmm versions 2
		}
	}
}
