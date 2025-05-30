using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Menu;
using Unity.Netcode;
using UnityEngine;

public class LobbyCodesManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private TextMeshProUGUI _joinCodeText;

    public string GetJoinCodeInputText() {  return _joinCodeInput.text; }

    public void InitHost()
    {
        Debug.Log("HOST CODES MANAGER");
        _joinCodeText.gameObject.SetActive(true);
        _joinCodeText.text = MenuManager.Instance.GameManager.JoinCode;
        _joinCodeInput.gameObject.SetActive(false);
        MenuManager.Instance.GameManager.OnHostInit -= InitHost;
    }

    public void InitClient()
    {
        Debug.Log("CLIENT CODES MANAGER");
        _joinCodeInput.gameObject.SetActive(true);
        _joinCodeText.gameObject.SetActive(false);
    }
}
