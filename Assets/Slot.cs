﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Slot {
	public int X;
	public int Y;
	public int Z;

	private MapGenerator mapGenerator;

	public Module Module;

	public HashSet<int> Modules;

	public Slot[] Neighbours;

	public int[][] PossibleNeighbours;

	public bool Collapsed {
		get {
			return this.Module != null;
		}
	}

	public int Entropy {
		get {
			return this.Modules.Count;
		}
	}

	public Slot(int x, int y, int z, MapGenerator mapGenerator) {
		this.X = x;
		this.Y = y;
		this.Z = z;
		this.mapGenerator = mapGenerator;
		this.Modules = new HashSet<int>(Enumerable.Range(0, mapGenerator.Modules.Length));
	}

	public void InitializeNeighbours() {
		this.Neighbours = new Slot[6];
		if (this.X > 0) {
			this.Neighbours[0] = this.mapGenerator.Map[this.X - 1, this.Y, this.Z];
		}
		if (this.Y > 0) {
			this.Neighbours[1] = this.mapGenerator.Map[this.X, this.Y - 1, this.Z];
		}
		if (this.Z > 0) {
			this.Neighbours[2] = this.mapGenerator.Map[this.X, this.Y, this.Z - 1];
		}
		if (this.X < this.mapGenerator.SizeX - 1) {
			this.Neighbours[3] = this.mapGenerator.Map[this.X + 1, this.Y, this.Z];
		}
		if (this.Y < this.mapGenerator.SizeY - 1) {
			this.Neighbours[4] = this.mapGenerator.Map[this.X, this.Y + 1, this.Z];
		}
		if (this.Z < this.mapGenerator.SizeZ - 1) {
			this.Neighbours[5] = this.mapGenerator.Map[this.X, this.Y, this.Z + 1];
		}
	}

	public void Collapse(int index) {
		this.Module = this.mapGenerator.Modules[index];
		this.mapGenerator.LatestFilled = this;
		this.Build();
		this.mapGenerator.SlotsFilled++;

		this.checkConsistency(index);

		var toRemove = this.Modules.ToList();
		toRemove.Remove(index);
		this.RemoveModules(toRemove);
	}

	private void checkConsistency(int index) {
		for (int d = 0; d < 6; d++) {
			if (this.Neighbours[d] != null && this.Neighbours[d].Module != null && !this.Neighbours[d].Module.PossibleNeighbours[(d + 3) % 6].Contains(index)) {
				this.markRed();
				// This would be a result of inconsistent code, should not be possible.
				throw new Exception("Illegal collapse, not in neighbour list.");
			}
		}

		if (!this.Modules.Contains(index)) {
			this.markRed();
			// This would be a result of inconsistent code, should not be possible.
			throw new Exception("Illegal collapse!");
		}
	}

	public void CollapseRandom() {
		if (!this.Modules.Any()) {
			throw new Exception("No modules to select.");	
		}
		if (this.Collapsed) {
			throw new Exception("Slot is already collapsed.");
		}
		var candidates = this.Modules.ToList();
		float max = candidates.Select(i => this.mapGenerator.Modules[i].Prototype.Probability).Sum();
		float roll = UnityEngine.Random.Range(0f, max);
		float p = 0;
		foreach (var candidate in candidates) {
			p += this.mapGenerator.Modules[candidate].Prototype.Probability;
			if (p >= roll) {
				this.Collapse(candidate);
				return;
			}			
		}
		this.Collapse(candidates.First());
	}

	public void RemoveModules(List<int> modules) {
		var affectedNeighbouredModules = Enumerable.Range(0, 6).Select(_ => new List<int>()).ToArray();

		foreach (int module in modules) {
			if (!this.Modules.Contains(module)) {
				continue;
			}
			for (int d = 0; d < 6; d++) {
				foreach (int possibleNeighbour in this.mapGenerator.Modules[module].PossibleNeighbours[d]) {
					if (this.PossibleNeighbours[d][possibleNeighbour] == 1) {
						affectedNeighbouredModules[d].Add(possibleNeighbour);
					}
					this.PossibleNeighbours[d][possibleNeighbour]--;
				}
			}
			this.Modules.Remove(module);
		}

		for (int d = 0; d < 6; d++) {
			if (affectedNeighbouredModules[d].Any() && this.Neighbours[d] != null) {
				this.Neighbours[d].RemoveModules(affectedNeighbouredModules[d]);
			}
		}
	}

	private void markRed() {
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.parent = this.mapGenerator.transform;
		cube.GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
		cube.transform.position = this.GetPosition();
	}

	public void Build() {
		if (this.Module == null || this.Module.Prototype.Spawn == false) {
			return;
		}

		var gameObject = GameObject.Instantiate(this.Module.Prototype.gameObject);
		var prototype = gameObject.GetComponent<ModulePrototype>();
		GameObject.DestroyImmediate(prototype);
		gameObject.transform.parent = this.mapGenerator.transform;
		gameObject.transform.position = this.GetPosition();
		gameObject.transform.rotation = Quaternion.Euler(Vector3.up * 90f * this.Module.Rotation);
	}

	public Vector3 GetPosition() {
		return this.mapGenerator.GetWorldspacePosition(this.X, this.Y, this.Z);
	}
}
