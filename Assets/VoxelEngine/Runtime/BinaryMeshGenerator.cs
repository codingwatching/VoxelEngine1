using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public enum SideOrientation
    {
        Right = 0,
        Left = 1,
        Top = 2,
        Bottom = 3,
        Back = 4,
        Front = 5,
    }

    [BurstCompile]
    partial struct BinaryMeshingJob : IJob
    {
        [ReadOnly]
        public NativeArray<ulong> BitMatrix;
        public NativeList<int> Triangles;
        public NativeList<uint> Verts;
        public NativeArray<ulong> CullingBitMatrix;
        private int vertexCount;
        private int sidesBitMatrixLength;

        public void Execute()
        {
            sidesBitMatrixLength = VoxelEngineConstants.CHUNK_VOXEL_SIZE * VoxelEngineConstants.CHUNK_VOXEL_SIZE;
            vertexCount = 0;

            GenerateSidesBitMatrix(BitMatrix, CullingBitMatrix);
            for (int i = 0; i < 6; i++)
            {
                vertexCount = DrawFaces(
                    new NativeSlice<ulong>(CullingBitMatrix, i * sidesBitMatrixLength, sidesBitMatrixLength),
                    (SideOrientation)i, Verts, Triangles, vertexCount);
            }
        }

        private static void GenerateSidesBitMatrix(NativeArray<ulong> bitMatrix, NativeArray<ulong> cullingBitMatrix)
        {
            int sizeSquare = VoxelEngineConstants.CHUNK_VOXEL_SIZE  * VoxelEngineConstants.CHUNK_VOXEL_SIZE ;

            for (int i = 0; i < sizeSquare; i++)
            {
                for (int faces = 0; faces < 3; faces++)
                {
                    cullingBitMatrix[i + sizeSquare * faces * 2] = bitMatrix[i + sizeSquare * faces] &
                                                                   ~(bitMatrix[i + sizeSquare * faces] >> 1);
                    cullingBitMatrix[i + sizeSquare * (faces * 2 + 1)] = bitMatrix[i + sizeSquare * faces] &
                                                                         ~(bitMatrix[i + sizeSquare * faces] << 1);
                }
            }
        }

        private static int DrawFaces(NativeSlice<ulong> cullingMatrix, SideOrientation sideOrientation,
            NativeList<uint> Verts, NativeList<int> Triangles, int vertexCount)
        {
            for (int i = 1; i < VoxelEngineConstants.CHUNK_VOXEL_SIZE-1 ; i++)
            {
                for (int j = 1; j < VoxelEngineConstants.CHUNK_VOXEL_SIZE-1 ; j++)
                {
                    for (int k = 1; k < VoxelEngineConstants.CHUNK_VOXEL_SIZE-1 ; k++)
                    {
                        if (((cullingMatrix[i + j * VoxelEngineConstants.CHUNK_VOXEL_SIZE ] >> k) & 1UL) == 1)
                        {
                            DrawFace(i-1, j-1, k-1, sideOrientation, Verts, Triangles, vertexCount);
                            vertexCount += 4;
                        }
                    }
                }
            }

            return vertexCount;
        }

        private static void DrawFace(int x, int y, int z, SideOrientation sideOrientation, NativeList<uint> Verts,
            NativeList<int> Triangles, int vertexCount)
        {
            switch (sideOrientation)
            {
                case SideOrientation.Left: //Left
                    Verts.Add(EncodeValue(sideOrientation, z, y, x));
                    Verts.Add(EncodeValue(sideOrientation, z, y, x + 1));
                    Verts.Add(EncodeValue(sideOrientation, z, y + 1, x + 1));
                    Verts.Add(EncodeValue(sideOrientation, z, y + 1, x));

                    FrontFace(Triangles, vertexCount);
                    break;

                case SideOrientation.Right: //Right
                    Verts.Add(EncodeValue(sideOrientation, z + 1, y, x));
                    Verts.Add(EncodeValue(sideOrientation, z + 1, y, x + 1));
                    Verts.Add(EncodeValue(sideOrientation, z + 1, y + 1, x + 1));
                    Verts.Add(EncodeValue(sideOrientation, z + 1, y + 1, x));
                    BackFace(Triangles, vertexCount);

                    break;
                case SideOrientation.Top: //Top
                    Verts.Add(EncodeValue(sideOrientation, x, z + 1, y));
                    Verts.Add(EncodeValue(sideOrientation, x, z + 1, y + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, z + 1, y + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, z + 1, y));

                    FrontFace(Triangles, vertexCount);
                    break;
                case SideOrientation.Bottom: //Bottom
                    Verts.Add(EncodeValue(sideOrientation, x, z, y));
                    Verts.Add(EncodeValue(sideOrientation, x, z, y + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, z, y + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, z, y));

                    BackFace(Triangles, vertexCount);

                    break;
                case SideOrientation.Front: //Front
                    Verts.Add(EncodeValue(sideOrientation, x, y, z));
                    Verts.Add(EncodeValue(sideOrientation, x, y + 1, z));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, y + 1, z));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, y, z));

                    FrontFace(Triangles, vertexCount);
                    break;
                case SideOrientation.Back: //Back
                    Verts.Add(EncodeValue(sideOrientation, x, y, z + 1));
                    Verts.Add(EncodeValue(sideOrientation, x, y + 1, z + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, y + 1, z + 1));
                    Verts.Add(EncodeValue(sideOrientation, x + 1, y, z + 1));


                    BackFace(Triangles, vertexCount);
                    break;
            }
        }

        private static void FrontFace(NativeList<int> Triangles, int vertexCount)
        {
            Triangles.Add(vertexCount);
            Triangles.Add(vertexCount + 1);
            Triangles.Add(vertexCount + 2);
            Triangles.Add(vertexCount);
            Triangles.Add(vertexCount + 2);
            Triangles.Add(vertexCount + 3);
        }

        private static void BackFace(NativeList<int> Triangles, int vertexCount)
        {
            Triangles.Add(vertexCount);
            Triangles.Add(vertexCount + 2);
            Triangles.Add(vertexCount + 1);
            Triangles.Add(vertexCount);
            Triangles.Add(vertexCount + 3);
            Triangles.Add(vertexCount + 2);
        }

        private static void TransposeMatrix(NativeSlice<ulong> bitMatrix)
        {
            for (var i = 0; i < 64; i++)
            {
                for (var j = i + 1; j < 64; j++)
                {
                    // Extract the bits at (i, j) and (j, i)
                    ulong bit1 = (bitMatrix[i] >> j) & 1UL;
                    ulong bit2 = (bitMatrix[j] >> i) & 1UL;

                    // Swap the bits if they are different
                    if (bit1 == bit2) continue;
                    bitMatrix[i] ^= (1UL << j);
                    bitMatrix[j] ^= (1UL << i);
                }
            }
        }

        private static uint EncodeValue(SideOrientation side, int x, int y, int z)
        {
            int faceId = (int)side;
            // Ensure values are within the allowed range
            x &= 0x3F; // 6 bits (0-63)
            y &= 0x3F; // 6 bits (0-63)
            z &= 0x3F; // 6 bits (0-63)
            faceId &= 0x7; // 3 bits (0-7)

            //change to byte
            uint packedData = (uint)(x) | ((uint)y << 6) | ((uint)z << 12) | ((uint)faceId << 18);

            return packedData;
        }
    }

    public class BinaryMeshGenerator : IMeshGenerator
    {
        private int trianglesCount = 0;

        private NativeList<int> triangles;
        private NativeList<uint> verts = new(Allocator.Persistent);

        public Mesh BuildChunkMesh(ChunkData chunkData, Mesh mesh = null)
        {
            triangles = new NativeList<int>(Allocator.Persistent);
            verts = new NativeList<uint>(Allocator.Persistent);

            if (!mesh)
                mesh = new Mesh();
            mesh.Clear();

            var job = new BinaryMeshingJob()
            {
                Triangles = triangles,
                Verts = verts,
                BitMatrix = chunkData.BitMatrix,
                CullingBitMatrix = new NativeArray<ulong>(VoxelEngineConstants.CHUNK_VOXEL_SIZE * VoxelEngineConstants.CHUNK_VOXEL_SIZE * 6, Allocator.TempJob)
            };
            var handle = job.Schedule();
            handle.Complete();
            job.CullingBitMatrix.Dispose();

            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 1),
            };
            var vertexCount = verts.Length;
            var trisCount = triangles.Length;
            //Verts
            mesh.SetVertexBufferParams(vertexCount, layout);
            mesh.SetVertexBufferData(verts.AsArray(), 0, 0, vertexCount);

            // Tris
            mesh.SetIndexBufferParams(trisCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(triangles.AsArray(), 0, 0, trisCount);

            mesh.SetSubMesh(0, new SubMeshDescriptor(0, trisCount), MeshUpdateFlags.DontRecalculateBounds);
            mesh.bounds = new Bounds(new Vector3(VoxelEngineConstants.CHUNK_VOXEL_SIZE /2f, VoxelEngineConstants.CHUNK_VOXEL_SIZE /2f, VoxelEngineConstants.CHUNK_VOXEL_SIZE /2f), new Vector3(VoxelEngineConstants.CHUNK_VOXEL_SIZE, VoxelEngineConstants.CHUNK_VOXEL_SIZE , VoxelEngineConstants.CHUNK_VOXEL_SIZE ));

            return mesh;
        }
    }
}