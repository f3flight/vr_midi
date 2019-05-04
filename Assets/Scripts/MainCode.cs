using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiJack;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

public class MainCode : MonoBehaviour
{
    public float[] VtoA(Vector3 v)
    {
        return new float[] { v.x, v.y, v.z };
    }

    public struct key_pos_sample
    {
        public int octave;
        public int sub_note;
        public Vector3 pos;

        public key_pos_sample(int p1, int p2, Vector3 p3)
        {
            octave = p1;
            sub_note = p2;
            pos = p3;
        }
    }

    public float get_white_key_num(int octave, int sub_note)
    {
        return octave * 7 + sub_note_pos[sub_note];
    }
#if UNITY_EDITOR_WIN
    MidiDriver driver;
#endif
    Dictionary<int, GameObject> keys = new Dictionary<int, GameObject>();
    Dictionary<GameObject, int> notes = new Dictionary<GameObject, int>();
    Dictionary<int, GameObject> key_rotators = new Dictionary<int, GameObject>();
    Dictionary<int, Color> colors = new Dictionary<int, Color>();
    List<Vector3> calibration_xz = new List<Vector3>();
    List<Vector3> calibration_xy = new List<Vector3>();
    List<key_pos_sample> calibration_scale = new List<key_pos_sample>();
    float pianoKeyModelWidth;
    float default_piano_key_width;
    float piano_key_width;
    float piano_key_length;
    float piano_key_height;
    float piano_key_gap_size = 0.001f; //gap between white keys, which fits unscaled white key prefab size
    float key_press_rotation_x = -4; //pressed key rotation angle, degrees
    List<float> sub_note_pos = new List<float> { 0, 0.5f, 1, 1.5f, 2, 3, 3.5f, 4, 4.5f, 5, 5.5f, 6 }; //for centered keys
                                                                                                      //List<float> sub_note_pos = new List<float> { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 }; //for offset black keys
    List<int> sub_note_black = new List<int> { 1, 3, 6, 8, 10 };
    Renderer r;
    float controller_bottom_y;
    int sub_note;
    int octave;
    int min_key = -1;
    int max_key = -1;
    public GameObject piano;
    public GameObject controller_right_gameobject;
    public GameObject oculus_touch_right_low_poly_model;
    public GameObject oculus_touch_right_direction_plane;
    public GameObject windows_controller_right_low_poly_model;
    public GameObject windows_controller_right_direction_plane;
    public GameObject low_poly_model;
    public GameObject controller_direction_plane;
    public GameObject piano_xy_plane;
    public GameObject piano_initial_rotation_wrapper;
    public GameObject desktop_plane_wrapper;
    public GameObject particle_system;
    Vector3 lowest_vertex = new Vector3();
    Vector3 temp_vector;
    Vector3 center;
    float lowest_y;
    int lowest_y_index;
    Bounds bnds;
    public GameObject body;
    GameObject sidewood_left, sidewood_right, pianoBody;
    bool initialized = false;
    public Object whiteKeyPrefab;
    public Object blackKeyPrefab;
    public Object sidePanelPrefab;
    public Object pianoBodyPrefab;
    int debug_note_gen_index = 0;
    bool post_start_init = false;
    // Use this for initialization
    void Start()
    {
#if UNITY_EDITOR_WIN
        driver = MidiDriver.Instance;
		driver.noteOnDelegate = note_on;
#endif
        GameObject test_key = Instantiate (whiteKeyPrefab) as GameObject;
		pianoKeyModelWidth = test_key.GetComponentInChildren<Renderer> ().bounds.size.x;
		default_piano_key_width = pianoKeyModelWidth + piano_key_gap_size;
		piano_key_width = default_piano_key_width;
		Debug.Log ("default piano key size = " + piano_key_width);
		piano_key_length = test_key.GetComponentInChildren<Renderer> ().bounds.size.z;
		piano_key_height = test_key.GetComponentInChildren<Renderer> ().bounds.size.y;
		Destroy (test_key);
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!post_start_init) {
			if (SteamVR.active) {
				Debug.Log ("SteamVR TrackingSystemName = " + SteamVR.instance.hmd_TrackingSystemName + "; model =" + SteamVR.instance.hmd_ModelNumber);
				Debug.Log ("And UnityEngine.XR says the model is = " + UnityEngine.XR.XRDevice.model);
				if (SteamVR.instance.hmd_TrackingSystemName.Contains ("holo")) {
					low_poly_model = windows_controller_right_low_poly_model;
					controller_direction_plane = windows_controller_right_direction_plane;
				} else if (SteamVR.instance.hmd_TrackingSystemName.Contains ("oculus")) {
					low_poly_model = oculus_touch_right_low_poly_model;
					controller_direction_plane = oculus_touch_right_direction_plane;
				}

				piano_initial_rotation_wrapper.transform.parent = GameObject.FindGameObjectWithTag ("Stage").transform;
				desktop_plane_wrapper.transform.parent = GameObject.FindGameObjectWithTag ("Stage").transform;

				post_start_init = true;
			}
		}
		if (Input.GetButtonDown ("Debug_P")) {
			note_on (MidiChannel.Ch1, debug_note_gen_index, 100);
			//VirtualMIDIOut.SendNoteOn ((byte)debug_note_gen_index, 100);
		}
		if (Input.GetButtonUp ("Debug_P")) {
			note_on (MidiChannel.Ch1, debug_note_gen_index, 0);
			//VirtualMIDIOut.SendNoteOff ((byte)debug_note_gen_index, 0);
			debug_note_gen_index ++;
		}

