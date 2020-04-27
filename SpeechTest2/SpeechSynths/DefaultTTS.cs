using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechTest2.SpeechSynths {
	abstract class DefaultTTS {
		public object ilock = new object();
		public int id = 0;
		public abstract DefaultConfig Prep(DefaultConfig cfg);
		public abstract void Speak(DefaultConfig cfg);
		public abstract string[] GetVoices();
		public abstract void Skip();

		public class DefaultConfig {
			internal int id;
			internal dynamic obj;

			public Message msg;
			public string voice;
			public float wpm;
		}
	}
}
