using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class directions_helper : MonoBehaviour
{

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void OnDrawGizmos ()
	{
		Color color;
		color = Color.green;
		// local up
		DrawHelperAtCenter (this.transform.up, color, 0.02f);
         
//		color.g -= 0.5f;
		// global up
//		DrawHelperAtCenter (Vector3.up, color, 1f);
         
		color = Color.blue;
		// local forward
		DrawHelperAtCenter (this.transform.forward, color, 0.02f);
         
//		color.b -= 0.5f;
		// global forward
//		DrawHelperAtCenter (Vector3.forward, color, 1f);
         
		color = Color.red;
		// local right
		DrawHelperAtCenter (this.transform.right, color, 0.02f);
         
//		color.r -= 0.5f;
		// global right
//		DrawHelperAtCenter (Vector3.right, color, 1f);
	}

	private void DrawHelperAtCenter (Vector3 direction, Color color, float scale)
	{
		Gizmos.color = color;
		Vector3 destination = transform.position + direction * scale;
		Gizmos.DrawLine (transform.position, destination);
	}
}
