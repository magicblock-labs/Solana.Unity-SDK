using TMPro;
using UnityEngine;

public class DropdownDefaultValue : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int rpcDefault = PlayerPrefs.GetInt("rpcCluster", 0);
        GetComponent<TMP_Dropdown>().value = rpcDefault;
    }
}
