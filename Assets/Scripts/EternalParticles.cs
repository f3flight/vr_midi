using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class EternalParticles : MonoBehaviour
{
    ParticleSystem ps;
    ParticleSystem.Particle[] psp;
    int particleNum;
    public float gravity = 1;
    public float particleSpeedLimit = 0.01f;
    public float gravityCutoffRatio = 0.25f;
    float radius;
    float gravityCutoffDistance;
    Vector3 normal;
    Plane p;

    void Mark(Vector3 pos, string text) {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.GetComponent<Renderer>().material.color = Color.red;
        dot.transform.localScale = Vector3.one * 0.01f;
        dot.transform.position = pos;
        dot.name = "Mark" + text;
    }

    void Start() {
		ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (psp == null)
        {
            psp = new ParticleSystem.Particle[ps.main.maxParticles];
            particleNum = ps.GetParticles(psp);
            Debug.Log("num particles = " + particleNum);
            for (int i = 0; i < particleNum; i++) {
                psp[i].startLifetime = 100000;
                psp[i].remainingLifetime = 100000;
                psp[i].velocity = Vector3.ClampMagnitude(new Vector3(Random.value*particleSpeedLimit/3,
                                                                     Random.value*particleSpeedLimit/3,
                                                                     Random.value*particleSpeedLimit/3),
                                                         particleSpeedLimit);
            }
        } else {
            ps.GetParticles(psp);    
        }

		normal = ps.transform.TransformDirection(Quaternion.Euler(ps.shape.rotation) * Vector3.forward);
		p = new Plane(normal, ps.transform.position);
		radius = ps.shape.donutRadius;
		gravityCutoffDistance = radius * gravityCutoffRatio;

        //Debug.Log((particleNum + 1) + " particles alive");
        //Debug.Log(normal);
        //Debug.DrawLine(ps.transform.position, ps.transform.position + normal * 2, Color.red, 600, false);
        Vector3 applied_force;
        for (int i = 0; i < particleNum; i++)
        {
            Vector3 po = p.ClosestPointOnPlane(psp[i].position);
            Vector3 ve = psp[i].position + po;
            Vector3 closest_grav_point = Vector3.Normalize(ps.transform.position + ve) * ps.shape.radius;
            Vector3 grav_vector = closest_grav_point - psp[i].position;
            if (grav_vector.magnitude > gravityCutoffDistance) {
                applied_force = Vector3.ClampMagnitude(grav_vector, grav_vector.magnitude - gravityCutoffDistance) * Time.deltaTime;
            } else {
                applied_force = Vector3.zero;
            }
			Debug.DrawLine(psp[i].position, psp[i].position + applied_force, Color.red, 0, false);
            Debug.DrawLine(ps.transform.position, closest_grav_point, Color.green, 0, true);
			Debug.DrawLine(psp[i].position, po, Color.yellow, 0, false);
            //Debug.DrawRay(po,normal,Color.yellow,600);
            psp[i].velocity += applied_force;
            psp[i].velocity = Vector3.ClampMagnitude(psp[i].velocity, particleSpeedLimit);
            //Debug.Log("velocity = " + psp[i].velocity);
            //Debug.Log("particle x y z = " + psp[i].position.x + " " + psp[i].position.y + " " + psp[i].position.z);
            //Debug.Log("point x y z = " + po.x + " " + po.y + " " + po.z);
            //psp[i].velocity += 
        }
        ps.SetParticles(psp, particleNum);
        //enabled = false;
        //float eternalRatio = 0;
        //for (int i = 0; i < particleNum; i++)
        //{
        //    Debug.Log("original lifetime values = " + psp[i].remainingLifetime + ", " + psp[i].startLifetime);
        //    eternalRatio = 99999 / psp[i].remainingLifetime;
        //    psp[i].remainingLifetime *= eternalRatio;
        //    psp[i].startLifetime *= eternalRatio;
        //    Debug.Log("new lifetime values = " + psp[i].remainingLifetime + ", " + psp[i].startLifetime);
        //}
        //if (particleNum > 0)
        //{
        //    ps.SetParticles(psp, particleNum);
        //    enabled = false;
        //}
        //enabled = false;
	}
}
