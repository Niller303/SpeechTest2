from flask import Flask, request, Response
import os, time, subprocess, re, json, tempfile

app = Flask(__name__)

@app.route("/say")
def say():
	text = request.args.get("text")
	voice = request.args.get("voice")
	#rate = request.args.get("rate")

	if (text == None):
		return "no text", 400

	uid = str(int(time.time()*1000))
	output_file = uid + ".wav" #Use tempfile dir
	#input_file = uid + ".txt" #Consider using an input file
	#https://ss64.com/osx/say.html
	cmds = [
		"say",
		"-v",
		voice if voice else "Samantha",
		"--output-file=" + output_file,
		"--data-format=LEF32@32000",
		#"--channels=2", #Stereo
		#"--bit-rate=AAC",
		#"--quality=127",
		"\"" + text + "\""
	]

	res = subprocess.run(cmds, shell=False, check=True)

	#thanks subprocess really cool
	output_file = [f for f in os.listdir(".") if output_file in f][0]

	def generate():
		with open(output_file, "rb") as fwav:
			while (True):
				data = fwav.read(1024)
				if (not data):
					break
				yield data
		os.remove(output_file)
	
	return Response(generate(), mimetype="audio/x-wav")

@app.route("/voices")
def voices():
	proc = subprocess.Popen(["say","-v","?"],stdout=subprocess.PIPE)
	out = proc.communicate()[0].decode().split("\n")

	voices = []
	for i in out:
		result = re.match("(.*) (\w{2}\_\w{2}) (.*)", i)
		if (result):
			voices.append([
				result.group(1).strip(),
				result.group(2).strip(),
				result.group(3).strip(),
			])

	return json.dumps(voices), 200

if __name__ == "__main__":
	app.run(host="0.0.0.0", port=5000)