using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Un4seen.Bass;

public class BassUnityInit : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (!Bass.BASS_Init (-1, AudioSettings.outputSampleRate, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero)) {
			Debug.LogError ("Failed to initialize BASS");
		} else {
			Debug.Log ("BASS Initialized");
		}
	}

	void OnApplicationQuit () {
		Bass.BASS_Free ();
	}
}
