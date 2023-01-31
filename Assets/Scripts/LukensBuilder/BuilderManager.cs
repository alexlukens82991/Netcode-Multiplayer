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
    public int m_CurrentGhostInt;
    public List<NetworkObject> m_Ghosts;
    public List<Transform> m_GhostPrefabs;


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
        List<ulong> spawned = new();
        foreach (Transform item in m_GhostPrefabs)
        {
            Transform newObj = Instantiate(item);

            NetworkObject newNetworkObj = newObj.GetComponent<NetworkObject>();
            
            newNetworkObj.SpawnWithOwnership(id);
            spawned.Add(newNetworkObj.NetworkObjectId);
        }


        SaveSpawnedRefsClientRpc(spawned.ToArray(), id);
    }

    [ClientRpc]
    public void SaveSpawnedRefsClientRpc(ulong[] spawnedObjects, ulong id)
    {
        m_Ghosts.Clear();

        foreach (ulong item in spawnedObjects)
        {
            try
            {
                NetworkObject foundObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[item];
                m_Ghosts.Add(foundObj);
            }
            catch
            {
                Debug.LogError("yep");
            }
        }


        //List<NetworkObject> foundObjs = new();
        //print("Saving spawned refs");
        //foreach (ulong item in spawnedObjects)
        //{
        //    NetworkObject foundObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[item];

        //    if (foundObj)
        //    {
        //        print("FOUND OBJECT!");
        //    }
        //    else
        //    {
        //        print("DID NOT FIND OBJECT");
        //    }


        //    foundObjs.Add(foundObj);
        //}

        //NetworkClient foundClient = NetworkManager.Singleton.ConnectedClients[id];

        //foundClient.PlayerObject.GetComponent<BuilderManager>().m_Ghosts = foundObjs;
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
        }
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


                string[] ghostExpanded = m_Ghosts[m_CurrentGhostInt].name.Split('_');
                string[] foundExpanded = foundObject.name.Split('_');

                if (ghostExpanded[0].Equals(foundExpanded[0]))
                {
                    m_Ghosts[0].transform.SetPositionAndRotation(foundObject.transform.position, foundObject.transform.rotation);
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
        PosAndRotData newData = new();

        newData._xPos = m_Ghosts[m_CurrentGhostInt].transform.position.x;
        newData._yPos = m_Ghosts[m_CurrentGhostInt].transform.position.y;
        newData._zPos = m_Ghosts[m_CurrentGhostInt].transform.position.z;

        newData._xRot = m_Ghosts[m_CurrentGhostInt].transform.rotation.x;
        newData._yRot = m_Ghosts[m_CurrentGhostInt].transform.rotation.y;
        newData._zRot = m_Ghosts[m_CurrentGhostInt].transform.rotation.z;
        newData._wRot = m_Ghosts[m_CurrentGhostInt].transform.rotation.w;

        SpawnWallServerRpc(OwnerClientId, newData);

    }


    [ServerRpc]
    private void SpawnWallServerRpc(ulong id, PosAndRotData posAndRotData)
    {
        string[] ghostExpaned = m_Ghosts[m_CurrentGhostInt].name.Split('_');
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
