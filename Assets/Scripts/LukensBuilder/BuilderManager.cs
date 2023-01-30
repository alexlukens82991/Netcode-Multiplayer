using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LukensUtils;
using Unity.Netcode;

public class BuilderManager : NetworkBehaviour
{
    public bool BuildModeOn;

    public GameObject m_CurrentActiveBuildGhost;
    [SerializeField] private Camera m_PlayerCam;

    [Header("Cache")]
    [SerializeField] private Transform m_Wall_Plank;

    private void Start()
    {
        if (IsOwner)
            StartCoroutine(RaycastRoutine());
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            SpawnWall();
        }
    }

    IEnumerator RaycastRoutine()
    {
        do
        {
            GameObject foundObject = LukensUtilities.RaycastFromMouse(100, m_PlayerCam);

            if (foundObject != null)
            {
                m_CurrentActiveBuildGhost.SetActive(true);

                string[] ghostExpanded = m_CurrentActiveBuildGhost.name.Split('_');
                string[] foundExpanded = foundObject.name.Split('_');

                if (ghostExpanded[0].Equals(foundExpanded[0]))
                {
                    m_CurrentActiveBuildGhost.transform.SetPositionAndRotation(foundObject.transform.position, foundObject.transform.rotation);
                }
            }
            else
            {
                m_CurrentActiveBuildGhost.SetActive(false);
            }
            yield return new WaitForFixedUpdate();
        } while (BuildModeOn);
    }

    private void SpawnWall()
    {
        PosAndRotData newData = new();

        newData._xPos = m_CurrentActiveBuildGhost.transform.position.x;
        newData._yPos = m_CurrentActiveBuildGhost.transform.position.y;
        newData._zPos = m_CurrentActiveBuildGhost.transform.position.z;

        newData._xRot = m_CurrentActiveBuildGhost.transform.rotation.x;
        newData._yRot = m_CurrentActiveBuildGhost.transform.rotation.y;
        newData._zRot = m_CurrentActiveBuildGhost.transform.rotation.z;
        newData._wRot = m_CurrentActiveBuildGhost.transform.rotation.w;

        SpawnWallServerRpc(OwnerClientId, newData);

    }

    [ServerRpc]
    private void SpawnWallServerRpc(ulong id, PosAndRotData posAndRotData)
    {
        Transform newWall = Instantiate(m_Wall_Plank);
        newWall.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

        Vector3 newPos = new(posAndRotData._xPos, posAndRotData._yPos, posAndRotData._zPos);
        Quaternion newRot = new(posAndRotData._xRot, posAndRotData._yRot, posAndRotData._zRot, posAndRotData._wRot);

        newWall.SetPositionAndRotation(newPos, newRot);
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
