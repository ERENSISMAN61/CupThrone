using UnityEngine;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Core;

public static class AuthenticationWrapper//doğrulama işlemlerini yöneten sınıf
{
    //doğrulanma durumu. varsayılan olarak doğrulanmamış
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> DoAuth(int maxRetries = 5) // Doğrulamayı başlat. max 5 deneme yapacak.
    {
        if (AuthState == AuthState.Authenticated)// eğer zaten doğrulanmışsa mevcut durumu döndür.
        {
            return AuthState;//mevcut durumu döndür.
        }

        if (AuthState == AuthState.Authenticating)// eğer zaten doğrulanıyorsa uyarı ver.
        {

            Debug.LogWarning("Already authenticating!");

            await Authenticating();

            return AuthState;
        }

        await SignInAnonymouslyAsync(maxRetries); //anonim olarak giriş yap.

        return AuthState;
    }

    private static async Task<AuthState> Authenticating() //doğrulanıyor durumunda olduğu sürece bekle.
    {
        while (AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated) // doğrulanıyor veya doğrulanmamış durumunda olduğu sürece bekle.
        {
            await Task.Delay(200);
        }

        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries)
    {

        AuthState = AuthState.Authenticating; //durumu doğrulanıyor olarak ayarla.


        int retries = 0;// deneme sayımızı sıfırla.

        while (AuthState == AuthState.Authenticating && retries < maxRetries) // doğrulanıyor durumunda ise ve deneme sayısı max deneme sayısından küçük olduğu durumda çalıştır.
        {
            try // hata durumunda yakalamak için try-catch bloğu kullan. try ile başlayan blok içinde hata olabilecek kodlar yazılır. ve çalıştırılır. Eğer hata olursa catch bloğu çalışır.
            //catch bloğunda hangi hata olduğunu belirten bir parametre alır. Bu parametre ile eşleşirse o catch bloğu çalışır
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); //anonim olarak giriş yapmaya çalış.

                // eğer giriş yapıldıysa ve yetkilendirildiyse doğrulandı olarak ayarla.
                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated; // doğrulandı olarak ayarla.
                    break;
                }
            }                             //"ex" ya da "exception" gibi değişken ismini kafana göre verebilirsin.
            catch (AuthenticationException ex)//AuthenticationException, yani kimlik doğrulama hatası durumunda hata mesajını yazdır.
            {
                Debug.LogError(ex);
                AuthState = AuthState.Error; // hata durumunu ayarla.
            }
            catch (RequestFailedException exception)//RequestFailedException, yani istek başarısız olduğunda hata mesajını yazdır.
            {
                Debug.LogError(exception);
                AuthState = AuthState.Error;
            }

            retries++; // deneme sayısını bir arttır.

            await Task.Delay(1000); // 1 saniye bekle. Çünkü doğrulama işlemi biraz zaman alabilir.

            if (AuthState != AuthState.Authenticated)// eğer doğrulanmadıysa uyarı ver.
            {
                Debug.LogWarning($"Player was not signed insuccessfully after {retries} retries.");// oyuncu, {deneme sayısı} deneme sonrasında başarılı bir şekilde giriş yapamadı.
                AuthState = AuthState.TimeOut; // zaman aşımı durumunu ayarla.
            }

        }

    }
}




public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut
}
