using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodMoonSkipperMod : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        try
        {
            Debug.Log(" [BloodMoonSkipper] Mod loading...");
            
            // Initialize vote manager on a delay
            ThreadManager.StartCoroutine(InitializeVoteManager());
            
            Debug.Log(" [BloodMoonSkipper] Mod loaded successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format(" [BloodMoonSkipper] Init failed: {0}", e.Message));
            Debug.LogError(e.StackTrace);
        }
    }

    private System.Collections.IEnumerator InitializeVoteManager()
    {
        yield return new WaitForSeconds(2f);
        
        try
        {
            GameObject go = new GameObject("BloodMoonVoteManager");
            go.AddComponent<BloodMoonVoteManager>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            
            Debug.Log(" [BloodMoonSkipper] Vote manager initialized!");
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format(" [BloodMoonSkipper] Vote manager init failed: {0}", e.Message));
        }
    }
}

public class ConsoleCmdSkipBloodMoon : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "skipbloodmoon", "skipbm" };
    }

    public override string getDescription()
    {
        return "Skips the next blood moon";
    }
    
    public override string getHelp()
    {
        return "Usage: skipbloodmoon\nSkips the next scheduled blood moon horde night.\nSingle player: Instant skip\nMultiplayer: Initiates a 30-second vote";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            World world = GameManager.Instance.World;
            if (world == null)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Error: World not found.");
                return;
            }

            int frequency = GamePrefs.GetInt(EnumGamePrefs.BloodMoonFrequency);
            if (frequency <= 0)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Blood moons are disabled.");
                return;
            }

            // Count online players
            List<EntityPlayer> players = world.GetPlayers();
            int onlineCount = 0;
            foreach (EntityPlayer player in players)
            {
                if (player != null && player.IsAlive())
                {
                    onlineCount++;
                }
            }

            // Single player - skip immediately
            if (onlineCount <= 1)
            {
                SkipBloodMoonInternal();
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Blood moon skipped (solo player).");
            }
            else
            {
                // Multiplayer - start vote
                BloodMoonVoteManager manager = BloodMoonVoteManager.Instance;
                if (manager == null)
                {
                    SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Error: Vote manager not found.");
                    return;
                }

                if (manager.IsVoteActive())
                {
                    SingletonMonoBehaviour<SdtdConsole>.Instance.Output("A vote is already in progress.");
                    return;
                }

                manager.StartVote();
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Blood moon skip vote initiated.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format(" [BloodMoonSkipper] Error: {0}", e.Message));
        }
    }

    public static void SkipBloodMoonInternal()
    {
        try
        {
            int currentDay = GameStats.GetInt(EnumGameStats.BloodMoonDay);
            int frequency = GamePrefs.GetInt(EnumGamePrefs.BloodMoonFrequency);

            int daysUntil = frequency - (currentDay % frequency);
            if (daysUntil == frequency) daysUntil = 0;
            
            int newDay = currentDay + daysUntil + 1;
            GameStats.Set(EnumGameStats.BloodMoonDay, newDay);
            
            int nextBM = currentDay + daysUntil + frequency;
            
            GameManager.Instance.ChatMessageServer(
                null,
                EChatType.Global,
                -1,
                string.Format("[FF8800]Blood moon skipped! Next blood moon on day {0}[-]", nextBM),
                null,
                EMessageSender.Server
            );
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format(" [BloodMoonSkipper] Error in SkipBloodMoon: {0}", e.Message));
        }
    }
}

public class ConsoleCmdVoteMoon : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "votemoon" };
    }

    public override string getDescription()
    {
        return "Vote yes or no on blood moon skip";
    }
    
    public override string getHelp()
    {
        return "Usage: votemoon <yes|no>\nVote on whether to skip the next blood moon.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            BloodMoonVoteManager manager = BloodMoonVoteManager.Instance;
            if (manager == null || !manager.IsVoteActive())
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No active vote.");
                return;
            }

            if (_params.Count < 1)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Usage: votemoon <yes|no>");
                return;
            }

            string voteStr = _params[0].ToLower();
            bool vote;
            
            if (voteStr == "yes" || voteStr == "y")
            {
                vote = true;
            }
            else if (voteStr == "no" || voteStr == "n")
            {
                vote = false;
            }
            else
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Invalid vote. Use 'yes' or 'no'.");
                return;
            }

            // Get the player entity ID from the sender info
            EntityPlayer player = GameManager.Instance.World.GetEntity(_senderInfo.RemoteClientInfo.entityId) as EntityPlayer;
            if (player != null)
            {
                manager.RegisterVote(player.entityId, vote);
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output(string.Format("Vote recorded: {0}", vote ? "YES" : "NO"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format(" [BloodMoonSkipper] Vote error: {0}", e.Message));
        }
    }
}

public class BloodMoonVoteManager : MonoBehaviour
{
    private static BloodMoonVoteManager instance;
    public static BloodMoonVoteManager Instance
    {
        get { return instance; }
    }

