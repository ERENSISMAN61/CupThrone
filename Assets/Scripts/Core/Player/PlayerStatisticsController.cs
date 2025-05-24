using Unity.Netcode;
using UnityEngine;

public class PlayerStatisticsController : NetworkBehaviour
{
    public NetworkVariable<int> resourcesCollected = new NetworkVariable<int>();
    public NetworkVariable<int> mobsKilled = new NetworkVariable<int>();
    public NetworkVariable<int> playersKilled = new NetworkVariable<int>();
    public NetworkVariable<int> bossKills = new NetworkVariable<int>();
    public NetworkVariable<int> bossDamage = new NetworkVariable<int>();
    public NetworkVariable<int> chestsOpened = new NetworkVariable<int>();
    public NetworkVariable<int> goldCollected = new NetworkVariable<int>();
    public NetworkVariable<float> distanceTraveled = new NetworkVariable<float>();
    public NetworkVariable<int> deaths = new NetworkVariable<int>();

    private Vector3 lastPosition;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            lastPosition = transform.position;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        // Mesafe takibi
        float dist = Vector3.Distance(transform.position, lastPosition);
        if (dist > 0.01f)
        {
            distanceTraveled.Value += dist;
            lastPosition = transform.position;
        }
    }

    private void OnEnable()
    {
        CupManager.OnGameEnded += OnGameEnded;
    }
    private void OnDisable()
    {
        CupManager.OnGameEnded -= OnGameEnded;
    }
    private void OnGameEnded()
    {
        Debug.Log("Game ended, sending player stats to CupManager...");
        if (IsOwner && GameObject.FindGameObjectWithTag("CupManager") != null)
        {
            // Oyuncu istatistiklerini CupManager'a gönder
            CupManager cupManager = GameObject.FindGameObjectWithTag("CupManager").GetComponent<CupManager>();
            if (cupManager != null)
            {
                cupManager.ReceivePlayerStats(
                OwnerClientId,
                resourcesCollected.Value,
                mobsKilled.Value,
                playersKilled.Value,
                bossKills.Value,
                bossDamage.Value,
                chestsOpened.Value,
                goldCollected.Value,
                distanceTraveled.Value,
                deaths.Value);
            }
        }
        {
        }
    }

    // Diğer istatistikler için public metodlar eklenebilir
    public void AddResource(int amount) => resourcesCollected.Value += amount;
    public void AddMobKill() => mobsKilled.Value++;
    public void AddPlayerKill() => playersKilled.Value++;
    public void AddBossKill() => bossKills.Value++;
    public void AddBossDamage(int amount) => bossDamage.Value += amount;
    public void AddChestOpened() => chestsOpened.Value++;
    public void AddGold(int amount) => goldCollected.Value += amount;
    public void AddDeath() => deaths.Value++;
}

