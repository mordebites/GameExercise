using UnityEditor;

public class ApplicationManager
{
    public static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
    Application.Quit();
#endif
    }
}
