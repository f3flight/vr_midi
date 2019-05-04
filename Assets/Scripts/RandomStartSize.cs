using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomStartSize : MonoBehaviour {

	public float minRatio = 1;
	public float maxRatio = 1;

	// Use this for initialization
	void Start () {
		transform.localScale = transform.localScale * Random.Range (minRatio, maxRatio);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
