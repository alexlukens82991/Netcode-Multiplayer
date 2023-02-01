using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerItemSpawner : NetworkBehaviour
{
    [SerializeField] private Transform m_Player;
    [SerializeField] private Transform m_SpawnPoint;
    [SerializeField] private Transform m_Sights;
    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnItemData spawnItemData = new();

            spawnItemData._hasVelocity = true;

            spawnItemData.SetPosition(m_SpawnPoint.position);
            spawnItemData.SetRotation(transform.rotation);

            Vector3 veloDir = (m_Sights.position - m_SpawnPoint.position).normalized;

            veloDir *= 10;

            print("Final velo: " + veloDir);

            spawnItemData.SetVelocity(veloDir);

            NetworkSpawner.Instance.SpawnItemServerRpc(0, spawnItemData);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnItemNoVelo(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SpawnItemNoVelo(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SpawnItemNoVelo(3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SpawnItemNoVelo(4);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SpawnItemNoVelo(5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SpawnItemNoVelo(6);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SpawnItemNoVelo(7);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SpawnItemNoVelo(8);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SpawnItemNoVelo(9);
        }


    }

    private void SpawnItemNoVelo(int itemInt)
    {
        SpawnItemData spawnItemData = new();

        Vector3 zerodY = new(m_SpawnPoint.position.x, m_Player.position.y, m_SpawnPoint.position.z);

        spawnItemData.SetPosition(zerodY);

        NetworkSpawner.Instance.SpawnItemServerRpc(itemInt, spawnItemData);
    }
}
