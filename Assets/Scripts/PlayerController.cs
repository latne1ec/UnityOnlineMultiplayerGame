using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public GameObject bulletPrefab;
	public Transform bulletSpawn;
	public bool isLocalPlayer = false;
	Vector3 oldPosition;
	Vector3 currentPosition;
	Quaternion oldRotation;
	Quaternion currentRotation;

	void Start () {
		oldPosition = transform.position;
		currentPosition = oldPosition;
		oldRotation = transform.rotation;
		currentRotation = oldRotation;
	}
	
	void Update () {

		if (!isLocalPlayer) {
			return;
		}

		var x = Input.GetAxis ("Horizontal") * Time.deltaTime * 150.0f;
		var z = Input.GetAxis ("Vertical") * Time.deltaTime * 15.0f;

		transform.Rotate (0, x, 0);
		transform.Translate (0, 0, z);

		currentPosition = transform.position;
		currentRotation = transform.rotation;

		if (currentPosition != oldPosition) {
			// If position is not the same, send new position to Server
			NetworkManager.instance.GetComponent<NetworkManager>().CommandMove(transform.position);
			oldPosition = currentPosition;
		}

		if (currentRotation != oldRotation) {
			// If rotation is not the same, send new rotation to Server
			NetworkManager.instance.GetComponent<NetworkManager>().CommandRotate(transform.rotation);
			oldRotation = currentRotation;
		}

		if(Input.GetKeyDown(KeyCode.Space)) {
			// Player is shooting, send data to Server
			NetworkManager.instance.GetComponent<NetworkManager>().CommandShoot();
		}
	}

	public void CmdFire() {
		var bullet = Instantiate (bulletPrefab, bulletSpawn.position, bulletSpawn.rotation) as GameObject;
		Bullet b = bullet.GetComponent<Bullet> ();
		b.playerFrom = this.gameObject;
		bullet.GetComponent<Rigidbody> ().velocity = bullet.transform.up * 6;

		Destroy(bullet, 2.0f);
	}
}
