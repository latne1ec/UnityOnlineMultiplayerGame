using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour {
	
	void Update () {
		// Point the Health bar and name towards the camera
		transform.LookAt (Camera.main.transform);
	}
}
