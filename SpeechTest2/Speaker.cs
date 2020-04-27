using SpeechTest2.SpeechSynths;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechTest2 {
	class Speaker : IDisposable {
		private BlockingCollection<SpeechSynths.DefaultTTS.DefaultConfig> textQueue;
		public Message currentMessage;
		public SpeechSynths.DefaultTTS speaker;
		private Thread speakThread;
		private CancellationTokenSource speakerCancelToken;

		public Speaker() {
			this.textQueue = new BlockingCollection<SpeechSynths.DefaultTTS.DefaultConfig>(new ConcurrentQueue<SpeechSynths.DefaultTTS.DefaultConfig>());
			this.speakThread = new Thread(SpeakRunner);
			this.speakerCancelToken = new CancellationTokenSource();
			this.speakThread.Start();

			switch (Program.config.tts_module) {
				case MacTTS.name:
					speaker = new MacTTS();
					break;
				case MaryTTS.name:
					speaker = new MaryTTS();
					break;
				case MozillaTTS.name:
					speaker = new MozillaTTS();
					break;
				case MSTTS.name:
				default:
					speaker = new MSTTS();
					break;
			}
		}

		private void SpeakRunner() {
			while (!speakerCancelToken.IsCancellationRequested) {
				DefaultTTS.DefaultConfig elm;
				try {
					Console.WriteLine("wait");
					elm = textQueue.Take(speakerCancelToken.Token);
					Console.WriteLine("awake");
				} catch (OperationCanceledException) {
					continue;
				}

				currentMessage = elm.msg;
				Console.WriteLine("SpeakRunner");
				try {
					Monitor.Enter(speakThread);
					speaker.Speak(elm);
					currentMessage = null;
				} catch (ThreadInterruptedException) {
					speaker.Skip(); //Some of them needs this, async bull
				} finally {
					Monitor.Exit(speakThread);
				}
			}
		}

		public void Speak(Message msg) {
			/*if (this.speaker. == MaryTTS.name) {
				DefaultTTS.DefaultConfig cfg = new SpeechSynths.MaryTTS.MaryConfig();
				((MaryTTS.MaryConfig)cfg).tts_locale = "en_US";
			} else {
				DefaultTTS.DefaultConfig cfg = new SpeechSynths.DefaultTTS.DefaultConfig();
			}*/
			DefaultTTS.DefaultConfig cfg = new SpeechSynths.DefaultTTS.DefaultConfig();
			
			cfg.msg = msg;
			cfg.voice = Program.config.default_voice;
			Console.WriteLine(msg.User);
			Console.WriteLine(msg.Text);
			if (msg.User != null) { //Internal message
				if (Program.config.custom_voices.ContainsKey(msg.User)) {
					cfg.voice = Program.config.custom_voices[msg.User];
				}
				if (Program.config.custom_rate.ContainsKey(msg.User)) {
					cfg.wpm = Program.config.custom_rate[msg.User];
				}
			}
			textQueue.Add(speaker.Prep(cfg));
		}

		/*
		 * Stops the current message from being read
		 */
		public void SkipSpeak() {
			if (!Monitor.TryEnter(speakThread)) {
				speakThread.Interrupt();
			} else {
				Monitor.Exit(speakThread);
			}
		}
		
		public void Dispose() {
			this.speakerCancelToken.Cancel();
			this.speakThread.Join();
			this.textQueue.Dispose();
			this.speakerCancelToken.Dispose();
		}
	}
}
