using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class DonutGravityReceiver : MonoBehaviour
{

	public DonutGravityEmitter gravityEmitter;

	void Update ()
	{
		Rigidbody body = GetComponent<Rigidbody> ();
		Vector3 relativePosition = gravityEmitter.gameObject.transform.InverseTransformPoint (transform.position);
		Vector3 planePosition = gravityEmitter.gameObject.transform.TransformPoint (new Vector3 (relativePosition.x, 0, relativePosition.z));
		Vector3 gravityPoint = gravityEmitter.transform.position + (planePosition - gravityEmitter.transform.position).normalized * gravityEmitter.radius;
		Vector3 gravityVector = gravityPoint - transform.position;
		Debug.DrawRay (transform.position, gravityVector);
		Vector3 forceVector;
		if (gravityEmitter.GravityCenterOnCutoffSurface) {
			forceVector = gravityVector.normalized * Mathf.Min (gravityVector.magnitude - gravityEmitter.cutoffRadius, gravityEmitter.gravity);
		} else {
			forceVector = gravityVector.normalized * Mathf.Min (gravityVector.magnitude, gravityEmitter.gravity);
		}
		if (gravityVector.magnitude < gravityEmitter.cutoffRadius) {
			forceVector *= gravityEmitter.cutoffGravityRatio;
			if (body.velocity.magnitude > gravityEmitter.dragCutoffVelocity && gravityEmitter.cutoffDrag != 0) {
				body.AddForce (-body.velocity * (gravityEmitter.cutoffDragIsQuadratic ? body.velocity.sqrMagnitude : body.velocity.magnitude) * gravityEmitter.cutoffDrag);
				Debug.DrawRay (transform.position, -body.velocity * gravityEmitter.cutoffDrag, Color.yellow);
			}
		}
		body.AddForce (forceVector);
		Debug.DrawRay (transform.position, forceVector, Color.red);
		Debug.DrawRay (transform.position, body.velocity, Color.green);
	}
}
