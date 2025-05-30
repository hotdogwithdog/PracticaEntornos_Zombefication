using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Menu;
using Unity.Netcode;
using UnityEngine;

public class LobbyCodesManager : MonoBehaviour
{
    private TMP_InputField _joinCodeInput;
    private TextMeshProUGUI _joinCodeText;

    public string GetJoinCodeInputText() {  return _joinCodeInput.text; }

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            _joinCodeText.gameObject.SetActive(true);
            _joinCodeText.text = MenuManager.Instance.GameManager.JoinCode;
            _joinCodeInput.gameObject.SetActive(false);
        }
        else
        {
            _joinCodeInput.gameObject.SetActive(true);
            _joinCodeText.gameObject.SetActive(false);
        }
    }
}
