using Newtonsoft.Json;
using SpeechTest2.SpeechSynths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechTest2 {
	class Settings {
		public static Settings LoadConfig() {
			if (!File.Exists(@"config.json")) {
				return new Settings();
			}

			return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"config.json"));
		}
		public static string SerializeObject<T>(T value) {
			StringBuilder sb = new StringBuilder(256);
			StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);

			var jsonSerializer = JsonSerializer.CreateDefault();
			using (JsonTextWriter jsonWriter = new JsonTextWriter(sw)) {
				jsonWriter.Formatting = Formatting.Indented;
				jsonWriter.IndentChar = '\t';
				jsonWriter.Indentation = 1;

				jsonSerializer.Serialize(jsonWriter, value, typeof(T));
			}

			return sw.ToString();
		}
		public void SaveConfig() {
			File.WriteAllText(@"config.json", SerializeObject(this));
		}

		internal Settings() {}

		//Speaker
		public string tts_ip = "localhost";
		public string tts_port = "5000";
		public string tts_module = MSTTS.name;
		public string default_voice = "Microsoft Zira Desktop";
		public Dictionary<string, string> custom_voices = new Dictionary<string, string>();
		public Dictionary<string, float> custom_rate = new Dictionary<string, float>();
		public List<string> mutes = new List<string>();

		//Streamer
		public string username = null;
		public string access_token = null;
		public string clientid = null;
		public string speaker_prerequisite = "";
		public string format_onjoin = "Hello im SpeechTest2 i just joined {0}'s channel!";
		public string format_onsub = "Pog {0} just subbed!";
		public string format_onsubprime = "Pog {0} just subbed using bezos prime!";
		public string format_onfollow = "{0} just followed a Peepga!";
		public List<string> badwords = new List<string> {};
		public int badword_penalty = 5; //In minutes

		//Command handler
		public string command_prerequisite = "s!";

	}
}
