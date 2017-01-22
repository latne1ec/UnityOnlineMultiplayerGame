using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using SocketIO;

public class NetworkManager : MonoBehaviour {

	public static NetworkManager instance;
	public Canvas canvas;
	public SocketIOComponent socket;
	public InputField playerNameInput;
	public GameObject player;

	void Awake () {
		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			print ("why?");
			Destroy (gameObject);
		}

		DontDestroyOnLoad (gameObject);
	}

	void Start () {
		// Subscribe to all web socket (emit) events
		socket.On("enemies", OnEnemies);
		socket.On("other player connected", OnOtherPlayerConnected);
		socket.On("play", OnPlay);
		socket.On("player move", OnPlayerMove);
		socket.On("player rotate", OnPlayerRotate);
		socket.On("player shoot", OnPlayerShoot);
		socket.On("health", OnHealth);
		socket.On("other player disconnected", OnOtherPlayerDisconnect);
	}

	public void JoinGame () {
		StartCoroutine (ConnectToServer ());
	}

	#region Commands

	IEnumerator ConnectToServer() {
		yield return new WaitForSeconds (0.5f);
		// Adds client to game
		socket.Emit ("player connect");
		yield return new WaitForSeconds (1.0f);

		string playerName = playerNameInput.text;
		List<SpawnPoint> playerSpawnPoints = GetComponent<PlayerSpawner> ().playerSpawnPoints;
		List<SpawnPoint> enemySpawnPoints = GetComponent<EnemySpawner> ().enemySpawnPoints;
		PlayerJSON playerJSON = new PlayerJSON (playerName, playerSpawnPoints, enemySpawnPoints);
		string data = JsonUtility.ToJson (playerJSON);
		socket.Emit ("play", new JSONObject(data));
		canvas.gameObject.SetActive (false);
	}

	// This is what we send to the server to broadcast our position to everyone else
	public void CommandMove (Vector3 vec3) {
		string data = JsonUtility.ToJson (new PositionJSON (vec3));
		socket.Emit ("player move", new JSONObject(data));

	}

	public void CommandRotate (Quaternion quat) {

		string data = JsonUtility.ToJson (new RotationJSON (quat));
		socket.Emit ("player rotate", new JSONObject(data));

	}

	public void CommandShoot () {

		socket.Emit ("player shoot");

	}

	public void CommandHealthChange(GameObject playerFrom, GameObject playerTo, int healthChange, bool isEnemy) {

		HealthChangeJSON healthChangeJSON = new HealthChangeJSON (playerTo.name, healthChange, playerFrom.name, isEnemy);
		socket.Emit("health", new JSONObject(JsonUtility.ToJson(healthChangeJSON)));
					
	}


	#endregion 

	#region Listeners AKA Handlers

	void OnEnemies(SocketIOEvent socketIOEvent) {

		string data = socketIOEvent.data.ToString ();
		// Handy method to create class from string
		EnemiesJSON enemiesJSON = EnemiesJSON.CreateFromJSON (data);
		EnemySpawner es = GetComponent<EnemySpawner> ();
		es.SpawnEnemies (enemiesJSON);

	}


	void OnOtherPlayerConnected(SocketIOEvent socketIOEvent) {
		string data = socketIOEvent.data.ToString ();
		UserJSON userJSON = UserJSON.CreateFromJSON (data);
		Vector3 position = new Vector3 (userJSON.position[0], userJSON.position [1], userJSON.position [2]);
		Quaternion rotation = Quaternion.Euler(userJSON.rotation[0], userJSON.rotation[1], userJSON.rotation[2]);
		GameObject o = GameObject.Find (userJSON.name) as GameObject;
		if (o != null) {
			return;
		} 

		// Create New Player
		GameObject p = Instantiate (player, position, rotation) as GameObject;
		PlayerController pc = p.GetComponent<PlayerController> ();
		Transform t = p.transform.Find ("HealthBarCanvas");
		Transform t1 = t.transform.Find ("Player Name");
		Text playerName = t1.GetComponent<Text> ();
		playerName.text = userJSON.name;
		pc.isLocalPlayer = false;
		p.name = userJSON.name;
		Health health = p.GetComponent<Health> ();
		health.currentHealth = userJSON.health;
		health.OnChangeHealth ();
	}

	void OnPlay(SocketIOEvent socketIOEvent) {
		string data = socketIOEvent.data.ToString ();
		UserJSON currentUserJSON = UserJSON.CreateFromJSON (data);
		Vector3 position = new Vector3 (currentUserJSON.position[0], currentUserJSON.position [1], currentUserJSON.position [2]);
		Quaternion rotation = Quaternion.Euler(currentUserJSON.rotation[0], currentUserJSON.rotation[1], currentUserJSON.rotation[2]);
		GameObject p = Instantiate (player, position, rotation) as GameObject;
		PlayerController pc = p.GetComponent<PlayerController> ();
		Transform t = p.transform.Find ("HealthBarCanvas");
		Transform t1 = t.transform.Find ("Player Name");
		Text playerName = t1.GetComponent<Text> ();
		playerName.text = currentUserJSON.name;
		pc.isLocalPlayer = true;
		p.name = currentUserJSON.name;
	}

	void OnPlayerMove(SocketIOEvent socketIOEvent) {

		string data = socketIOEvent.data.ToString ();
		UserJSON userJSON = UserJSON.CreateFromJSON (data);
		Vector3 position = new Vector3 (userJSON.position[0], userJSON.position [1], userJSON.position [2]);
		// If it's the current player, exit
		if(userJSON.name == playerNameInput.text) {
			return;
		}

		GameObject p = GameObject.Find(userJSON.name) as GameObject;
		if(p != null) {
			p.transform.position = position;
		}

	}
	void OnPlayerRotate(SocketIOEvent socketIOEvent) {
		string data = socketIOEvent.data.ToString ();
		UserJSON userJSON = UserJSON.CreateFromJSON (data);
		Quaternion rotation = Quaternion.Euler(userJSON.rotation[0], userJSON.rotation[1], userJSON.rotation[2]);
		// If it's the current player, exit
		if(userJSON.name == playerNameInput.text) {
			return;
		}

		GameObject p = GameObject.Find(userJSON.name) as GameObject;
		if(p != null) {
			p.transform.rotation = rotation;
		}
			
	}
	void OnPlayerShoot(SocketIOEvent socketIOEvent) {

		string data = socketIOEvent.data.ToString ();
		ShootJSON shootJSON = ShootJSON.CreateFromJSON (data);
		// Find game object 
		GameObject p = GameObject.Find(shootJSON.name);
		// Instantiate the bullet from player script
		PlayerController pc = p.GetComponent<PlayerController>();
		pc.CmdFire ();

	}
	void OnHealth(SocketIOEvent socketIOEvent) {

		string data = socketIOEvent.data.ToString ();
		UserHealthJSON userHealthJSON = UserHealthJSON.CreateFromJSON (data);
		GameObject p = GameObject.Find(userHealthJSON.name);
		Health h = p.GetComponent<Health> ();
		h.currentHealth = userHealthJSON.health;
		h.OnChangeHealth ();
	
	}

	void OnOtherPlayerDisconnect(SocketIOEvent socketIOEvent) {

		print ("user disconnected");
		string data = socketIOEvent.data.ToString ();
		UserJSON userJSON = UserJSON.CreateFromJSON (data);
		Destroy (GameObject.Find (userJSON.name));
	}


	#endregion


	/// ALL MESSAGES WE ARE SENDING AND RECEIVING FROM SERVER
	#region JSONMessageClasses

	[Serializable]
	public class PlayerJSON {
		public string name;
		public List<PointJSON> playerSpawnPoints;
		public List<PointJSON> enemySpawnPoints;

		public PlayerJSON(string _name, List<SpawnPoint> _playerSpawnPoints, List<SpawnPoint> _enemySpawnPoints) {
			playerSpawnPoints = new List<PointJSON>();
			enemySpawnPoints = new List<PointJSON>();
			name = _name;
			foreach (SpawnPoint playerSpawnPoint in _playerSpawnPoints) {
				PointJSON pointJSON = new PointJSON(playerSpawnPoint);
				playerSpawnPoints.Add(pointJSON);
			}

			foreach (SpawnPoint enemySpawnPoint in _enemySpawnPoints) {
				PointJSON pointJSON = new PointJSON(enemySpawnPoint);
				enemySpawnPoints.Add(pointJSON);
			}
		}
	}

	[Serializable]

	public class PointJSON {
		
		public float[] position;
		public float[] rotation;
		public PointJSON(SpawnPoint spawnPoint) {
			position = new float[] {
				spawnPoint.transform.position.x,
				spawnPoint.transform.position.y,
				spawnPoint.transform.position.z
			};
			rotation = new float[] {
				spawnPoint.transform.eulerAngles.x,
				spawnPoint.transform.eulerAngles.y,
				spawnPoint.transform.eulerAngles.z
			};
		}
	}

	[Serializable]
	public class PositionJSON {
		public float[] position;

		public PositionJSON(Vector3 _position) {
			position = new float[] {
				_position.x, _position.y, _position.z
			};
		}
	}

	[Serializable]
	public class RotationJSON {
		public float[] rotation;

		public RotationJSON(Quaternion _rotation) {
			rotation = new float[] {
				_rotation.eulerAngles.x, _rotation.eulerAngles.y, _rotation.eulerAngles.z
			};
		}
	}

	[Serializable]
	public class UserJSON {
		public string name;
		public float[] position;
		public float[] rotation;
		public int health;

		public static UserJSON CreateFromJSON(string data) {
			return JsonUtility.FromJson<UserJSON> (data);
		}
	}

	[Serializable]
	public class HealthChangeJSON {
		public string name;
		public int healthChange;
		public string from;
		public bool isEnemy;

		public HealthChangeJSON(string _name, int _healthChange, string _from, bool _isEnemy) {
			name = _name;
			healthChange = _healthChange;
			from = _from;
			isEnemy = _isEnemy;
		}
	}

	[Serializable]
	// FROM THE SERVER - State of all enemies in game
	public class EnemiesJSON {
		
		public List<UserJSON> enemies;
		public static EnemiesJSON CreateFromJSON(string data) {
			return JsonUtility.FromJson<EnemiesJSON> (data);
		}

	}

	[Serializable]
	public class ShootJSON {
		public string name;

		public static ShootJSON CreateFromJSON(string data) {
			return JsonUtility.FromJson<ShootJSON> (data);
		}

	}


	[Serializable]
	public class UserHealthJSON {
		public string name;
		public int health;

		public static UserHealthJSON CreateFromJSON(string data) {
			return JsonUtility.FromJson<UserHealthJSON> (data);
		}
	}

	#endregion
}
