using System;
using UnityEngine;

namespace VoxelEngine.Example
{
    public class VoxelEngineExample : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter meshFilter;
        [SerializeField]
        private MeshFilter meshFilter2;
        [SerializeField]
        private MeshRenderer meshRenderer;
        [SerializeField]
        private MeshRenderer meshRenderer1;
        private void Start()
        {
            GenerateMeshNaive();
            GenerateMeshBinary();
        }

        private void GenerateMeshNaive()
        {
            IVoxelsGenerator voxelsesGenerator = new ExampleVoxelsGenerator(32,32,32);
            IMeshGenerator meshGenerator = new ChunkMeshGenerator();
            var chunk = new ChunkData()
            {
                Voxels = voxelsesGenerator.Voxels,
                BitMatrix = voxelsesGenerator.BitMatrix
            };
            var mesh = meshGenerator.BuildChunkMesh(chunk);
            meshFilter.mesh = mesh;
        }
        
        public ComputeBuffer voxelBuffer;
        public Shader voxelShader;
        private Material voxelMaterial;
        private void GenerateMeshBinary()
        {
            IVoxelsGenerator voxelsesGenerator = new ExampleVoxelsGenerator(32,32,32);
            IMeshGenerator meshGenerator = new BinaryMeshGenerator();
            voxelBuffer = new ComputeBuffer(32 * 32 * 8, sizeof(int));
            voxelBuffer.SetData(voxelsesGenerator.GetVoxelBuffer());
            voxelMaterial = Instantiate(meshRenderer1.sharedMaterial);
            voxelMaterial.SetBuffer(13, voxelBuffer);
            var chunk = new ChunkData()
            {
                Voxels = voxelsesGenerator.Voxels,
                BitMatrix = voxelsesGenerator.BitMatrix
            };
            var mesh = meshGenerator.BuildChunkMesh(chunk);
            meshRenderer.material = voxelMaterial;
            meshFilter2.mesh = mesh;
        }
        
        private void OnDestroy()
        {
            voxelBuffer.Release();
        }
    }
}