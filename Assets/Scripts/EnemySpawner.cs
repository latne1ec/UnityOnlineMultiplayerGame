﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour {

	public GameObject enemy;
	public GameObject spawnPoint;
	public int numberOfEnemies;
	[HideInInspector] 
	public List<SpawnPoint> enemySpawnPoints;

	void Start () {
		// Set the Random Spawn Points
		for (int i = 0; i < numberOfEnemies; i++) {
			var spawnPosition = new Vector3 (Random.Range (-8f, 8f), 0f, Random.Range (-8f, 8f));
			var spawnRotation = Quaternion.Euler(0f, Random.Range (0,180), 0f);
			SpawnPoint enemySpawnPoint = (Instantiate(spawnPoint, spawnPosition, spawnRotation) as GameObject).GetComponent<SpawnPoint>();

			enemySpawnPoints.Add (enemySpawnPoint);
		}
		//SpawnEnemies ();
	}

	public void SpawnEnemies(NetworkManager.EnemiesJSON enemiesJSON) {

		foreach (NetworkManager.UserJSON enemyJSON in enemiesJSON.enemies) {

			if (enemyJSON.health <= 0) {
				continue;
			}

			Vector3 position = new Vector3 (enemyJSON.position [0], enemyJSON.position [1], enemyJSON.position [2]);
			Quaternion rotation = Quaternion.Euler (enemyJSON.rotation [0], enemyJSON.rotation [1], enemyJSON.rotation [2]);
			GameObject newEnemy = Instantiate (enemy, position, rotation) as GameObject;
			newEnemy.name = enemyJSON.name;
			PlayerController pc = newEnemy.GetComponent<PlayerController> ();
			pc.isLocalPlayer = false;
			Health health = newEnemy.GetComponent<Health> ();
			health.currentHealth = enemyJSON.health;
			health.OnChangeHealth ();
			health.isEnemy = true;
			health.destroyOnDeath = true;
		}
	}
}
