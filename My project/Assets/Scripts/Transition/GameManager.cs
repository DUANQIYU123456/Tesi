using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameObject playerstats;

    protected override void Awake()
    {
        base.Awake();
        if (playerstats == null)
        {
            playerstats = GameObject.FindWithTag("Player");
        }
    }
}
