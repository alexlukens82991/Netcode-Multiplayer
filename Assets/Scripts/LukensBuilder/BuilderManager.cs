using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LukensUtils;
using Unity.Netcode;
using Unity.Netcode.Components;
using SunTemple;

public class BuilderManager : NetworkBehaviour
{
    public bool BuildModeOn;
    public NetworkTransform NetworkTransform;
    public Vector3 StartPosition;

    public NetworkObject m_CurrentActiveBuildGhost;
    public List<Transform> m_Ghosts;
    public List<Transform> m_GhostPrefabs;
    private Dictionary<Transform, NetworkAnimator> m_GhostAnimatorsDictionary = new();

    public NetworkVariable<int> m_CurrentGhostInt = new NetworkVariable<int>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private Camera m_PlayerCam;

    [Header("Cache")]
    [SerializeField] private Transform[] m_FinishedPrefabs;
    [SerializeField] private Transform m_Wall_Plank;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        NetworkTransform.SetState(StartPosition);

        Initialize();

        StartCoroutine(RaycastRoutine());
    }

    private void Initialize()
    {
        print("Local intialize");
        Door[] doors = FindObjectsOfType<Door>();

        foreach (Door door in doors)
        {
            door.SetDoorPlayer(gameObject, m_PlayerCam);
        }

        IntializeServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void IntializeServerRpc(ulong id)
    {
        print("intialize server rpc");

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };

        List<ulong> spawned = new();

        foreach (Transform item in m_GhostPrefabs)
        {
            Transform newObj = Instantiate(item);

            NetworkObject newNetworkObj = newObj.GetComponent<NetworkObject>();
            
            newNetworkObj.SpawnWithOwnership(id);
            spawned.Add(newNetworkObj.NetworkObjectId);
        }

        // call this here because clientRpc wont call it if this is server
        if (IsServer)
        {
            InitializeGhostList(spawned.ToArray(), clientRpcParams);
        }

        UpdateClientInitializedClientRpc(spawned.ToArray(), clientRpcParams);
    }

    [ClientRpc]
    private void UpdateClientInitializedClientRpc(ulong[] ghostTiles, ClientRpcParams clientRpcParams)
    {
        if (IsServer) return;
        InitializeGhostList(ghostTiles, clientRpcParams);
    }

    private void InitializeGhostList(ulong[] ghostTiles, ClientRpcParams clientRpcParams)
    {
        print("INITIALIZED");
        m_Ghosts.Clear();

        foreach (ulong item in ghostTiles)
        {
            NetworkObject foundObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[item];
            m_Ghosts.Add(foundObj.transform);
            NetworkAnimator foundAnimator = foundObj.GetComponent<NetworkAnimator>();
            m_GhostAnimatorsDictionary.Add(foundObj.transform, foundAnimator);
        }
    }


    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            SpawnWall();
        }

        if (Input.GetMouseButtonDown(1))
        {
            //toggle
            ToggleCurrentGhostInt();
        }
    }

    private bool toggleCooled = true;

    private void ToggleCurrentGhostInt()
    {
        if (!IsOwner || !toggleCooled) return;

        print("FIRED");
        toggleCooled = false;
        m_CurrentGhostInt.Value++;

        if (m_CurrentGhostInt.Value == m_Ghosts.Count)
        {
            m_CurrentGhostInt.Value = 0;
        }
        foreach (KeyValuePair<Transform, NetworkAnimator> ghostRef in m_GhostAnimatorsDictionary)
        {
            if (ghostRef.Key.Equals(m_Ghosts[m_CurrentGhostInt.Value]))
            {
                ghostRef.Value.ResetTrigger("Disable");
                ghostRef.Value.SetTrigger("Enable");
            }
            else
            {
                ghostRef.Value.ResetTrigger("Enable");
                ghostRef.Value.SetTrigger("Disable");
            }
        }

        LukensUtilities.DelayedFire(() => toggleCooled = true, 1);

    }

    IEnumerator RaycastRoutine()
    {
        yield return new WaitForSeconds(1);
        do
        {

            GameObject foundObject = LukensUtilities.RaycastFromMouse(100, m_PlayerCam);

            if (foundObject != null && m_Ghosts.Count > 0)
            {
                // show ghost
                //m_CurrentActiveBuildGhost.GetComponent<NetworkObject>().;


                string[] ghostExpanded = m_Ghosts[m_CurrentGhostInt.Value].name.Split('_');
                string[] foundExpanded = foundObject.name.Split('_');

                if (ghostExpanded[0].Equals(foundExpanded[0]))
                {
                    m_Ghosts[m_CurrentGhostInt.Value].transform.SetPositionAndRotation(foundObject.transform.position, foundObject.transform.rotation);
                }
            }
            else
            {
                // hide ghost
            }
            yield return new WaitForFixedUpdate();
        } while (BuildModeOn);
    }

    private void SpawnWall()
    {
        if (m_Ghosts.Count == 0) return;

        PosAndRotData newData = new();
        int currGhost = m_CurrentGhostInt.Value;

        newData._xPos = m_Ghosts[currGhost].position.x;
        newData._yPos = m_Ghosts[currGhost].position.y;
        newData._zPos = m_Ghosts[currGhost].position.z;

        newData._xRot = m_Ghosts[currGhost].rotation.x;
        newData._yRot = m_Ghosts[currGhost].rotation.y;
        newData._zRot = m_Ghosts[currGhost].rotation.z;
        newData._wRot = m_Ghosts[currGhost].rotation.w;

        SpawnWallServerRpc(OwnerClientId, newData);

    }


    [ServerRpc]
    private void SpawnWallServerRpc(ulong id, PosAndRotData posAndRotData)
    {
        if (m_Ghosts.Count == 0)
        {
            Debug.LogWarning("GHOSTS NOT INTIALIZED. ", gameObject);
        }

        string[] ghostExpaned = m_Ghosts[m_CurrentGhostInt.Value].name.Split('_');
        Transform foundTransform = null;
        foreach (Transform item in m_FinishedPrefabs)
        {
            string[] foundExpanded = item.name.Split('_');

            if (foundExpanded[0].Equals(ghostExpaned[0]) && foundExpanded[1].Equals(ghostExpaned[1]))
            {
                foundTransform = item;
            }
        }

        Transform newWall = Instantiate(foundTransform);
        newWall.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

        Vector3 newPos = new(posAndRotData._xPos, posAndRotData._yPos, posAndRotData._zPos);
        Quaternion newRot = new(posAndRotData._xRot, posAndRotData._yRot, posAndRotData._zRot, posAndRotData._wRot);

        //newWall.SetPositionAndRotation(newPos, newRot);
        NetworkTransform foundNetworkTransform = newWall.GetComponent<NetworkTransform>();

        foundNetworkTransform.SetState(newPos, newRot);
    }

    

    public struct PosAndRotData : INetworkSerializable
    {
        public float _xPos;
        public float _yPos;
        public float _zPos;

        public float _xRot;
        public float _yRot;
        public float _zRot;
        public float _wRot;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _xPos);
            serializer.SerializeValue(ref _yPos);
            serializer.SerializeValue(ref _zPos);

            serializer.SerializeValue(ref _xRot);
            serializer.SerializeValue(ref _yRot);
            serializer.SerializeValue(ref _zRot);
            serializer.SerializeValue(ref _wRot);
        }
    }

}
