using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance;

    public HostGameManager GameManager { get; private set; }

    public static HostSingleton Instance // neden HostSingleton Instance? Çünkü bu bir singleton sınıfı ve bu sınıfın tek bir örneği olacak.
    {

        get
        {
            if (instance != null) { return instance; }

            instance = FindFirstObjectByType<HostSingleton>();

            if (instance == null)
            {
                Debug.LogError("There is no HostSingleton in the scene!");
                return null;
            }

            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }




    //clientte Initialization yaparken bu metodda initialize yapmıyoruz sadece oluşturuyoruz. bu yüzden async bir fonksiyon olmasına gerek yok. 
    //Oluşturma işi anlık kısa süren bi iş. async kullanmaya gerek yok.
    public void CreateHost() // CreateHost fonksiyonu neden async bir fonksiyon değil? Çünkü bu fonksiyonun içinde await kullanılmıyor.
    //neden await kullanılmıyor? Çünkü bu fonksiyonun içinde asenkron işlemler yapılmıyor. Neden asenkron işlemler yapılmıyor? Çünkü bu fonksiyonun içinde network işlemleri yapılmıyor. !!AI yazdı.!!
    {
        GameManager = new HostGameManager();
        //GameManager = gameObject.AddComponent<HostGameManager>();
    }

    /*     public async Task CreateClient() 
    {
        gameManager = new ClientGameManager();
        await gameManager.InitAsync();
    }
    */

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }

}
