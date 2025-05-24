using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Collections;


public class CupManager : NetworkBehaviour
{
    public static event System.Action OnGameEnded;
    private bool gameEnded = false;
    private List<PlayerStatsData> receivedStats = new();

    private void OnEnable()
    {
        OnGameEnded += StartCheckGameEndCoroutine;
        BossEnemy.OnBossDefeated += HandleBossDefeated;
    }
    private void OnDisable()
    {
        OnGameEnded -= StartCheckGameEndCoroutine;
        BossEnemy.OnBossDefeated -= HandleBossDefeated;
    }


    public class PlayerResult
    {
        public ulong clientId;
        public int cupCount;
        public string playerName;
        public Dictionary<string, int> earnedAchievements = new();
    }

    public enum AchievementType
    {
        MostResources, MostMobs, MostPlayers, BossKiller, MostBossDamage,
        MostChests, MostGold, MostDistance, MostDeaths, LeastGold, LeastDistance, LeastResources
    }

    private static readonly List<(AchievementType, int)> AchievementRewards = new()
    {
        (AchievementType.MostResources, 1),
        (AchievementType.MostMobs, 2),
        (AchievementType.MostPlayers, 2),
        (AchievementType.BossKiller, 3),
        (AchievementType.MostBossDamage, 2),
        (AchievementType.MostChests, 1),
        (AchievementType.MostGold, 1),
        (AchievementType.MostDistance, 1),
        (AchievementType.MostDeaths, -2),
        (AchievementType.LeastGold, -1),
        (AchievementType.LeastDistance, -1),
        (AchievementType.LeastResources, -1)
    };

    public List<PlayerResult> LastResults { get; private set; } = new();


    private void HandleBossDefeated(BossEnemy boss)
    {
        Debug.Log("CupManager: Boss defeated, checking for game end...");
        if (!gameEnded && IsServer)
        {
            gameEnded = true;
            OnGameEnded?.Invoke(); // Oyun bitti eventi
            receivedStats.Clear();
        }
    }
    public void ReceivePlayerStats(
        ulong clientId,
        int resourcesCollected,
        int mobsKilled,
        int playersKilled,
        int bossKills,
        int bossDamage,
        int chestsOpened,
        int goldCollected,
        float distanceTraveled,
        int deaths)
    {
        if (!IsServer) return;
        if (receivedStats.Any(x => x.clientId == clientId)) return;
        receivedStats.Add(new PlayerStatsData
        {
            clientId = clientId,
            resourcesCollected = resourcesCollected,
            mobsKilled = mobsKilled,
            playersKilled = playersKilled,
            bossKills = bossKills,
            bossDamage = bossDamage,
            chestsOpened = chestsOpened,
            goldCollected = goldCollected,
            distanceTraveled = distanceTraveled,
            deaths = deaths
        });

        Debug.Log($"CupManager: Received stats for client {clientId}. Total players: {receivedStats.Count}");
    }

    public struct PlayerStatsData
    {
        public ulong clientId;
        public int resourcesCollected;
        public int mobsKilled;
        public int playersKilled;
        public int bossKills;
        public int bossDamage;
        public int chestsOpened;
        public int goldCollected;
        public float distanceTraveled;
        public int deaths;
    }

    public void DistributeCupsAtGameEnd(List<PlayerStatsData> players)
    {
        if (!IsServer) return;
        // 5 rastgele başarı seç
        var selected = AchievementRewards.OrderBy(x => Random.value).Take(5).ToList();
        var results = new List<PlayerResult>();
        foreach (var p in players)
        {
            results.Add(new PlayerResult { clientId = p.clientId, cupCount = 0, playerName = "Unknown" });
        }
        // Her başarıma göre kupaları dağıt
        foreach (var (type, reward) in selected)
        {
            switch (type)
            {
                case AchievementType.MostResources:
                    GiveToMax(players, results, p => p.resourcesCollected, reward, type);
                    break;
                case AchievementType.MostMobs:
                    GiveToMax(players, results, p => p.mobsKilled, reward, type);
                    break;
                case AchievementType.MostPlayers:
                    GiveToMax(players, results, p => p.playersKilled, reward, type);
                    break;
                case AchievementType.BossKiller:
                    GiveToMax(players, results, p => p.bossKills, reward, type);
                    break;
                case AchievementType.MostBossDamage:
                    GiveToMax(players, results, p => p.bossDamage, reward, type);
                    break;
                case AchievementType.MostChests:
                    GiveToMax(players, results, p => p.chestsOpened, reward, type);
                    break;
                case AchievementType.MostGold:
                    GiveToMax(players, results, p => p.goldCollected, reward, type);
                    break;
                case AchievementType.MostDistance:
                    GiveToMax(players, results, p => (int)p.distanceTraveled, reward, type);
                    break;
                case AchievementType.MostDeaths:
                    GiveToMax(players, results, p => p.deaths, reward, type);
                    break;
                case AchievementType.LeastGold:
                    GiveToMin(players, results, p => p.goldCollected, reward, type);
                    break;
                case AchievementType.LeastDistance:
                    GiveToMin(players, results, p => (int)p.distanceTraveled, reward, type);
                    break;
                case AchievementType.LeastResources:
                    GiveToMin(players, results, p => p.resourcesCollected, reward, type);
                    break;
            }
        }
        // Sıralama ve birinciyi bulma
        var sorted = results.OrderByDescending(r => r.cupCount).ToList();
        LastResults = sorted;
        ulong winner = sorted.First().clientId;
        Debug.Log($"Winner: {sorted.First().playerName} (ID: {winner}), Cups: {sorted.First().cupCount}");
        Debug.Log("--- CUP RANKINGS ---");
        int rank = 1;
        foreach (var r in sorted)
        {
            Debug.Log($"{rank}. {r.playerName} (ID: {r.clientId}) - {r.cupCount} cups");
            rank++;
        }
        // Burada UI veya başka sistemlere bildirim yapılabilir
    }

    private void GiveToMax(List<PlayerStatsData> players, List<PlayerResult> results, System.Func<PlayerStatsData, int> selector, int reward, AchievementType type)
    {
        int max = players.Max(selector);
        foreach (var (p, r) in players.Zip(results, (p, r) => (p, r)))
        {
            if (selector(p) == max)
            {
                r.cupCount += reward;
                r.earnedAchievements[type.ToString()] = reward;
            }
        }
    }
    private void GiveToMin(List<PlayerStatsData> players, List<PlayerResult> results, System.Func<PlayerStatsData, int> selector, int reward, AchievementType type)
    {
        int min = players.Min(selector);
        foreach (var (p, r) in players.Zip(results, (p, r) => (p, r)))
        {
            if (selector(p) == min)
            {
                r.cupCount += reward;
                r.earnedAchievements[type.ToString()] = reward;
            }
        }
    }

    private void StartCheckGameEndCoroutine()
    {
        if (gameEnded || !IsServer) return;
        StartCoroutine(CheckGameEndCoroutine());
    }
    private IEnumerator CheckGameEndCoroutine()
    {
        if (!IsServer) yield break;
        Debug.Log("CupManager: Game ended, checking for player statistics...");
        yield return new WaitForSeconds(1f); // Oyun bitişi için kısa bir gecikme
        if (receivedStats.Count > 0)
        {
            DistributeCupsAtGameEnd(receivedStats);
        }
        else
        {
            Debug.LogWarning("No player statistics received at game end.");
        }
    }

}
