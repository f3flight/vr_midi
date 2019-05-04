using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Midi;

public class BassUnitySoundfont : MonoBehaviour {

	public string pathToSoundfont = "";
	public int bassMidiFontHandle;
	public TextAsset embeddedSoundfont;

	static byte[] embeddedSoundfontBytes;
	static int ms_pos;

	void Start () {
		if (pathToSoundfont.Length > 0 && File.Exists (pathToSoundfont)) {
			Debug.Log ("Loading soundfont by path");
			bassMidiFontHandle = BassMidi.BASS_MIDI_FontInit (pathToSoundfont);
		} else if (embeddedSoundfont != null) {
			Debug.Log ("Loading soundfont from assets");

			embeddedSoundfontBytes = embeddedSoundfont.bytes;
			BASS_FILEPROCS fileprocs = new BASS_FILEPROCS (
				new FILECLOSEPROC((user) => {
					Debug.Log("Soundfont - close");
					// do nothing, MemoryStream will be taken care of automatically;
				}),
				new FILELENPROC((user) => {
					Debug.Log("Soundfont - length");
					return embeddedSoundfontBytes.Length;}),
				new FILEREADPROC((buffer, length, user) => {
					int max_length = (int)Mathf.Min(Mathf.Max(0, embeddedSoundfontBytes.Length - ms_pos), length);
					Debug.Log("Soundfont - read, length = " + length + ", pos = " + ms_pos + ", max_length = " + max_length);
					System.Runtime.InteropServices.Marshal.Copy(embeddedSoundfontBytes, ms_pos, buffer, max_length);
					ms_pos += max_length;
					return max_length;}),
				new FILESEEKPROC((offset, user) => {
					Debug.Log("Soundfont - seek, offset = " + offset);
					if (offset < embeddedSoundfontBytes.Length) {
						ms_pos = (int)offset;
						return true;
					} else {
						return false;
					}})
			);
			bassMidiFontHandle = BassMidi.BASS_MIDI_FontInitUser (fileprocs, System.IntPtr.Zero, 0);
		} else {
			Debug.LogError ("Failed to initialize soundfont. Configure the scirpt object!");
			return;
		}

		BASS_MIDI_FONT[] BassMidiFontArray = { new BASS_MIDI_FONT (bassMidiFontHandle, -1, 0) };
		BassMidi.BASS_MIDI_StreamSetFonts (0, BassMidiFontArray, 1);
	}

	void OnApplicationQuit () {
		//if (!BassMidi.BASS_MIDI_FontFree (bassMidiFontHandle)) {
		//	Debug.LogError ("Failed to free the soundfont");
		//} else {
		//	Debug.Log ("Freed the soundfont");
		//}
	}
}
