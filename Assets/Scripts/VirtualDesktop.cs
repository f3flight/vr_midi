using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRTK;

public class VirtualDesktop : MonoBehaviour {

	float lastTime = -1;
	Renderer r;
	UnityEngine.Texture2D t;
	int screenWidth;
	int screenHeight;
	System.Drawing.Size s;
	System.Drawing.Bitmap b;
	System.Drawing.Graphics g;
	System.Threading.Thread displayCaptureThread;
	bool displayCaptureThreadIsRunning = true;
	byte[] displayByteBuffer;
	bool displayByteBufferInitialized = false;
	bool displayByteBufferUpdated = false;
	int displayByteBufferBytes;
	System.Drawing.Rectangle displayRect = new System.Drawing.Rectangle(0,0,0,0);

	public GameObject virtualDesktopPlane;
	private GameObject interactingObject;

	private uint mouseX, mouseY;
	private bool mouseControlled;
	private bool mouseIsDown;
	private bool mouseIsIn;

	[DllImport("user32.dll", EntryPoint = "SetCursorPos")]
	private static extern bool SetCursorPos (int x, int y);

	[DllImport("user32.dll")]
	private static extern void mouse_event (uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

	private const uint MOUSE_LEFT_DOWN = 0x02;
	private const uint MOUSE_LEFT_UP = 0x04;
	private const uint MOUSE_RIGHT_DOWN = 0x08;
	private const uint MOUSE_RIGHT_UP = 0x10;
	private const uint MOUSE_MOVE = 0x01;

	void SubscribeEvents(object sender, InteractableObjectEventArgs e) {
		if (e.interactingObject.GetComponent<VRTK_DestinationMarker> () != null) {
			mouseIsIn = true;
			interactingObject = e.interactingObject;
			if (!mouseControlled) {
				Debug.Log ("+Move +Up");
				interactingObject.GetComponent<VRTK_ControllerEvents> ().TriggerReleased += MouseUp;
				mouseControlled = true;
			}
			Debug.Log ("+Move +Out +Down");
			interactingObject.GetComponent<VRTK_DestinationMarker> ().DestinationMarkerHover += MouseMove;
			interactingObject.GetComponent<VRTK_DestinationMarker> ().DestinationMarkerExit += MouseOut;
			interactingObject.GetComponent<VRTK_ControllerEvents> ().TriggerPressed += MouseDown;
		}
	}

	public void MouseMove(object sender, DestinationMarkerEventArgs e) {
		//Debug.Log ("mousemove Sender = <<<" + sender.ToString () + ">>>");
		//Debug.Log ("mousemove e.target.gameobject.name = <<<" + e.target.gameObject.name + ">>>");
		mouseX = (uint)Mathf.Max(0, Mathf.Min(screenWidth, Mathf.RoundToInt (e.raycastHit.textureCoord.x * Screen.currentResolution.width)));
		mouseY = (uint)Mathf.Max(0, Mathf.Min(screenHeight, Mathf.RoundToInt ((1 - e.raycastHit.textureCoord.y) * Screen.currentResolution.height)));
		SetCursorPos ((int)mouseX, (int)mouseY);
	}

	public void MouseOut(object sender, DestinationMarkerEventArgs e) {
		if (interactingObject != null) { // not sure why MouseOut gets called 2 times... might be a bug in my code somewhere else.
			interactingObject.GetComponent<VRTK_DestinationMarker> ().DestinationMarkerHover -= MouseMove;
			interactingObject.GetComponent<VRTK_DestinationMarker> ().DestinationMarkerExit -= MouseOut;
			interactingObject.GetComponent<VRTK_ControllerEvents> ().TriggerPressed -= MouseDown;
			Debug.Log ("-Move -Out -Down");
			if (!mouseIsDown) {
				interactingObject.GetComponent<VRTK_ControllerEvents> ().TriggerReleased -= MouseUp;
				Debug.Log ("OUT: -Up");
				interactingObject = null;
				mouseControlled = false;
			}
			mouseIsIn = false;
		}
	}

	public void MouseDown(object sender, ControllerInteractionEventArgs e) {
		mouseIsDown = true;
		mouse_event (MOUSE_LEFT_DOWN, mouseX, mouseY, 0, 0);
	}

	public void MouseUp(object sender, ControllerInteractionEventArgs e) {
		mouse_event (MOUSE_LEFT_UP, mouseX, mouseY, 0, 0);
		if (!mouseIsIn) {
			interactingObject.GetComponent<VRTK_ControllerEvents> ().TriggerReleased -= MouseUp;
			Debug.Log ("UP: -Up");
			interactingObject = null;
			mouseControlled = false;
		}
		mouseIsDown = false;
	}

	// Use this for initialization
	void Start () {
		screenWidth = Screen.currentResolution.width;
		screenHeight = Screen.currentResolution.height;
		t = new UnityEngine.Texture2D (screenWidth, screenHeight, TextureFormat.BGRA32, false);
		r = GetComponent<Renderer> ();
		r.material.mainTexture = t;
		r.material.mainTextureOffset = Vector2.up;
		r.material.mainTexture.wrapMode = TextureWrapMode.Mirror;
		//r.material.EnableKeyword("_EMISSION");
		//r.material.SetTexture ("_EmissionMap", t);
		t.anisoLevel = 0;
		//t.filterMode = FilterMode.Point;
		s = new System.Drawing.Size (screenWidth, screenHeight);
		b = new System.Drawing.Bitmap (screenWidth, screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		g = System.Drawing.Graphics.FromImage (b);
		displayRect.Width = screenWidth;
		displayRect.Height = screenHeight;
		displayCaptureThread = new System.Threading.Thread (UpdateScreenCaptureBuffers);
		displayCaptureThread.Start ();
		Debug.Log ("thread started");

		if (GetComponent<VRTK_InteractableObject>() == null)
		{
			VRTK_Logger.Error(VRTK_Logger.GetCommonMessage(VRTK_Logger.CommonMessageKeys.REQUIRED_COMPONENT_MISSING_FROM_GAMEOBJECT, "VirtualDesktop", "VRTK_InteractableObject", "---"));
			return;
		}
		GetComponent<VRTK_InteractableObject> ().InteractableObjectTouched += new InteractableObjectEventHandler(SubscribeEvents);
		Debug.Log ("Subscribed?");
	}

	void OnApplicationQuit() {
		displayCaptureThreadIsRunning = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (displayByteBufferUpdated) {
			t.LoadRawTextureData (displayByteBuffer);
			t.Apply ();
			displayByteBufferUpdated = false;
			//Debug.Log ("apply buffer");
		}
	}

	void UpdateScreenCaptureBuffers() {
		Debug.Log ("inside thread");
		while (displayCaptureThreadIsRunning) {
				g.CopyFromScreen (0, 0, 0, 0, s);
				//Debug.Log ("screen copied");
				// Lock the bitmap's bits. 
				System.Drawing.Imaging.BitmapData bmpData = b.LockBits (displayRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, b.PixelFormat);
				displayByteBufferBytes = bmpData.Stride * b.Height;
				if (!displayByteBufferInitialized) {
					Debug.Log ("init buffer");
					displayByteBuffer = new byte[displayByteBufferBytes];
					displayByteBufferInitialized = true;
				}
				if (!displayByteBufferUpdated) {
					//Debug.Log ("fill buffer");
					System.Runtime.InteropServices.Marshal.Copy (bmpData.Scan0, displayByteBuffer, 0, displayByteBufferBytes);
					displayByteBufferUpdated = true;
				}
				b.UnlockBits (bmpData);
		}
		Debug.Log ("exited thread");
	}
}
