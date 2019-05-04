using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Midi;

public class BassUnitySoundGenerator : MonoBehaviour {

	int bassMidiStream;
	AudioClip realtimeAudio;
	AudioSource generatedAudioSource;
	float[] buffer;
	bool isPlaying;

	public void init () {
		buffer = new float[AudioSettings.outputSampleRate]; // 1 second buffer just in case
		bassMidiStream = BassMidi.BASS_MIDI_StreamCreate (1, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE, AudioSettings.outputSampleRate);
		realtimeAudio = AudioClip.Create ("realtimeAudio", 1, 1, AudioSettings.outputSampleRate, false);
		realtimeAudio.SetData (new float[] { 1 }, 0);
		generatedAudioSource = gameObject.AddComponent<AudioSource> () as AudioSource;
		generatedAudioSource.clip = realtimeAudio;
		generatedAudioSource.loop = true;
		generatedAudioSource.spatialBlend = 1;
		//generatedAudioSource.Play ();
		Debug.Log ("generator started");
	}
		
	void OnApplicationQuit() {
		//Debug.Log ("Freeing midi stream");
		//Bass.BASS_StreamFree (bassMidiStream);
	}

	void CopyFromStream(int length) {
		GCHandle hGC = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		Bass.BASS_ChannelGetData(bassMidiStream, hGC.AddrOfPinnedObject(), length*4);
		hGC.Free();
	}
	
	void OnAudioFilterRead(float[] data, int channels) {
		if (isPlaying) { // This does not really disable the source, but at least should decrease CPU usage
			CopyFromStream (data.Length);
			for (int i = 0; i < data.Length; i++) {
				data [i] *= buffer [i];
			}
		}
	}
	
	public void NoteEvent(int note, int velocity, bool noteOn) {
		if (noteOn) {
			CancelInvoke ();
			if (!generatedAudioSource.isPlaying) {
				isPlaying = true;
				generatedAudioSource.Play ();
			}
		}
		if (!BassMidi.BASS_MIDI_StreamEvent (bassMidiStream, 0, BASSMIDIEvent.MIDI_EVENT_NOTE, (ushort)(note | (velocity << 8)))) {
			Debug.LogError ("Failed to send midi event to BASS Midi Stream");
		} else {
			Debug.Log ("Sent midi event to BASS Midi Stream " + bassMidiStream + ", note = " + note + ", velocity = " + velocity);
		}
		if (!noteOn) {
			Invoke ("StopPlaying", 3);
		}
	}

	void StopPlaying() {
		isPlaying = false;
		generatedAudioSource.Stop ();
	}
}
