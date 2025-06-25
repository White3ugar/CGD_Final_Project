using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    void Start()
    {
        AddCollidersToModel("LibraryModel");
        AddCollidersToModel("courtRoom");
        AddCollidersToModel("ClassLobby");
    }

    void AddCollidersToModel(string modelName)
    {
        GameObject modelRoot = GameObject.Find(modelName);

        if (modelRoot == null)
        {
            Debug.LogWarning($"❗ {modelName} not found.");
            return;
        }

        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>())
        {
            if (child.GetComponent<Collider>() != null) continue;

            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Mesh mesh = meshFilter.sharedMesh;

                if (mesh.subMeshCount > 0 &&
                    mesh.GetTopology(0) == MeshTopology.Triangles &&
                    mesh.triangles.Length >= 3)
                {
                    MeshCollider meshCollider = child.gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = false;
                }
            }
        }

        Debug.Log($"✅ Collider setup finished for '{modelName}'. MeshColliders only, no fallback.");
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"🧍 Player collided with '{hit.gameObject.name}' at {hit.point}");
    }
}
