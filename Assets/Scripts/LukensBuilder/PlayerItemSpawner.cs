using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerItemSpawner : NetworkBehaviour
{
    [SerializeField] private Transform m_Parent;
    [SerializeField] private Transform m_Sights;
    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SpawnItemData spawnItemData = new();

            spawnItemData.SetPosition(transform.position);
            spawnItemData.SetRotation(transform.rotation);

            Vector3 veloDir = (m_Sights.position - m_Parent.position).normalized;

            print("Sights position: " + m_Sights.position);
            print("m_Parent position: " + m_Parent.position);
            print("Velocity: " + veloDir);

            veloDir *= 10;

            print("Final velo: " + veloDir);

            spawnItemData.SetVelocity(veloDir);

            NetworkSpawner.Instance.SpawnItemServerRpc(0, spawnItemData);
        }
    }
}
