using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechTest2.SpeechSynths {
	class MSTTS : DefaultTTS{
		public new const string name = "MS";
		private SpeechSynthesizer synth;

		public MSTTS() {
			// Initialize a new instance of the SpeechSynthesizer.  
			this.synth = new SpeechSynthesizer();

			// Configure the audio output.
			this.synth.SetOutputToDefaultAudioDevice();
		}

		public override DefaultConfig Prep(DefaultConfig cfg) {
			lock (ilock) {
				cfg.id = id++;
			}

			return cfg;
		}

		public override void Speak(DefaultConfig cfg) {
			var voices = this.GetVoices();
			if (!voices.Contains(cfg.voice)) {
				cfg.voice = voices[0];
			}
			this.synth.SelectVoice(cfg.voice);
			var p = this.synth.SpeakAsync(cfg.msg.Text);
			while (!p.IsCompleted) {
				Thread.Sleep(250);
			}
		}

		public override string[] GetVoices() {
			return this.synth.GetInstalledVoices().Select((v) => v.VoiceInfo.Name).ToArray();
		}

		public override void Skip() {
			this.synth.SpeakAsyncCancelAll();
		}
	}
}
