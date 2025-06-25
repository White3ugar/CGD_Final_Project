using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using ExitGames.Client.Photon;

public class SpawnManager : MonoBehaviour
{
    public Transform classroomSpawn;
    public Transform gameRoomSpawn;

    // New spawn points for courtroom
    public Transform Debater1SpawnPoint;
    public Transform Debater2SpawnPoint;
    public Transform VoterSpawnPoint;

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate("Player", classroomSpawn.position, Quaternion.identity);
        }
    }

    public void TeleportToGameRoom()
    {
        PhotonView[] allViews = FindObjectsOfType<PhotonView>();

        foreach (PhotonView view in allViews)
        {
            if (view.IsMine && view.CompareTag("Player"))
            {
                StartCoroutine(TeleportCharacter(view, gameRoomSpawn));
                FindObjectOfType<GameroomManager>()?.TopicSelection();
                return;
            }
        }

        Debug.LogError("❌ No local player found to teleport to GameRoom!");
    }

    public void TeleportToCourtRoom()
    {
        PhotonView[] allViews = FindObjectsOfType<PhotonView>();

        foreach (PhotonView view in allViews)
        {
            if (view.IsMine && view.CompareTag("Player"))
            {
                Player localPlayer = PhotonNetwork.LocalPlayer;
                object roleValue;

                if (localPlayer.CustomProperties.TryGetValue("Role", out roleValue))
                {
                    string role = roleValue.ToString().ToLower();
                    Transform targetSpawn = null;

                    if (role == "debater 1")
                        targetSpawn = Debater1SpawnPoint;
                    else if (role == "debater 2")
                        targetSpawn = Debater2SpawnPoint;
                    else if (role == "voter")
                        targetSpawn = VoterSpawnPoint;
                    else
                        Debug.LogWarning($"⚠ Unknown role: {role}");

                    if (targetSpawn != null)
                    {
                        StartCoroutine(TeleportCharacter(view, targetSpawn));
                    }
                    else
                    {
                        Debug.LogError("❌ No matching spawn point found for role.");
                    }
                }
                else
                {
                    Debug.LogError("❌ Role not found in custom properties.");
                }

                // Disable countdown text
                CourtManager courtManager = FindObjectOfType<CourtManager>();
                if (courtManager != null)
                    courtManager.DisableGameroomCountdownText();
                else
                    Debug.LogWarning("⚠ CourtManager not found in scene.");

                return;
            }
        }

        Debug.LogError("❌ No local player found to teleport to CourtRoom!");
    }

    private IEnumerator TeleportCharacter(PhotonView view, Transform targetSpawn)
    {
        Debug.Log("✅ Preparing to teleport local player...");

        var controller = view.GetComponent<CharacterController>();
        var ptv = view.GetComponent<PhotonTransformView>();
        var movementScript = view.GetComponent<PlayerController>();

        if (ptv != null) ptv.enabled = false;
        if (movementScript != null) movementScript.enabled = false;
        if (controller != null) controller.enabled = false;

        yield return new WaitForFixedUpdate();

        view.transform.position = targetSpawn.position;
        view.transform.rotation = targetSpawn.rotation;

        yield return new WaitForFixedUpdate();

        if (controller != null) controller.enabled = true;
        if (movementScript != null) movementScript.enabled = true;
        if (ptv != null) ptv.enabled = true;

        Debug.Log($"🚩 Teleported to: {targetSpawn.position}");
    }
}
