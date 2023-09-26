using System.Numerics;
using RaytracingVulkan.Primitives;
using Silk.NET.Assimp;

namespace RaytracingVulkan;

public static unsafe class MeshImporter
{
    private static readonly Assimp Assimp = Assimp.GetApi();
    public static Mesh[] FromFile(string filename)
    {
        var pScene = Assimp.ImportFile(filename, (uint) PostProcessPreset.TargetRealTimeFast);
        var meshes = VisitNode(pScene->MRootNode, pScene);
        Assimp.ReleaseImport(pScene);
        return meshes.ToArray();
    }

    private static List<Mesh> VisitNode(Node* pNode, Scene* pScene)
    {
        var meshes = new List<Mesh>();
        for (var m = 0; m < pNode->MNumMeshes; m++)
        {
            var pMesh = pScene->MMeshes[pNode->MMeshes[m]];
            meshes.Add(VisitMesh(pMesh));
        }
        for(var i = 0; i < pNode->MNumChildren; i++) meshes.AddRange(VisitNode(pNode->MChildren[i], pScene));
        return meshes;
    }

    private static Mesh VisitMesh(Silk.NET.Assimp.Mesh* pMesh)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        for (var i = 0; i < pMesh->MNumVertices; i++)
        {
            var vertex = new Vertex
            {
                Position = pMesh->MVertices[i]
            };
            if (pMesh->MNormals != null) vertex.Normal = pMesh->MNormals[i];
            if (pMesh->MTextureCoords[0] != null)
            {
                var pTex3 = pMesh->MTextureCoords[0][i];
                vertex.TextureCoordinate = new Vector2(pTex3.X, pTex3.Y);
            }
            vertices.Add(vertex);
        }

        for (var j = 0; j < pMesh->MNumFaces; j++)
        {
            var face = pMesh->MFaces[j];
            for (uint i = 0; i < face.MNumIndices; i++) indices.Add(face.MIndices[i]);
        }

        return new Mesh {Vertices = vertices.ToArray(), Indices = indices.ToArray()};
    }
}
