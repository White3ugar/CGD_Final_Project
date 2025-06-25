using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ArgumentSpawner : MonoBehaviourPun
{
    public Transform[] spawnPoints; // Assign in inspector
    public GameObject collectablePrefab; // Assign prefab in Inspector

   public void SpawnPlayerCollectables(List<ArgumentSet> argumentSets)
    {
        int count = Mathf.Min(argumentSets.Count, spawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = spawnPoints[i].position;
            GameObject go = PhotonNetwork.Instantiate(collectablePrefab.name, spawnPos, Quaternion.identity);

            // Only the local player owns this, so this will succeed
            ArgumentCollectable ac = go.GetComponent<ArgumentCollectable>();
            ac.Initialize(argumentSets[i].supportingArguments, argumentSets[i].refutingArguments);

            PhotonView pv = go.GetComponent<PhotonView>();
            Debug.Log($"📦 Collectable {i + 1} created at {spawnPos} by {PhotonNetwork.LocalPlayer.NickName} (Owner: {pv.Owner?.NickName ?? "Unknown"}, Actor #{pv.OwnerActorNr})");
        }

        if (argumentSets.Count > spawnPoints.Length)
        {
            Debug.LogWarning($"⚠️ Not enough spawn points! {argumentSets.Count} collectables requested, but only {spawnPoints.Length} spawn points provided.");
        }
    }
}