		if (Input.GetButtonDown ("Debug_0")) {
			note_on (MidiChannel.Ch1, 0, 100);
			//VirtualMIDIOut.SendNoteOn ((byte)debug_note_gen_index, 100);
		}
		if (Input.GetButtonUp ("Debug_0")) {
			note_on (MidiChannel.Ch1, 0, 0);
			//VirtualMIDIOut.SendNoteOff ((byte)debug_note_gen_index, 0);
		}

		if (Input.GetButtonDown ("Fire1") && initialized) {			
			Vector3 sample = controller_direction_plane.transform.position;

			GameObject dot = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			sample.y = 0;
			dot.transform.position = sample;
			dot.transform.localScale = new Vector3 (0.005f, 0.005f, 0.005f);
			dot.GetComponent<Renderer> ().material.color = Color.red;

			if (calibration_xz.Count > 9) {
				calibration_xz.RemoveAt (0);
			}

			calibration_xz.Add (sample);
			if (calibration_xz.Count > 1) {
				float[,] calibration_X_array = new float [calibration_xz.Count, 2];
				float[] calibration_y_array = new float [calibration_xz.Count];
				for (int i = 0; i < calibration_xz.Count; i++) {
					calibration_X_array [i, 0] = 1;
					calibration_X_array [i, 1] = piano_initial_rotation_wrapper.transform.InverseTransformPoint (calibration_xz [i]).x;
					calibration_y_array [i] = piano_initial_rotation_wrapper.transform.InverseTransformPoint (calibration_xz [i]).z;
				}
				Matrix<float> X = Matrix<float>.Build.DenseOfArray (calibration_X_array);
				Vector<float> y = Vector<float>.Build.DenseOfArray (calibration_y_array);
				Vector<float> s = X.Solve (y);
				Debug.Log ("Y-Angle - Solved: " + s.ToString ());
				float y_angle = Mathf.Rad2Deg * Mathf.Atan (s [1]);
				Debug.Log ("y_angle = " + y_angle.ToString ());
				;
				Vector3 current_rotation = piano.transform.localRotation.eulerAngles;
				piano.transform.localRotation = Quaternion.Euler (current_rotation.x, -y_angle, current_rotation.z);
				piano_xy_plane.transform.localRotation = piano.transform.localRotation;
				Vector3 new_position = piano.transform.localPosition;
				Debug.Log ("piano.transform.localPposition = " + new_position);
				new_position.z = s [0] + new_position.x * s [1];
				Debug.Log ("new_position = " + new_position);
				float z_shift = piano.transform.localPosition.z - new_position.z;
				Debug.Log ("z_shift = " + z_shift);
				piano.transform.localPosition = new_position;
				piano_xy_plane.transform.localPosition = new_position;
			} else {
				Vector3 initial_rotation_vector = controller_direction_plane.transform.up;
				initial_rotation_vector.y = 0; // drop vertical component
				piano.transform.rotation = Quaternion.LookRotation (initial_rotation_vector);
				Plane plane = new Plane (controller_direction_plane.transform.up, controller_direction_plane.transform.position);
				Ray ray = new Ray (piano.transform.position, initial_rotation_vector);
				float distance;
				plane.Raycast (ray, out distance);
				piano.transform.Translate (initial_rotation_vector.normalized * distance);
				update_virtual_desktop_position ();
			}
		} 
//		else if (Input.anyKeyDown) {
//			foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode))) {
//				if (Input.GetKeyDown (code)) {
//					Debug.Log (System.Enum.GetName (typeof(KeyCode), code));
//				}
//			}
//		}

	}

	void update_virtual_desktop_position() {
		
		var allChildren = piano.transform.gameObject.GetComponentsInChildren<Transform>();
		center = Vector3.zero;
		foreach (var child in allChildren)
		{
			center += child.transform.position;
		}
		center = center / allChildren.Length;
		desktop_plane_wrapper.transform.position = center;
		desktop_plane_wrapper.transform.rotation = piano.transform.rotation;
		desktop_plane_wrapper.SetActive (true);
	}

	void lowest_vertex_update ()
	{
		if (body == null) {
			if (controller_right_gameobject.transform.parent.Find ("Model") != null) {
				if (controller_right_gameobject.transform.parent.Find ("Model").Find ("body") != null) {
					body = controller_right_gameobject.transform.parent.Find ("Model").Find ("body").gameObject; // for StaemVR
				}
			} else if (controller_right_gameobject.transform.parent.Find ("Hand") != null) {
				body = controller_right_gameobject.transform.parent.Find ("Hand").gameObject; // for VRTK Simulator
			}
		}
		if (body != null) {
			//low_poly_body.transform.position = body.transform.position;
			//low_poly_body.transform.rotation = body.transform.rotation;
			MeshFilter[] meshes = low_poly_model.GetComponentsInChildren<MeshFilter> ();
			for (int i = 0; i < meshes.Length; i++) {
				bool lowest = false;
				Mesh ms = meshes [i].mesh;
				int vc = ms.vertexCount;
				Debug.Log ("vc = " + vc.ToString ());
				for (int j = 0; j < vc; j++) {
					temp_vector = meshes [i].transform.TransformPoint (ms.vertices [j]);
//					Debug.Log ("temp_vector = " + temp_vector.ToString ());
					if (i == 0 && j == 0) {
						lowest_y = temp_vector.y;
//					if(i==0&&j==0){ bnds = new Bounds(meshes[i].transform.TransformPoint(ms.vertices[j]), Vector3.zero);}
//					else { bnds.Encapsulate (meshes[i].transform.TransformPoint (ms.vertices [j]));}
					}
					if (temp_vector.y < lowest_y) {
						lowest_y = temp_vector.y;
						lowest_y_index = j;
						lowest = true;
					}
				}
				if (lowest) {
					lowest_vertex = meshes [i].transform.TransformPoint (ms.vertices [lowest_y_index]);
					Debug.Log ("lowest_vertex = " + lowest_vertex.ToString ());
				}
			}
		}
	}

	void note_on (MidiChannel channel, int note, float velocity)
	{
		if (channel != MidiChannel.Ch1) {
			Debug.Log ("received midi on channel " + channel + ", ignoring");
			return;
		}
		Debug.Log (note.ToString () + " " + velocity.ToString ());
		if (velocity > 0) {
			sub_note = note % 12;
			octave = note / 12;
			//octave = octave - 3;
			Debug.Log ("octave = " + octave.ToString () + ", note = " + note.ToString());
			if (note < min_key || note > max_key) {
				int init_sub_note = 0;
				int init_octave = 0;
				if (note < min_key) {
					max_key = min_key == -1 ? note : max_key;
					min_key = note;
					Debug.Log ("new min_key = " + min_key.ToString());
				}
				if (note > max_key) {
					min_key = min_key == -1 ? note : min_key;
					max_key = note;
					Debug.Log ("new max_key = " + max_key.ToString());
				}
				for (int i = min_key; i <= max_key; i++) {
					int index = i; // necessary copy of variable for correct generation of delegates
					Debug.Log ("for i = " + min_key.ToString() + "; i <= " + max_key.ToString() + "; i++");
					if (!keys.ContainsKey (i)) {
						init_sub_note = i % 12;
						init_octave = i / 12;
						Vector3 key_pos = new Vector3 ((init_octave * 7 + sub_note_pos [init_sub_note]) * piano_key_width,-piano_key_height/2f,0);
						GameObject key = Instantiate(sub_note_black.Contains (init_sub_note) ? blackKeyPrefab : whiteKeyPrefab) as GameObject;
						key.name = "Key" + i;
						key.transform.SetPositionAndRotation (piano.transform.position, piano.transform.rotation);
						key.transform.parent = piano.transform;
						key.transform.Translate (key_pos);
						GameObject key_rotator = new GameObject ();
						key_rotator.name = key.name + "Rotator";
						key_rotator.transform.SetPositionAndRotation (key.transform.position, key.transform.rotation);
						key_rotator.transform.parent = piano.transform;
						Vector3 rotatorShift = new Vector3 (0, piano_key_height, piano_key_length * 1.5f);
						key_rotator.transform.Translate (rotatorShift);
						key.transform.parent = key_rotator.transform;
						GameObject keyCollider = new GameObject ();
						keyCollider.name = key.name + "Collider";
						keyCollider.tag = "PianoKey";
						BoxCollider newCollider = keyCollider.AddComponent<BoxCollider> () as BoxCollider;
						BoxCollider originalCollider = key.GetComponent<BoxCollider> ();
						newCollider.center = originalCollider.center;
						newCollider.size = originalCollider.size;
						Destroy (originalCollider);
						keyCollider.transform.SetPositionAndRotation (key.transform.position, key.transform.rotation);
						key_rotator.transform.parent = keyCollider.transform;
						keyCollider.transform.parent = piano.transform;

						VRTK.VRTK_InteractableObject key_interact_script = keyCollider.AddComponent<VRTK.VRTK_InteractableObject> () as VRTK.VRTK_InteractableObject;
						BassUnitySoundGenerator soundGenerator = key.AddComponent<BassUnitySoundGenerator>() as BassUnitySoundGenerator;
						soundGenerator.init ();
						key_interact_script.InteractableObjectTouched += (sender, e) => {
							note_press_indicate(index);
							soundGenerator.NoteEvent(index, 100, true);
						};
						key_interact_script.InteractableObjectUntouched += (sender, e) => {
							note_release_indicate (index);
							soundGenerator.NoteEvent(index, 0, false);
						};

						keys [i] = key;
						notes [key] = i;
						colors [i] = key.GetComponentInChildren<Renderer> ().material.color;
						key_rotators [i] = key_rotator;
					}
				}
				if (sidewood_left == null) {
					sidewood_left = Instantiate (sidePanelPrefab) as GameObject;
					sidewood_right = Instantiate (sidePanelPrefab) as GameObject;
					sidewood_left.transform.parent = piano.transform;
					sidewood_right.transform.parent = piano.transform;
					sidewood_right.transform.localScale = new Vector3 (-1, 1, 1);
					pianoBody = Instantiate (pianoBodyPrefab) as GameObject;
					pianoBody.transform.parent = piano.transform;
					pianoBody.transform.rotation = piano.transform.rotation;
				}
				sidewood_left.transform.SetPositionAndRotation (keys [min_key].transform.position, keys [min_key].transform.rotation);
				sidewood_right.transform.SetPositionAndRotation (keys [max_key].transform.position, keys [max_key].transform.rotation);
				pianoBody.transform.position = keys [min_key].transform.position;
				pianoBody.transform.Translate (Vector3.left * piano_key_width/2);
				pianoBody.transform.localScale = new Vector3 ((get_white_key_num(max_key / 12, max_key%12) - get_white_key_num(min_key/12,min_key %12) + 1)*piano_key_width/pianoKeyModelWidth,1,1);
				initialized = true;
			}

			if (calibration_xy.Count > 9) {
				note_press_indicate (note);
				keys[note].GetComponent<BassUnitySoundGenerator>().NoteEvent(note, 100, true);
				return; // stopping calibration for now
				calibration_xy.RemoveAt (0);
				calibration_scale.RemoveAt (0);
			}

			lowest_vertex_update ();

			if (calibration_xz.Count == 0) {
				if (!sub_note_black.Contains (sub_note)) {
					Vector3 direction = controller_direction_plane.transform.up;
					direction.y = 0;
					piano_initial_rotation_wrapper.transform.rotation = Quaternion.LookRotation (direction);
					piano.transform.position = lowest_vertex;
					piano.transform.Translate (new Vector3 (-(octave * 7 + sub_note_pos [sub_note]) * piano_key_width, 0, 0));
					update_virtual_desktop_position ();
				}
			} else {
				if (sub_note_black.Contains (sub_note)) {
					note_press_indicate (note);
					keys[note].GetComponent<BassUnitySoundGenerator>().NoteEvent(note, 100, true);
					return; // do not calibrate with black keys
				}
				GameObject dot = GameObject.CreatePrimitive (PrimitiveType.Sphere);
				Plane plane = new Plane (piano_xy_plane.transform.forward, piano_xy_plane.transform.position);
				dot.transform.position = plane.ClosestPointOnPlane (lowest_vertex);
				dot.transform.localScale = new Vector3 (0.005f, 0.005f, 0.005f);
				dot.GetComponent<Renderer> ().material.color = Color.red;
				calibration_xy.Add (lowest_vertex);
				calibration_scale.Add (new key_pos_sample (octave, sub_note, lowest_vertex));
				if (calibration_xy.Count > 1) {
					float[,] calibration_X_array = new float [calibration_xy.Count, 2];
					float[] calibration_y_array = new float [calibration_xy.Count];
					for (int i = 0; i < calibration_xy.Count; i++) {
						calibration_X_array [i, 0] = 1;
						calibration_X_array [i, 1] = piano_initial_rotation_wrapper.transform.InverseTransformPoint (calibration_xy [i]).x;
						calibration_y_array [i] = piano_initial_rotation_wrapper.transform.InverseTransformPoint (calibration_xy [i]).y;
					}
					Matrix<float> X = Matrix<float>.Build.DenseOfArray (calibration_X_array);
					Vector<float> y = Vector<float>.Build.DenseOfArray (calibration_y_array);
					Vector<float> s = X.Solve (y);
					Debug.Log ("Z-Angle Solved: " + s.ToString ());
					float z_angle = Mathf.Rad2Deg * Mathf.Atan (s [1]);
					Debug.Log ("z_angle = " + z_angle.ToString ());
					Vector3 current_rotation = piano.transform.localRotation.eulerAngles;
					piano.transform.localRotation = Quaternion.Euler (current_rotation.x, current_rotation.y, z_angle);


					Vector3 new_position = piano.transform.localPosition;
					new_position.y = s [0] + new_position.x * s [1];
					float y_shift = piano.transform.localPosition.y - new_position.y;
					Debug.Log ("y_shift = " + y_shift);
					piano.transform.localPosition = new_position;
//					float start_note = 0;
//					float start_x = 0;
//					int data_points = 0;
//					for (int i = 0; i < calibration_scale.Count; i++) {
//						if (i == 0) {
//							start_note = calibration_scale [i].octave * 7 + sub_note_pos [calibration_scale [i].sub_note];
//							start_x = piano.transform.InverseTransformPoint (calibration_scale [i].pos).x;
//						}
//						if (calibration_scale [i].octave * 7 + sub_note_pos [calibration_scale [i].sub_note] != start_note) {
//							piano_key_size += (piano.transform.InverseTransformPoint (calibration_scale [i].pos).x - start_x) / (calibration_scale [i].octave * 7 + sub_note_pos [calibration_scale [i].sub_note] - start_note);
//							data_points++;
//						}
//					}

					Debug.Log ("piano.transform.localPosition = " + piano.transform.localPosition);
					piano.transform.localScale = Vector3.one;
					float[,] calibration2_X_array = new float [calibration_scale.Count, 2];
					float[] calibration2_y_array = new float [calibration_scale.Count];
					for (int i = 0; i < calibration_scale.Count; i++) {
						calibration2_X_array [i, 0] = 1;
						calibration2_X_array [i, 1] = get_white_key_num (calibration_scale [i].octave, calibration_scale [i].sub_note);
						calibration2_y_array [i] = piano.transform.InverseTransformPoint (calibration_scale [i].pos).x;
						Debug.Log ("key world vector = " + calibration_scale [i].pos.ToString ());
						Debug.Log ("key white num = " + calibration2_X_array [i, 1].ToString () + "");
						Debug.Log ("key local vector = " + piano.transform.InverseTransformPoint (calibration_scale [i].pos).ToString ());
					}
					Matrix<float> X2 = Matrix<float>.Build.DenseOfArray (calibration2_X_array);
					Vector<float> y2 = Vector<float>.Build.DenseOfArray (calibration2_y_array);
					Vector<float> s2 = X2.Solve (y2);
					Debug.Log ("Note offset & scale Solved: " + s2.ToString ());
					float x_offset = s2 [0];
					Debug.Log ("x_offset = " + x_offset);
					Debug.Log ("shifting piano local x by " + x_offset);
					piano.transform.Translate (new Vector3 (x_offset, 0, 0));
					piano_key_width = s2 [1];
					Debug.Log ("new piano_key_size = " + piano_key_width);
					Debug.Log ("new scale = " + piano_key_width / default_piano_key_width);
					piano.transform.localScale = new Vector3 (piano_key_width / default_piano_key_width, 1, 1);
					update_virtual_desktop_position ();
				} else {
					// this section does note work well, needs fixing
					Plane plane1 = new Plane (piano_xy_plane.transform.forward, piano_xy_plane.transform.position);
					piano.transform.position = plane1.ClosestPointOnPlane (lowest_vertex);
					piano.transform.Translate (new Vector3 (-get_white_key_num (octave, sub_note) * piano_key_width, 0, 0));
					update_virtual_desktop_position ();
				}
			}
			note_press_indicate (note);
			keys[note].GetComponent<BassUnitySoundGenerator>().NoteEvent(note, 100, true);
		} else {
			if (keys.ContainsKey(note)) {
				note_release_indicate (note);
				keys[note].GetComponent<BassUnitySoundGenerator>().NoteEvent(note, 0, false);
			}
		}
	}

	void note_press_indicate(int note) {
		Debug.Log ("indicating press of note " + note);
		keys [note].GetComponentInChildren<Renderer> ().material.color = Color.green;
		//keys [i].transform.RotateAround (key_rotators [i].transform.position, Vector3.right, key_press_rotation_x);
		key_rotators [note].transform.localRotation = Quaternion.Euler (Vector3.right * key_press_rotation_x);

		particle_system.transform.position = keys [note].transform.position;
		particle_system.GetComponent<ParticleSystem> ().Emit (1);
	}

	void note_release_indicate(int note) {
		Debug.Log ("indicating release of note " + note);
		if (!keys.ContainsKey (note)) {
			Debug.LogWarning ("some error here, note " + note + " is not in the list, but got here");
		}
		keys [note].GetComponentInChildren<Renderer> ().material.color = colors [note];
		key_rotators[note].transform.localRotation = Quaternion.Euler (Vector3.zero);
		//keys [i].transform.RotateAround (key_rotators [i].transform.position, Vector3.right, -key_press_rotation_x);
	}
}
