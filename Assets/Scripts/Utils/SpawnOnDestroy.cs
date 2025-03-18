using UnityEngine;

public class SpawnOnDestroy : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    private void OnDestroy()
    {

        Instantiate(prefab, transform.position, prefab.transform.rotation);
        //Quatenion.identity, prefabin transform rotasyonunu sıfırlar. 0,0,0 yapar.
    }
}
