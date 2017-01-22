var express = require("express");
var app = express();
var server = require("http").createServer(app);
var io = require("socket.io").listen(server);

var socket_list = {};
var PLAYER_LIST = {};
var player_number = 0;

server.listen(2000);
console.log("It's lit");

// GLOBAL VARIABLES
var enemies = [];
var playerSpawnPoints = [];
var clients = [];

app.get('/', function (req, res) {
	res.sendFile(__dirname + "/index.html");
}); 

io.on("connection", function(socket) {

	// Context for each connection
	var currentPlayer = {};
	currentPlayer.name = "unknown";
	socket.on("player connect", function() {
		console.log(currentPlayer.name+" received: player connection");
		for(var i = 0; i < clients.length; i++) {
			var playerConnected = {
				name:clients[i].name,
				position:clients[i].position,
				rotation:clients[i].rotation,
				health:clients[i].health
			};
			// In your current game we need to tell you about the other players
			socket.emit("other player connected", playerConnected);
			console.log(currentPlayer.name+ " emit: other player connected: "+JSON.stringify(playerConnected));
		}
	});

	// Spawn enemies and current player position, broadcast your position to everyone else
	socket.on("play", function(data) {
		console.log(currentPlayer.name+" received: play: "+JSON.stringify(data));
		// If this is the first person to join the game init the enemies
		if(clients.length === 0) {
			numberOfEnemies = data.enemySpawnPoints.length;
			enemies = [];
			// forEach (enemySpawnPoint in data.enemySpawnPoints) {
			// }
			data.enemySpawnPoints.forEach(function(enemySpawnPoint) {
				var enemy = {
					name: guid(),
					position: enemySpawnPoint.position,
					rotation: enemySpawnPoint.rotation,
					health: 100
				};
				enemies.push(enemy);
			});
			playerSpawnPoints = [];
			data.playerSpawnPoints.forEach(function(_playerSpawnPoint) {
				var playerSpawnPoint = {
					position: _playerSpawnPoint.position,
					rotation: _playerSpawnPoint.rotation,
				};
				playerSpawnPoints.push(playerSpawnPoint);
			});
		}

		var enemiesResponse = {
			enemies: enemies
		};
		// Every time a player joins we always send the state of the enemies
		console.log(currentPlayer.name+ " emit: enemies: "+JSON.stringify(enemiesResponse));
		socket.emit("enemies", enemiesResponse);
		

		var randomSpawnPoint = playerSpawnPoints[Math.floor(Math.random() * playerSpawnPoints.length)];
		currentPlayer = {
			name:data.name,
			position:randomSpawnPoint.position,
			rotation: randomSpawnPoint.rotation,
			health:100
		};
		clients.push(currentPlayer);
		// Hey we got your info, here is where you will spawn at
		console.log(currentPlayer.name+ " emit: play:" +JSON.stringify(currentPlayer));
		socket.emit("play", currentPlayer);
		
		// In your current game we need to tell the other players about you, say you joined late or something
		socket.broadcast.emit("other player connected", currentPlayer);
	});

	socket.on("player move", function(data) {
		console.log("received: move: "+JSON.stringify(data));
		// Update current player position and broadcast it to everyone else
		currentPlayer.position = data.position;
		socket.broadcast.emit("player move", currentPlayer);
	});

	socket.on("player rotate", function(data) {
		console.log("received: rotation: "+JSON.stringify(data));
		// Update current player rotation and broadcast it to everyone else
		currentPlayer.rotation = data.rotation;
		socket.broadcast.emit("player rotate", currentPlayer);
	});

	socket.on("player shoot", function() {
		// Not passing any data because when the player shoots we are recieving who is shooting from the client already
		console.log(currentPlayer.name+ " received: shoot");
		var data = {
			name: currentPlayer.name,
		};
		console.log(currentPlayer.name+ " broadcast: shoot: " +JSON.stringify(data));
		socket.emit("player shoot", data);
		socket.broadcast.emit("player shoot", data);
	});

	socket.on("health", function(data) {
		console.log(currentPlayer.name+ " received: health: " +JSON.stringify(data));
		// Only change the health once, we can do this by checking the originating player
		if(data.from === currentPlayer.name) {
			var indexDamaged = 0;
			if(!data.isEnemy) {
				clients = clients.map(function(client, index) {
					if(client.name === data.name) {
						indexDamaged = index;
						client.health -= data.healthChange;
					}
					return client;
				});
			} else {
				// Is Enemy
				enemies = enemies.map(function(enemy, index) {
					if(enemy.name === data.name) {
						indexDamaged = index;
						enemy.health -= data.healthChange;
					}
					return enemy;
				});
			}
			// Send new health to client
			var response = {
				name: (!data.isEnemy) ? clients[indexDamaged].name : enemies[indexDamaged].name,
				health: (!data.isEnemy) ? clients[indexDamaged].health : enemies[indexDamaged].health
			};
			console.log(currentPlayer.name+" broadcast: health: "+JSON.stringify(response));
			socket.emit("health", response);
			socket.broadcast.emit("health", response);
		}
	});

	socket.on("disconnect", function() {
		console.log(currentPlayer.name+ " received: disconnect: " +currentPlayer.name);
		socket.broadcast.emit("other player disconnected", currentPlayer);
		console.log(currentPlayer.name+ " broadcast: other player disconnected: " +JSON.stringify(currentPlayer));
		for(var i=0; i<clients.length; i++) {
			if(clients[i].name === currentPlayer.name) {
				clients.splice(i,1);
			}
		}
	});

});

function guid() {
	function s4() {
		return Math.floor((1+Math.random()) * 0x10000).toString(16).substring(1);
	}
	return s4() + s4() + "-" + s4() + "-" + s4() + "-" + s4() + "-" + s4() + s4(); 
}

