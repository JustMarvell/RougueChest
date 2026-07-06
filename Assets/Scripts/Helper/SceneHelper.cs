using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHelper : MonoBehaviour
{
    public void ReloadScene()
    {
        Debug.Log("Reload Game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void TPToWhite()
    {
        Vector3 position = new Vector3(3.5f, 8f, -3f);
        Quaternion rotation = Quaternion.Euler(new Vector3(55f, 0f, 0f));
        Camera.main.transform.SetPositionAndRotation(position, rotation);
    }

    public void TPToBlack()
    {
        Vector3 position = new Vector3(3.5f, 8f, 9f);
        Quaternion rotation = Quaternion.Euler(new Vector3(55f, -180f, 0f));
        Camera.main.transform.SetPositionAndRotation(position, rotation);
    }

    public void TPToTOP()
    {
        Vector3 position = new Vector3(3.5f, 10f, 3.5f);
        Quaternion rotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
        Camera.main.transform.SetPositionAndRotation(position, rotation);
    }

    public void TPToLeft()
    {
        Vector3 position = new Vector3(-3f, 8f, 3.5f);
        Quaternion rotation = Quaternion.Euler(new Vector3(55f, 90f, 0f));
        Camera.main.transform.SetPositionAndRotation(position, rotation);
    }

    public void TPToRight()
    {
        Vector3 position = new Vector3(9f, 8f, 3.5f);
        Quaternion rotation = Quaternion.Euler(new Vector3(55f, -90f, 0f));
        Camera.main.transform.SetPositionAndRotation(position, rotation);
    }
}
