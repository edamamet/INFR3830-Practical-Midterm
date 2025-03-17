using System;
using System.Collections.Generic;
using UnityEngine;
namespace _Project.Scripts {
    public class RemoteCubeManager : MonoBehaviour {
        [SerializeField] Transform cubePrefab;
        [SerializeField] Transform cubeParent;

        Dictionary<Guid, Transform> cubes = new();

        void OnEnable() {
            Client.OnPositionReceived += OnPositionReceived;
            Client.OnPlayerConnected += OnPlayerConnected;
        }

        void OnDisable() {
            Client.OnPositionReceived -= OnPositionReceived;
            Client.OnPlayerConnected -= OnPlayerConnected;
        }

        void OnPositionReceived(Guid id, Vector3 position) {
            if (id == Client.ClientId) return;
            Debug.Log($"Received position: {id},{position}");
            if (!cubes.TryGetValue(id, out var cube)) return;
            cube.position = position;
        }

        void OnPlayerConnected(Guid id) {
            Debug.Log($"{id} connected, {Client.ClientId}");
            if (id == Client.ClientId) return;
            var newCube = Instantiate(cubePrefab, GameManager.Position, Quaternion.identity, cubeParent);
            newCube.name = id.ToString();
            cubes.Add(id, newCube);
        }
    }
}
