using Photon.Pun;
using UnityEngine;

public class testScript  : MonoBehaviourPunCallbacks
{
    public override void OnJoinedRoom()
    {
        Debug.Log("✅ PhotonDebug: OnJoinedRoom was triggered.");
    }
}