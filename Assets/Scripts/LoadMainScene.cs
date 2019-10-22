using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LoadMainScene : MonoBehaviour
{
    private void Start() 
    {
        /* Jump through hoops: according to a Unity forum message, the first scene
         * is not preloaded while all other scenes are.  So our main "Scene MineSweeper"
         * should not be the first scene... */
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Scene MineSweeper");
    }
}
