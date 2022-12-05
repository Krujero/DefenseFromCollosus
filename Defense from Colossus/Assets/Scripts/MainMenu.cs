using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClipboardExtension
{
    /// <summary>
    /// Puts the string into the Clipboard.
    /// </summary>
    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }
}
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject idCode;

    public void CopyToClipboard()
    {
        idCode.GetComponent<TMPro.TMP_Text>().text.CopyToClipboard();
        
    }
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
