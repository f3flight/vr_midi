#if false

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TobiasErichsen.teVirtualMIDI;

public class VirtualMIDIOut : MonoBehaviour
{
	public static TeVirtualMIDI outPort;
	public static byte channel = 0;

	void Start ()
	{
		outPort = new TeVirtualMIDI ("test_midi_VR_out");
	}

	void OnApplicationQuit() {
		outPort.shutdown ();
	}

	public static void SendNoteOn (byte note, byte velocity)
	{
		Debug.Log ("sending midi NoteOn message - channel" + channel + "note " + note + " velocity " + velocity);
		byte[] message = new byte[3];
		message [0] = (byte)(channel + 0x9 << 4); // 0x9 - constant header for noteOn
		message [1] = note;
		message [2] = velocity;
		outPort.sendCommand (message);
	}

	public static void SendNoteOff (byte note, byte velocity)
	{
		Debug.Log ("sending midi NoteOff message - channel" + channel + "note " + note + " velocity " + velocity);
		byte[] message = new byte[3];
		message [0] = (byte)(channel + 0x8 << 4); // 0x8 - constant header for noteOff
		message [1] = note;
		message [2] = velocity;
		outPort.sendCommand (message);
	}
}

#endif