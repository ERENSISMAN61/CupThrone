using System.Threading.Tasks;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{

    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

    private async void Start()
    {

        DontDestroyOnLoad(gameObject);

        //LaunchInMode, true for dedicated server, false for client
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
        //nasıl çalışır? graphicsDeviceType null ise dedicated server, değilse client olacak şekilde çalışır.
        //neden graphicsDeviceType null ise dedicated server oluyor? çünkü dedicated server'da grafik işlemleri yoktur.

    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {//true for dedicated server, false for client

        if (isDedicatedServer)
        {

        }
        else
        {

            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();

            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();


            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();//clientSingleton'ın GameManager'ı Menu sahnesine git.
            }
        }
    }

}
