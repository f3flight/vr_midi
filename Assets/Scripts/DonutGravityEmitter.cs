using UnityEngine;

public class DonutGravityEmitter : MonoBehaviour {

	public float radius;
	public float donutRadius;
	public float cutoffRadius;
	public float cutoffGravityRatio;
	public float cutoffDrag;
	public bool cutoffDragIsQuadratic;
	public float dragCutoffVelocity;
	public bool GravityCenterOnCutoffSurface;

	[HideInInspector]
	public float gravity;

	void DrawDonut(float mainRadius, float tubeRadius, Color color) {
		UnityEditor.Handles.color = color;
		UnityEditor.Handles.DrawWireDisc (transform.position, transform.rotation * Vector3.up, mainRadius-tubeRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position, transform.rotation * Vector3.up, mainRadius+tubeRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position + Vector3.up*tubeRadius, transform.rotation * Vector3.up, mainRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position - Vector3.up*tubeRadius, transform.rotation * Vector3.up, mainRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position + Vector3.right*mainRadius, transform.rotation * Vector3.forward, tubeRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position - Vector3.right*mainRadius, transform.rotation * Vector3.forward, tubeRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position + Vector3.forward*mainRadius, transform.rotation * Vector3.right, tubeRadius);
		UnityEditor.Handles.DrawWireDisc (transform.position - Vector3.forward*mainRadius, transform.rotation * Vector3.right, tubeRadius);
	}

	void OnDrawGizmos() {
		DrawDonut (radius, donutRadius, Color.cyan);
		if (GravityCenterOnCutoffSurface) {
			DrawDonut (radius, cutoffRadius, Color.blue);
			//UnityEditor.Handles.color = Color.black;
			//UnityEditor.Handles.DrawWireDisc (transform.position, transform.rotation * Vector3.up, radius);
		} else {
			DrawDonut (radius, cutoffRadius, Color.magenta);
			UnityEditor.Handles.color = Color.blue;
			UnityEditor.Handles.DrawWireDisc (transform.position, transform.rotation * Vector3.up, radius);
		}
	}

	void Start () {
		gravity = donutRadius;
	}
}
