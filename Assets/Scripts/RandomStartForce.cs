using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RandomStartForce : MonoBehaviour {

	public float min = 0;
	public float max = 100;

	void Start () {
		GetComponent<Rigidbody>().AddForce(
			Vector3.ClampMagnitude(
				new Vector3(
					Random.Range(min,max)*(Random.value-0.5f),
					Random.Range(min,max)*(Random.value-0.5f),
					Random.Range(min,max)*(Random.value-0.5f)
				), max)
		);
	}		
}
