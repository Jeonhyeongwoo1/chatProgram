using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private Text _text;

    public void UpdateUI(string text)
    {
        _text.text = text;
        gameObject.SetActive(true);
    }
}