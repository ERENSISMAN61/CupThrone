using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using System.Text;
using Unity.Services.Authentication;
public class ClientGameManager : IDisposable
{
    private const string MenuSceneName = "Menu";

    private JoinAllocation allocation;
    private NetworkClient networkClient;

    public async Task<bool> InitAsync()// kimlik doğrulama işlemlerini başlat
    {
        //Authenticate player

        await UnityServices.InitializeAsync();//Burada, Unity'nin çevrimiçi hizmetleri başlatılıyor. 
                                              //Bu hizmetler, kimlik doğrulama gibi işlemler için gereklidir.

        networkClient = new NetworkClient(NetworkManager.Singleton);

        AuthState authState = await AuthenticationWrapper.DoAuth();//kimlik doğrulamayı başlat.

        if (authState == AuthState.Authenticated) //eğer doğrulandıysa
        {
            return true;//true döndür.
        }

        return false;//doğrulanamadıysa false döndür.
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return;
        }

        //NetworkManager'ın UnityTransport bileşenini al
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();


        RelayServerData relayServerData = new RelayServerData(
        allocation.RelayServer.IpV4,               // Host IP address (string)
        (ushort)allocation.RelayServer.Port,       // Port (ushort)
        allocation.AllocationIdBytes,               // Allocation ID (byte[])
        allocation.ConnectionData,                  // Connection data (byte[])
        allocation.HostConnectionData,              // Host connection data (byte[])
        allocation.Key,                             // HMAC key (byte[])
        false                                        // isSecure (bool) – true for DTLS
    );



        //Burda Relay kullanmak için gerekli olan verileri oluşturuyoruz.
        //RelayServerData relayServerData = new RelayServerData(
        //     allocation.RelayServer.IpV4,
        //    (ushort)allocation.RelayServer.Port,
        //   allocation.AllocationIdBytes,
        //  allocation.ConnectionData,
        // allocation.ConnectionData,
        //allocation.Key,
        //false);// DTLS güvenli iletişim için isSecure true olarak ayarlandı. Hala UDP kullanıyoruz fakat sifreli UDP. TCP değil.


        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };
        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;


        //Transport modunu Relay'e ayarlıyoruz.
        transport.SetRelayServerData(relayServerData);

        //Clienti Başlatıyoruz.   
        NetworkManager.Singleton.StartClient();

    }

    public void Dispose()
    {
        networkClient?.Dispose();
    }

}