    private bool voteActive = false;
    private float voteStartTime;
    private const float VOTE_DURATION = 30f;
    private Dictionary<int, bool> votes = new Dictionary<int, bool>();
    private HashSet<int> eligibleVoters = new HashSet<int>();
    private GameObject voteUI;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (voteActive)
        {
            float elapsed = Time.time - voteStartTime;
            if (elapsed >= VOTE_DURATION)
            {
                EndVote();
            }
        }
    }

    public bool IsVoteActive()
    {
        return voteActive;
    }

    public void StartVote()
    {
        voteActive = true;
        voteStartTime = Time.time;
        votes.Clear();
        eligibleVoters.Clear();

        World world = GameManager.Instance.World;
        if (world != null)
        {
            List<EntityPlayer> players = world.GetPlayers();
            foreach (EntityPlayer player in players)
            {
                if (player != null && player.IsAlive())
                {
                    eligibleVoters.Add(player.entityId);
                }
            }

            GameManager.Instance.ChatMessageServer(
                null,
                EChatType.Global,
                -1,
                "[FFFF00]Vote: Skip the next blood moon? You have 30 seconds to vote![-]",
                null,
                EMessageSender.Server
            );

            GameManager.Instance.ChatMessageServer(
                null,
                EChatType.Global,
                -1,
                "[FFFF00]Press F1 and type 'votemoon yes' or 'votemoon no'![-]",
                null,
                EMessageSender.Server
            );

            // Show UI on local player
            ShowVoteUI();

            Debug.Log(string.Format(" [BloodMoonSkipper] Vote started with {0} eligible voters", eligibleVoters.Count));
        }
    }

    public void RegisterVote(int entityId, bool vote)
    {
        if (!voteActive || !eligibleVoters.Contains(entityId)) return;
        
        votes[entityId] = vote;
        Debug.Log(string.Format(" [BloodMoonSkipper] Vote registered: Entity {0} voted {1}", entityId, vote ? "YES" : "NO"));
    }

    private void EndVote()
    {
        voteActive = false;

        int yesVotes = 0;
        int noVotes = 0;

        foreach (var vote in votes)
        {
            if (vote.Value) yesVotes++;
            else noVotes++;
        }

        int didNotVote = eligibleVoters.Count - votes.Count;
        bool passed = yesVotes > noVotes;

        HideVoteUI();

        string resultMessage;
        if (passed)
        {
            resultMessage = string.Format(
                "[00FF00]Vote PASSED! ({0} Yes, {1} No, {2} Abstained) - Skipping blood moon.[-]",
                yesVotes, noVotes, didNotVote
            );
            ConsoleCmdSkipBloodMoon.SkipBloodMoonInternal();
        }
        else
        {
            resultMessage = string.Format(
                "[FF0000]Vote FAILED! ({0} Yes, {1} No, {2} Abstained) - Blood moon will occur.[-]",
                yesVotes, noVotes, didNotVote
            );
        }

        GameManager.Instance.ChatMessageServer(
            null,
            EChatType.Global,
            -1,
            resultMessage,
            null,
            EMessageSender.Server
        );

        votes.Clear();
        eligibleVoters.Clear();
    }

    private void ShowVoteUI()
    {
        if (voteUI != null) return;

        GameObject canvasGO = new GameObject("VoteCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        voteUI = new GameObject("VotePanel");
        voteUI.transform.SetParent(canvasGO.transform, false);
        
        Image panelImage = voteUI.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        RectTransform panelRect = voteUI.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 300);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText(voteUI.transform, "QuestionText", "Skip the next Blood Moon?", 32, new Vector2(0, 80));
        CreateText(voteUI.transform, "InstructionText", "Press F1 and type 'votemoon yes' or 'votemoon no'", 20, new Vector2(0, 20));

        CreateButton(voteUI.transform, "YES", new Color(0.2f, 0.8f, 0.2f), new Vector2(-150, -50), () => CastVote(true));
        CreateButton(voteUI.transform, "NO", new Color(0.8f, 0.2f, 0.2f), new Vector2(150, -50), () => CastVote(false));
    }

    private void HideVoteUI()
    {
        if (voteUI != null)
        {
            Destroy(voteUI.transform.parent.gameObject);
            voteUI = null;
        }
    }

    private GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;
        
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560, 60);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        
        return textGO;
    }

    private void CreateButton(Transform parent, string label, Color color, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(label + "Button");
        buttonGO.transform.SetParent(parent, false);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);
        
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 60);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        Text buttonText = textGO.AddComponent<Text>();
        buttonText.text = label;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 28;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }

    private void CastVote(bool vote)
    {
        EntityPlayerLocal localPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        if (localPlayer != null)
        {
            RegisterVote(localPlayer.entityId, vote);
            
            // Update UI to show vote was cast
            Transform questionTransform = voteUI.transform.Find("QuestionText");
            if (questionTransform != null)
            {
                Text questionText = questionTransform.GetComponent<Text>();
                if (questionText != null)
                {
                    questionText.text = string.Format("You voted: {0}", vote ? "YES" : "NO");
                    questionText.color = vote ? Color.green : Color.red;
                }
            }
        }
    }
}
