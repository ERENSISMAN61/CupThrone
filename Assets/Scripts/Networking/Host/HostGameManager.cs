using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using System;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Unity.Services.Authentication;
public class HostGameManager : IDisposable
{
    private Allocation allocation;
    private string joinCode;
    private string lobbyId;

    private const int MaxConnections = 20; //const şu işe yarar: değişkenin değeri sabit olarak belirlenir ve değiştirilemez.
    private const string GameSceneName = "OutdoorsScene";
    private NetworkServer networkServer;

    public async Task StartHostAsync()
    {

        try
        {   //relay servisi ile allocation oluşturma
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (RelayServiceException e)//relay servisinden hata alındığında
        {
            Debug.LogError(e);
            return;
        }

        try
        {   //Arkadaşların Join edebilmesi için allocation id'ye göre join code alma
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            //await, async fonksiyonlarının çalışmasını beklemesi gerektiğini belirtir.

            Debug.Log($"Join Code: {joinCode}");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return;
        }

        //NetworkManager'ın UnityTransport bileşenini al
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        //Burda Relay kullanmak için gerekli olan verileri oluşturuyoruz.
        // RelayServerData'yı oluşturuyoruz.
        // Önemli: 5. parametre olarak host bağlantı verisini (allocation.HostConnectionData) kullanmalısınız.
        RelayServerData relayServerData = new RelayServerData(
            allocation.RelayServer.IpV4,             // Relay sunucusunun IP adresi
            (ushort)allocation.RelayServer.Port,       // Relay sunucusunun portu
            allocation.AllocationIdBytes,              // Allocation ID (byte[])
            allocation.ConnectionData,                 // Bağlantı verisi (client için)
            allocation.ConnectionData,                 // Host bağlantı verisi (Allocation'da ayrı veri yok)
            allocation.Key,                            // HMAC anahtarı
            false                                       // isSecure: DTLS (şifreli UDP) kullanılıyorsa true
        );// DTLS güvenli iletişim için isSecure true olarak ayarlandı. Hala UDP kullanıyoruz fakat sifreli UDP. TCP değil.

        //Transport modunu Relay'e ayarlıyoruz.
        transport.SetRelayServerData(relayServerData);



        //--------------------------------
        //Lobby oluşturma
        try
        {
            //Lobby oluşturma için gerekli olan verileri oluşturuyoruz.
            //CreateLobbyOptions: Lobby oluşturma için gerekli olan verileri içerir.
            //IsPrivate: Lobby'nin özel olup olmadığını belirtir.
            //Data: Lobby'ye eklenen verileri içerir.
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false;
            lobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                {//JoinCode: Lobby'ye eklenen verileri içerir.
                    "JoinCode", new DataObject(DataObject.VisibilityOptions.Member,
                    value: joinCode)
                }
            };

            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");

            //Lobby oluşturma
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync($"{playerName}'s Lobby", MaxConnections, lobbyOptions);

            lobbyId = lobby.Id;

            //Lobby'ye heartbeat ping gönderimi. Çünkü Relay'de heartbeat ping gönderimi gerekiyor. 
            //Bu sayede Relay'de lobby'nin aktif olup olmadığını kontrol edebiliriz.
            //Heatbeat ping göndermezsek Relay'de lobby'yi kapatır.
            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
            return;
        }
        //-----------------------------

        networkServer = new NetworkServer(NetworkManager.Singleton);

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };
        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;



        //Hostu Başlatıyoruz.   
        NetworkManager.Singleton.StartHost();


        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        //LoadSceneMode.Single: Bu seçenek, mevcut sahneyi kapatır ve yeni sahneyi yükler.
        //LoadSceneMode.Additive: Bu seçenek, mevcut sahneyi korur ve yeni sahneyi ekler.


    }



    //Lobby'ye heartbeat ping gönderimi
    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {//Lobby'ye heartbeat ping gönderimi. Relay'de heartbeat ping göndermezsek lobby'yi kapatır. 
         //Kim kapatır? Relay mi? Cevap: Relay.
         //lobbynin kullanıldığını bildirmiş oluyoruz 15 saniyede bir
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public async void Dispose()
    {
        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));

        if (!string.IsNullOrEmpty(lobbyId))
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            lobbyId = string.Empty;
        }

        networkServer?.Dispose();
    }

}
