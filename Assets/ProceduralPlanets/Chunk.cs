﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Chunk
{

	public Planet planet;

	public ulong id;
	public Chunk parent;
	public ulong generation;

	//public Range rangePosRealSubdivided;
	//public Range rangePosToGenerateInto;
	//public Range rangeLocalPosToGenerateInto;
	public Range rangeUnitCubePosRealSubdivided;
	public Range rangeUnitCubePosToGenerateInto;
	public Range rangePosToCalculateScreenSizeOn;


	public WorldPos offsetFromPlanetCenter;

	public float chunkRangeMaxAngleDeg;


	// float slope = (abs(sx0 - s0) + abs(sy0 - s0)) / _chunkRelativeSize * (w / 256.0);
	//public float ChunkRelativeSize { get { return chunkRadius / planetConfig.radiusStart; } }

	//public bool GenerateUsingPlanetGlobalPos { get { return chunkRangeMaxAngleDeg > 2; } }
	public bool GenerateUsingPlanetGlobalPos { get { return true; } }
	//public int SlopeModifier { get { return (int)Mathf.Pow(2, generation); } }
	//public float SlopeModifier { get { return (float)((planetConfig.radiusStart / chunkRadius / 4 * (heightMapResolution / 1024.0)) / 44.0 * HeightRange); } }
	public float SlopeModifier { get { return (float)(Mathf.Pow(2, generation) * (HeightMapResolution / 1024.0) * HeightRange); } }
	public float HeightRange { get { return heightMax - heightMin; } }

	public Planet.PlanetConfig planetConfig { get { return planet.planetConfig; } }
	public Planet.ChunkConfig chunkConfig { get { return planet.chunkConfig; } }


	public RenderTexture chunkHeightMap;
	public RenderTexture chunkNormalMap;
	public RenderTexture chunkDiffuseMap;
	public RenderTexture chunkSlopeAndCurvatureMap;
	public RenderTexture chunkMeshNormals;

	public float heightMin = 0;
	public float heightMax = 1;

	public ChildPosition childPosition;

	public enum ChildPosition
	{
		NoneNoParent = 0,
		TopLeft = 1,
		TopRight = 2,
		BottomLeft = 3,
		BottomRight = 4,
	}


	public bool generationBegan;
	public bool isGenerationDone;
	public List<Chunk> children = new List<Chunk>(4);
	public float chunkRadius;

	int HeightMapResolution { get { return chunkConfig.textureResolution; } }
	int NormalMapResolution { get { return HeightMapResolution; } }
	int ChunkSlopeMapResolution { get { return HeightMapResolution; } }
	int DiffuseMapResolution { get { return ChunkSlopeMapResolution; } }

	public class Behavior : MonoBehaviour
	{
		public Chunk chunk;
		private void OnDrawGizmosSelected()
		{
			if (chunk != null)
				chunk.OnDrawGizmosSelected();
		}
	}

	public static Chunk Create(Planet planet, Range range, ulong id, Chunk parent = null, ulong generation = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		MyProfiler.BeginSample("Procedural Planet / Create chunk");

		var chunk = new Chunk();

		chunk.planet = planet;
		chunk.id = id;
		chunk.parent = parent;
		chunk.generation = generation;
		chunk.childPosition = childPosition;
		chunk.rangeUnitCubePosRealSubdivided = range;

		//chunk.rangePosToGenerateInto = new Range
		//{
		//	a = chunk.rangeUnitCubePos.a.normalized * planet.planetConfig.radiusStart,
		//	b = chunk.rangeUnitCubePos.b.normalized * planet.planetConfig.radiusStart,
		//	c = chunk.rangeUnitCubePos.c.normalized * planet.planetConfig.radiusStart,
		//	d = chunk.rangeUnitCubePos.d.normalized * planet.planetConfig.radiusStart,
		//};

		if (chunk.chunkConfig.useSkirts)
		{
			var ratio = ((chunk.chunkConfig.numberOfVerticesOnEdge - 1) / 2.0f) / ((chunk.chunkConfig.numberOfVerticesOnEdge - 1 - 2) / 2.0f);
			var center = chunk.rangeUnitCubePosRealSubdivided.CenterPos;
			var a = chunk.rangeUnitCubePosRealSubdivided.a - center;
			var b = chunk.rangeUnitCubePosRealSubdivided.b - center;
			var c = chunk.rangeUnitCubePosRealSubdivided.c - center;
			var d = chunk.rangeUnitCubePosRealSubdivided.d - center;
			chunk.rangeUnitCubePosToGenerateInto.a = a * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.b = b * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.c = c * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.d = d * ratio + center;
		}
		else
		{
			chunk.rangeUnitCubePosToGenerateInto = chunk.rangeUnitCubePosRealSubdivided;
		}


		chunk.rangePosToCalculateScreenSizeOn = new Range
		{
			a = chunk.rangeUnitCubePosToGenerateInto.a.normalized * planet.planetConfig.radiusStart,
			b = chunk.rangeUnitCubePosToGenerateInto.b.normalized * planet.planetConfig.radiusStart,
			c = chunk.rangeUnitCubePosToGenerateInto.c.normalized * planet.planetConfig.radiusStart,
			d = chunk.rangeUnitCubePosToGenerateInto.d.normalized * planet.planetConfig.radiusStart,
		};

		chunk.chunkRadius = chunk.rangePosToCalculateScreenSizeOn.ToBoundingSphere().radius;


		//chunk.rangeDirToGenerateInto = new Range
		//{
		//	a = chunk.rangePosToGenerateInto.a.normalized,
		//	b = chunk.rangePosToGenerateInto.b.normalized,
		//	c = chunk.rangePosToGenerateInto.c.normalized,
		//	d = chunk.rangePosToGenerateInto.d.normalized,
		//};


		//chunk.rangeLocalPosToGenerateInto = new Range
		//{
		//	a = chunk.rangePosToGenerateInto.a - chunk.offsetFromPlanetCenter,
		//	b = chunk.rangePosToGenerateInto.b - chunk.offsetFromPlanetCenter,
		//	c = chunk.rangePosToGenerateInto.c - chunk.offsetFromPlanetCenter,
		//	d = chunk.rangePosToGenerateInto.d - chunk.offsetFromPlanetCenter,
		//};

		chunk.chunkRangeMaxAngleDeg = Mathf.Max(
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.a, chunk.rangeUnitCubePosToGenerateInto.b),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.b, chunk.rangeUnitCubePosToGenerateInto.c),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.c, chunk.rangeUnitCubePosToGenerateInto.d),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.d, chunk.rangeUnitCubePosToGenerateInto.a),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.a, chunk.rangeUnitCubePosToGenerateInto.c),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.b, chunk.rangeUnitCubePosToGenerateInto.d)
		);

		chunk.offsetFromPlanetCenter = chunk.rangeUnitCubePosToGenerateInto.CenterPos.normalized * planet.planetConfig.radiusStart;

		MyProfiler.EndSample();

		return chunk;
	}

	private void AddChild(WorldPos a, WorldPos b, WorldPos c, WorldPos d, ChildPosition cp, ushort index)
	{
		var range = new Range()
		{
			a = a,
			b = b,
			c = c,
			d = d,
		};

		var child = Create(
			planet: planet,
			parent: this,
			range: range,
			id: id << 2 | index,
			generation: generation + 1,
			childPosition: cp
		);

		children.Add(child);
	}

	public void EnsureChildrenInstancesAreCreated()
	{
		if (children.Count <= 0)
		{
			/*
			a----ab---b
			|    |    |
			ad--mid---bc
			|    |    |
			d----dc---c
			*/

			var a = rangeUnitCubePosRealSubdivided.a;
			var b = rangeUnitCubePosRealSubdivided.b;
			var c = rangeUnitCubePosRealSubdivided.c;
			var d = rangeUnitCubePosRealSubdivided.d;
			var ab = (a + b) / 2.0f;
			var ad = (a + d) / 2.0f;
			var bc = (b + c) / 2.0f;
			var dc = (d + c) / 2.0f;
			var mid = (ab + ad + dc + bc) / 4.0f;

			AddChild(a, ab, mid, ad, ChildPosition.TopLeft, 0);
			AddChild(ab, b, bc, mid, ChildPosition.TopRight, 1);
			AddChild(ad, mid, dc, d, ChildPosition.BottomLeft, 2);
			AddChild(mid, bc, c, dc, ChildPosition.BottomRight, 3);
		}
	}



	public void Generate()
	{
		if (generationBegan) return;
		generationBegan = true;



		MyProfiler.BeginSample("Procedural Planet / Generate chunk");

		if (gameObject) GameObject.Destroy(gameObject);

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Height map");
		GenerateHeightMap();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh");
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate");
		GenerateMesh();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Prepare");
		PrepareMesh();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Skirts");
		MoveSkirtVertices();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Upload");
		UploadMesh();
		MyProfiler.EndSample();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Normal map");
		GenerateNormalMap();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Diffuse map");
		GenerateDiffuseMap();
		MyProfiler.EndSample();

		MyProfiler.EndSample();

		isGenerationDone = true;
	}


	void SetAll(ComputeShader c, int kernelIndex)
	{
		rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");

		c.SetInt("_numberOfVerticesOnEdge", chunkConfig.numberOfVerticesOnEdge);

		c.SetFloat("_heightMin", heightMin);
		c.SetFloat("_heightMax", heightMax);

		c.SetFloat("_radiusStart", planetConfig.radiusStart);
		c.SetFloat("_radiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);

		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);

		c.SetFloat("_chunkRadius", chunkRadius);
		c.SetInt("_generation", (int)generation);

		c.SetTexture(kernelIndex, "_planetHeightMap", planetConfig.planetHeightMap);
		if (chunkHeightMap != null) c.SetTexture(kernelIndex, "_chunkHeightMap", chunkHeightMap);
		if (chunkNormalMap != null) c.SetTexture(kernelIndex, "_chunkNormalMap", chunkNormalMap);
		if (chunkSlopeAndCurvatureMap != null) c.SetTexture(kernelIndex, "_chunkSlopeAndCurvatureMap", chunkSlopeAndCurvatureMap);

		c.SetFloat("_slopeModifier", SlopeModifier);



		var parentUvStart = Vector2.zero;
		if (childPosition == ChildPosition.TopLeft) parentUvStart = new Vector2(0, 0);
		else if (childPosition == ChildPosition.TopRight) parentUvStart = new Vector2(0.5f, 0);
		else if (childPosition == ChildPosition.BottomLeft) parentUvStart = new Vector2(0, 0.5f);
		else if (childPosition == ChildPosition.BottomRight) parentUvStart = parentUvStart = new Vector2(0.5f, 0.5f);


		if (chunkConfig.useSkirts)
		{
			var off = 1.0f / (chunkConfig.numberOfVerticesOnEdge - 1) / 2.0f;
			if (childPosition == ChildPosition.TopLeft) parentUvStart += new Vector2(off, off);
			else if (childPosition == ChildPosition.TopRight) parentUvStart += new Vector2(-off, off);
			else if (childPosition == ChildPosition.BottomLeft) parentUvStart += new Vector2(off, -off);
			else if (childPosition == ChildPosition.BottomRight) parentUvStart += new Vector2(-off, -off);
		}

		c.SetVector("_parentUvStart", parentUvStart);

	}


	void GenerateHeightMap()
	{
		// pass 0
		if (chunkConfig.rescaleToMinMax)
		{
			var heightRough = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			heightRough.wrapMode = TextureWrapMode.Clamp;
			heightRough.filterMode = FilterMode.Bilinear;
			heightRough.enableRandomWrite = true;
			heightRough.Create();

			var c = chunkConfig.generateChunkHeightMapPass1;
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
			c.SetFloat("_heightMin", 0);
			c.SetFloat("_heightMax", 1);

			c.SetTexture(0, "_chunkHeightMap", heightRough);
			c.Dispatch(0, heightRough.width / 16, heightRough.height / 16, 1);

			MyProfiler.BeginSample("find texture min max");
			var result = FindTextureMinMax.Find(heightRough);
			MyProfiler.EndSample();
			heightMax = result.max.x;
			heightMin = result.min.x;

			var r = 0.1f;//HeightRange / 10.0f;
			heightMax += r;
			heightMin -= r;

			//DEBUG
			//heightMax = 1; heightMin = 0;

			RenderTexture.ReleaseTemporary(heightRough);
		}

		// pass 1
		RenderTexture height1;
		{
			height1 = RenderTexture.GetTemporary(HeightMapResolution, HeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			height1.wrapMode = TextureWrapMode.Clamp;
			height1.filterMode = FilterMode.Bilinear;
			height1.enableRandomWrite = true;
			height1.Create();

			var c = chunkConfig.generateChunkHeightMapPass1;
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
			c.SetFloat("_heightMin", heightMin);
			c.SetFloat("_heightMax", heightMax);

			c.SetTexture(0, "_chunkHeightMap", height1);
			c.Dispatch(0, height1.width / 16, height1.height / 16, 1);

			GenerateSlopeAndCurvatureMap(height1);
		}

		// pass 2
		{
			if (chunkHeightMap == null)
			{
				chunkHeightMap = new RenderTexture(HeightMapResolution, HeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
				chunkHeightMap.wrapMode = TextureWrapMode.Clamp;
				chunkHeightMap.filterMode = FilterMode.Bilinear;
				/*chunkHeightMap.enableRandomWrite = true;
				chunkHeightMap.useMipMap = true;
				chunkHeightMap.autoGenerateMips = false;
				chunkHeightMap.antiAliasing = 8;*/
				chunkHeightMap.enableRandomWrite = true;
				chunkHeightMap.Create();
			}

			var c = chunkConfig.generateChunkHeightMapPass2;
			SetAll(c, 0);
			c.SetTexture(0, "_chunkHeightMap", height1);

			c.SetTexture(0, "_chunkHeightMapNew", chunkHeightMap);
			c.Dispatch(0, chunkHeightMap.width / 16, chunkHeightMap.height / 16, 1);

			if (chunkHeightMap.useMipMap) chunkHeightMap.GenerateMips();

			GenerateSlopeAndCurvatureMap(chunkHeightMap);
		}

		RenderTexture.ReleaseTemporary(height1);


	}


	void GenerateSlopeAndCurvatureMap(RenderTexture heightMap)
	{
		if (chunkSlopeAndCurvatureMap == null)
		{
			chunkSlopeAndCurvatureMap = new RenderTexture(ChunkSlopeMapResolution, ChunkSlopeMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			chunkSlopeAndCurvatureMap.wrapMode = TextureWrapMode.Clamp;
			chunkSlopeAndCurvatureMap.filterMode = FilterMode.Bilinear;
			chunkSlopeAndCurvatureMap.enableRandomWrite = true;
			chunkSlopeAndCurvatureMap.Create();
		}


		var c = chunkConfig.generateSlopeAndCurvatureMap;

		var kernelIndex = 0;
		if (parent != null) kernelIndex = c.FindKernel("parentExists");
		else kernelIndex = c.FindKernel("parentDoesNotExist");

		SetAll(c, kernelIndex);
		if (parent != null)
			c.SetTexture(kernelIndex, "_parentChunkSlopeAndCurvatureMap", parent.chunkSlopeAndCurvatureMap);
		c.SetTexture(kernelIndex, "_chunkHeightMap", heightMap);

		c.SetTexture(kernelIndex, "_chunkSlopeAndCurvatureMap", chunkSlopeAndCurvatureMap);
		c.Dispatch(kernelIndex, chunkSlopeAndCurvatureMap.width / 16, chunkSlopeAndCurvatureMap.height / 16, 1);
	}



	ComputeBuffer vertexGPUBuffer { get { return planet.chunkVertexGPUBuffer; } }
	Vector3[] vertexCPUBuffer { get { return planet.chunkVertexCPUBuffer; } }

	Mesh mesh;
	void GenerateMesh()
	{

		var c = chunkConfig.generateChunkVertices;
		var v = vertexCPUBuffer;
		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		var kernelIndex = 0;
		if (GenerateUsingPlanetGlobalPos) kernelIndex = c.FindKernel("generateUsingPlanetGlobalPos");
		else kernelIndex = c.FindKernel("generateUsingChunkLocalPos");

		rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
		c.SetInt("_numberOfVerticesOnEdge", verticesOnEdge);
		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);
		c.SetTexture(kernelIndex, "_chunkHeightMap", chunkHeightMap);
		c.SetFloat("_heightMin", heightMin);
		c.SetFloat("_heightMax", heightMax);

		c.SetBuffer(kernelIndex, "_vertices", vertexGPUBuffer);
		c.Dispatch(kernelIndex, verticesOnEdge, verticesOnEdge, 1);

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate / Download data");
		vertexGPUBuffer.GetData(v);
		MyProfiler.EndSample();

		{
			int aIndex = 0;
			int bIndex = verticesOnEdge - 1;
			int cIndex = verticesOnEdge * verticesOnEdge - 1;
			int dIndex = cIndex - (verticesOnEdge - 1);
			rangePosToCalculateScreenSizeOn.a = new WorldPos(v[aIndex]);
			rangePosToCalculateScreenSizeOn.b = new WorldPos(v[bIndex]);
			rangePosToCalculateScreenSizeOn.c = new WorldPos(v[cIndex]);
			rangePosToCalculateScreenSizeOn.d = new WorldPos(v[dIndex]);

			if (!GenerateUsingPlanetGlobalPos)
			{
				rangePosToCalculateScreenSizeOn.a += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.b += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.c += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.d += offsetFromPlanetCenter;
			}
		}


	}

	void PrepareMesh()
	{
		if (mesh) Mesh.Destroy(mesh);
		// TODO: optimize: fill mesh vertices on GPU instead of CPU, calculate UVs, normals and tangents on GPU instead of CPU, remember we still need vertices on CPU for mesh collider
		mesh = new Mesh();
		mesh.name = this.ToString();
		mesh.vertices = vertexCPUBuffer;
		mesh.triangles = planet.GetSegmentIndicies();
		mesh.uv = planet.GetSefgmentUVs();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}

	void MoveSkirtVertices()
	{
		if (chunkConfig.useSkirts)
		{
			var v = vertexCPUBuffer;
			var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

			var decreaseSkirtsBy = -offsetFromPlanetCenter.ToVector3().normalized * chunkRadius / 2.0f;
			for (int i = 0; i < verticesOnEdge; i++)
			{
				v[i] += decreaseSkirtsBy; // top line
				v[verticesOnEdge * (verticesOnEdge - 1) + i] += decreaseSkirtsBy; // bottom line
			}
			for (int i = 1; i < verticesOnEdge - 1; i++)
			{
				v[verticesOnEdge * i] += decreaseSkirtsBy; // left line
				v[verticesOnEdge * i + verticesOnEdge - 1] += decreaseSkirtsBy; // right line
			}
			mesh.vertices = vertexCPUBuffer;
		}
	}

	void UploadMesh()
	{
		mesh.UploadMeshData(false);
	}


	void GenerateNormalMap()
	{
		var c = chunkConfig.generateChunkNormapMap;
		if (c == null) return;

		if (chunkNormalMap != null && chunkNormalMap.width != NormalMapResolution)
		{
			chunkNormalMap.Release();
			chunkNormalMap = null;
		}

		if (chunkNormalMap == null)
		{
			chunkNormalMap = new RenderTexture(NormalMapResolution, NormalMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			chunkNormalMap.enableRandomWrite = true;
			chunkNormalMap.Create();
		}

		SetAll(c, 0);
		c.SetFloat("_heightMapRealRange", planetConfig.radiusHeightMapMultiplier);
		c.SetFloat("_normalLength", chunkRadius / chunkNormalMap.width);

		c.SetTexture(0, "_chunkNormalMap", chunkNormalMap);
		c.Dispatch(0, chunkNormalMap.width / 16, chunkNormalMap.height / 16, 1);

		if (material) material.SetTexture("_BumpMap", chunkNormalMap);
	}



	void GenerateDiffuseMap()
	{
		if (chunkDiffuseMap != null && chunkDiffuseMap.width != DiffuseMapResolution)
		{
			chunkDiffuseMap.Release();
			chunkDiffuseMap = null;
		}

		if (chunkDiffuseMap == null)
		{
			chunkDiffuseMap = new RenderTexture(DiffuseMapResolution, DiffuseMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			chunkDiffuseMap.wrapMode = TextureWrapMode.Clamp;
			chunkDiffuseMap.filterMode = FilterMode.Trilinear;
			chunkDiffuseMap.enableRandomWrite = true;
			chunkDiffuseMap.useMipMap = true;
			chunkDiffuseMap.autoGenerateMips = false;
			chunkDiffuseMap.antiAliasing = 8;
			chunkDiffuseMap.Create();
		}

		var c = chunkConfig.generateChunkDiffuseMap;
		SetAll(c, 0);
		c.SetFloat("_mipMapLevel", Mathf.Max(1, chunkRadius / 10.0f) - 1);
		c.SetTexture(0, "_grass", chunkConfig.grass);
		c.SetTexture(0, "_clay", chunkConfig.clay);
		c.SetTexture(0, "_rock", chunkConfig.rock);

		c.SetTexture(0, "_chunkDiffuseMap", chunkDiffuseMap);
		c.Dispatch(0, chunkDiffuseMap.width / 16, chunkDiffuseMap.height / 16, 1);

		if (chunkDiffuseMap.useMipMap) chunkDiffuseMap.GenerateMips();
		if (material) material.mainTexture = chunkDiffuseMap;
	}

	private void OnDrawGizmosSelected()
	{
		if (gameObject && gameObject.activeSelf)
		{
			Gizmos.color = Color.cyan;
			//rangePosRealSubdivided.DrawGizmos();
			rangePosToCalculateScreenSizeOn.DrawGizmos();
		}
	}


	bool lastVisible = false;
	public void SetVisible(bool visible)
	{
		if (this.generationBegan && this.isGenerationDone)
		{
			if (visible == lastVisible) return;
			lastVisible = visible;
		}

		if (visible)
		{
			if (this.generationBegan == false)
			{
				//Log.Warn("trying to show segment " + this + " that did not begin generation");
			}
			else if (this.isGenerationDone == false)
			{
				//Log.Warn("trying to show segment " + this + " that did not finish generation");
			}
			else DoRender(true);
		}
		else
		{
			DoRender(false);
		}
	}

	public void HideAllChildren()
	{
		foreach (var child in children)
		{
			child.SetVisible(false);
			child.HideAllChildren();
		}
	}

	public void MarkForRegeneration()
	{
		generationBegan = false;
		isGenerationDone = false;
		if (gameObject) GameObject.Destroy(gameObject);
		lastVisible = false;
	}

	void DestroyData()
	{
		if (gameObject) GameObject.Destroy(gameObject);
		if (mesh) Mesh.Destroy(mesh);
		if (chunkNormalMap) chunkNormalMap.Release();
		if (chunkDiffuseMap) chunkDiffuseMap.Release();
		if (chunkHeightMap) chunkHeightMap.Release();
	}

	void TryCreateGameObject()
	{
		if (gameObject) return;

		MyProfiler.BeginSample("Procedural Planet / Create GameObject");

		var name = ToString();

		var go = gameObject = new GameObject(name);
		go.transform.parent = planet.transform;
		if (!GenerateUsingPlanetGlobalPos)
			go.transform.localPosition = offsetFromPlanetCenter;

		var behavior = go.AddComponent<Behavior>();
		behavior.chunk = this;

		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = chunkConfig.chunkMaterial;
		material = meshRenderer.material;

		if (chunkConfig.createColliders)
		{
			MyProfiler.BeginSample("Procedural Planet / Create GameObject / Collider");
			var meshCollider = go.AddComponent<MeshCollider>();
			meshCollider.sharedMesh = mesh;
			MyProfiler.EndSample();
		}

		if (chunkDiffuseMap) material.mainTexture = chunkDiffuseMap;
		if (chunkNormalMap) material.SetTexture("_BumpMap", chunkNormalMap);

		MyProfiler.EndSample();
	}

	DateTime notRenderedTimeStamp;
	GameObject gameObject;
	Material material;
	void DoRender(bool doRender)
	{
		if (doRender)
		{
			if (isGenerationDone)
				TryCreateGameObject();

			if (gameObject != null && !gameObject.activeSelf)
				gameObject.SetActive(true);
		}
		else
		{
			if (gameObject != null)
			{
				if (gameObject.activeSelf)
				{
					notRenderedTimeStamp = DateTime.UtcNow;
					gameObject.SetActive(false);

					// TODO: schedule CleanUpChance(); for execution in chunkConfig.destroyGameObjectIfNotVisibleForSeconds
				}
				else
				{
					CleanUpChance();
				}
			}
		}
	}

	void CleanUpChance()
	{
		if (!gameObject.activeSelf)
		{
			if ((DateTime.UtcNow - notRenderedTimeStamp).TotalSeconds > chunkConfig.destroyGameObjectIfNotVisibleForSeconds)
			{
				GameObject.Destroy(gameObject);
				gameObject = null;
			}
		}
	}


	private float GetSizeOnScreen(Planet.SubdivisionData data)
	{
		var myPos = rangePosToCalculateScreenSizeOn.CenterPos + planet.transform.position;
		var distanceToCamera = Vector3.Distance(myPos, data.pos);

		// TODO: this is world space, doesnt take into consideration rotation, not good,
		// but we dont care about rotation, we want to have correct detail even if we are looking at chunk from side?
		var sphere = rangePosToCalculateScreenSizeOn.ToBoundingSphere();
		var radiusWorldSpace = sphere.radius;
		var fov = data.fieldOfView;
		var cot = 1.0f / Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
		var radiusCameraSpace = radiusWorldSpace * cot / distanceToCamera;

		return radiusCameraSpace;
	}

	public float lastGenerationWeight;
	public float GetGenerationWeight(Planet.SubdivisionData data)
	{
		var weight = GetSizeOnScreen(data);
		lastGenerationWeight = weight;
		return weight;
	}

	public override string ToString()
	{
		return typeof(Chunk) + " id:#" + id + " generation:" + generation;
	}


}
