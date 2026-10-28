using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum TYPE
{
    BLUE = 0,
    GREEN = 1,
    RED = 2
}

public class GameController : MonoBehaviour
{
    [Header("Linked Objects")]
    [SerializeField]
    private GameObject gameContainer;
    [SerializeField]
    private GameObject startPanel;
    [SerializeField] 
    private GameObject gamePanel;
    [SerializeField]
    private GameObject goodJobPanel;
    [SerializeField]
    private GameObject inputNamePanel;
    [SerializeField]
    private GameObject gameOverPanel;
    [SerializeField]
    private TextMeshProUGUI scoreText; 
    [SerializeField]
    private TextMeshPro timerText;
    [SerializeField] 
    private TextPopup popUpText;
    [SerializeField]
    private float gameTimer = 30;


    [SerializeField]
    private float spawnVirusBetweenTime = 2.5f;
    [SerializeField]
    private float spawnAnitBodyBetweenTime = 1.25f;
    private float spawnVirusTimer = 0f;
    private float spawnAnitBodyTimer = 0f;
    [SerializeField]
    public float randomAngle = 10f; // degrees of randomness
    [SerializeField]
    public float speed = 5f;

    int score = 0;
    bool gameStart = false;
    private int currentVirusIndex = 0; // track cycle
    private int currentAntiBodyIndex = 0; // track cycle

    [Header("Game Items")]
    [SerializeField]
    List<GameItem> virusList = new List<GameItem>();
    [SerializeField]
    List<GameItem> blueAntiBodyList = new List<GameItem>();
    [SerializeField]
    List<GameItem> greenAntiBodyList = new List<GameItem>();
    [SerializeField]
    List<GameItem> redAntiBodyList = new List<GameItem>();
    [SerializeField]
    List<GameItem> fullAntiBodyList = new List<GameItem>();
    List<List<GameItem>> antiBodyLists = new List<List<GameItem>>();
    List<List<GameItem>> collectedLists = new List<List<GameItem>>();

    [SerializeField] private float mergeUnitsPerSecond = 8f; // move speed for merge
    [SerializeField] private float mergeMinDuration = 0.15f;
    [SerializeField] private float mergeMaxDuration = 0.75f;
    [SerializeField] private Ease mergeMoveEase = Ease.InOutSine;
    [SerializeField] private Ease mergeScaleEase = Ease.InOutSine;

    // Full Antibody homing config
    [Header("Full Anti Body Setting")]
    [SerializeField] private float fullSeekSpeed = 10f;   // units/sec
    [SerializeField] private float fullTurnRate = 10f;   // how quickly velocity turns toward target
    [SerializeField] private float fullDetectRadius = 50f;   // how far to look for viruses
    [SerializeField] private float fullLifetime = 8f;    // auto self-destruct if it can't find a target
    [SerializeField] private int fullKillScore = 5;     // score per virus destroyed
    [SerializeField] private LayerMask virusLayerMask;        // set this to your "Virus" layer(s)

    [Header("Vignette Effect")]
    [SerializeField] private Volume globalVolume;      // assign your Global Volume
    [SerializeField] private float vigMin = 0.40f;     // pulse range
    [SerializeField] private float vigMax = 0.45f;
    [SerializeField] private float vigHalfCycle = 0.25f; // 0.25s up, 0.25s down

    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private float goodJobStayDuration = 3f;

    private RectTransform goodJobRT;
    private RectTransform gameOverRT;
    private Vector2 goodJobTargetPos;
    private Vector2 gameOverTargetPos;
    private Tween gameOverSeq;

    private float normalScale = 1f;
    private float scaleFactor = 1.1f;
    private float warningScaleFactor = 1.25f;
    private bool warningStarted = false;
    private bool gameOver = false;
    private Tween pulseTween;
    private Tween vignetteTween;
    private Vignette vignette; // URP vignette component from the profile
    private float vignetteBaseIntensity = 0f;
    private Color vignetteBaseColor = Color.black;

    private float initialGameTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPanel.SetActive(true);
        gamePanel.SetActive(false);

        goodJobRT = goodJobPanel.GetComponent<RectTransform>();
        gameOverRT = inputNamePanel.GetComponent<RectTransform>();

        goodJobTargetPos = goodJobRT.anchoredPosition;   // where it sits normally
        gameOverTargetPos = gameOverRT.anchoredPosition;  // where it sits normally

        // Ensure panels start hidden
        goodJobPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        inputNamePanel.SetActive(false);

        score = 0;
        scoreText.text = "Score: " + score.ToString();
        initialGameTimer = gameTimer; // keep the serialized start value

        antiBodyLists.Clear();
        antiBodyLists.Add(blueAntiBodyList);
        antiBodyLists.Add(greenAntiBodyList);
        antiBodyLists.Add(redAntiBodyList);

        //1.BLUE 2.GREEN 3.RED
        collectedLists.Add(new List<GameItem>());
        collectedLists.Add(new List<GameItem>());
        collectedLists.Add(new List<GameItem>());

        // Get Vignette from the volume profile
        if (globalVolume && globalVolume.profile && globalVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            // make sure intensity is actually driven by the profile (override enabled)
            vignette.intensity.overrideState = true;
        }

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.color.overrideState = true; // ensure color is controlled by us too

            // capture starting values from the profile
            vignetteBaseIntensity = vignette.intensity.value;
            vignetteBaseColor = vignette.color.value;

            // start with vignette OFF (no tint visible)
            vignette.intensity.value = 0f;
            vignette.active = false; // disable the effect block entirely until needed
        }
    }

    void OnDisable()
    {
        pulseTween?.Kill();
        vignetteTween?.Kill();
    }

    void GameOver()
    {
        gameOver = true;
        gameStart = false;

        AudioManager.Instance?.StopBackgroundMusic(1);
        AudioManager.Instance?.PlaySFX("Game Win");

        // Stop effects
        pulseTween?.Kill();
        vignetteTween?.Kill();
        timerText.transform.DOKill(false);

        if (vignette != null)
        {
            vignetteTween?.Kill();
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;  // fully off
            vignette.active = false;        // hide the block entirely
        }

        // Hide HUD/gameplay panel
        gamePanel.SetActive(false);

        // Make sure Game Over panel is hidden until after Good Job finishes
        gameOverPanel.SetActive(false);
        inputNamePanel.SetActive(false);
        
        // Build the sequence
        gameOverSeq?.Kill();
        var seq = DOTween.Sequence();

        gameOverPanel.SetActive(true);
        // 1) Slide Good Job up from bottom to its target, stay 3s, then slide back down
        seq.AppendCallback(() =>
        {
            goodJobPanel.SetActive(true);
            goodJobRT.anchoredPosition = OffscreenBottom(goodJobRT, goodJobTargetPos);
        });
        seq.Append(goodJobRT.DOAnchorPos(goodJobTargetPos, slideDuration).SetEase(Ease.OutBack));
        seq.AppendInterval(goodJobStayDuration);
        seq.Append(goodJobRT.DOAnchorPos(OffscreenBottom(goodJobRT, goodJobTargetPos), slideDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() =>
        {
            goodJobPanel.SetActive(false);
        });

        // 2) Slide Game Over panel up from bottom and run its setup
        seq.AppendCallback(() =>
        {
            inputNamePanel.SetActive(true);
            gameOverRT.anchoredPosition = OffscreenBottom(gameOverRT, gameOverTargetPos);
            // call your existing setup when it becomes visible
            var ctrl = gameOverPanel.GetComponent<GameOverPanelController>();
            if (ctrl) 
                ctrl.Setup();
        });
        seq.Append(gameOverRT.DOAnchorPos(gameOverTargetPos, slideDuration).SetEase(Ease.OutCubic));

        gameOverSeq = seq;
    }

    // Update is called once per frame
    void Update()
    {
        if(!gameStart || gameOver)
        {
            return;
        }

        gameTimer -= Time.deltaTime;
        if (gameTimer < 0)
        {
            gameTimer = 0;
            GameOver();
        }
        timerText.text = System.Convert.ToInt32(gameTimer).ToString();

        // Switch to warning mode when 5 seconds remain
        if (!warningStarted && gameTimer <= 5f)
        {
            warningStarted = true;
            StartWarningPulse();
        }

        //Virus Spawning
        spawnVirusTimer += Time.deltaTime;
        if (spawnVirusTimer >= spawnVirusBetweenTime)
        {
            spawnVirusTimer = 0f;

            // Cycle through virus types
            SpawnVirus((TYPE)currentVirusIndex);

            currentVirusIndex++;
            if (currentVirusIndex >= virusList.Count) // loop back
                currentVirusIndex = 0;
        }

        //Antibody Spawning
        spawnAnitBodyTimer += Time.deltaTime;
        if (spawnAnitBodyTimer >= spawnAnitBodyBetweenTime)
        {
            spawnAnitBodyTimer = 0f;
            int r = Random.Range(0, antiBodyLists[currentAntiBodyIndex].Count);
            SpawnObject(antiBodyLists[currentAntiBodyIndex][r], r);

            currentAntiBodyIndex++;
            if (currentAntiBodyIndex >= 3) // loop back
                currentAntiBodyIndex = 0;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);

            // Create a ray from the touch position
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameItem item = hit.transform.GetComponent<GameItem>();
                if (item != null)
                {
                    item.OnTouch();
                }
            }
        }
    }

    public void StartGame()
    {
        startPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameStart = true;

        ReplayGame();
        // Tween localScale up and down forever
        StartNormalPulse();
    }

    void StartWarningPulse()
    {
        // stop the old tween and reset scale so the new one starts cleanly
        pulseTween.Kill();
        timerText.transform.localScale = Vector3.one * normalScale;

        // instantly change to red
        timerText.DOColor(new Color(1.0f,0,0,0.5f), 0.2f);

        // stronger/faster pulse
        pulseTween = timerText.transform
            .DOScale(warningScaleFactor, 0.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);


        if (vignette != null)
        {
            vignetteTween?.Kill();
            vignette.active = true;                 // enable only during warning
            vignette.intensity.value = vigMin;      // start of pulse
            vignetteTween = DOTween.To(
                () => vignette.intensity.value,
                v => vignette.intensity.value = v,
                vigMax,
                vigHalfCycle
            ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    void StartNormalPulse()
    {
        // gentle breathing until warning time
        pulseTween = timerText.transform.DOScale(scaleFactor, 0.5f)
              .SetLoops(-1, LoopType.Yoyo)
              .SetEase(Ease.InOutSine);
    }

    public void UpdateScore(int value, Vector3 pos)
    {
        score += value;
        scoreText.text = "Score: " + score.ToString();

        var obj = Instantiate(popUpText, pos, Quaternion.identity, gameContainer.transform);
        obj.Play("+" + value.ToString());
    }

    public void SpawnVirus(TYPE type)
    {
        SpawnObject(virusList[(int)type], -1, pulseOnSpawn: true);
    }

    void SpawnObject(GameItem item, int variantIndex = -1, bool pulseOnSpawn = false)
    {
        // Placeholder for future object spawning logic
        // Decide which edge to spawn from
        int edge = Random.Range(0, 4); // 0=left, 1=right, 2=top, 3=bottom
        Vector3 spawnScreenPos = Vector3.zero;

        switch (edge)
        {
            case 0: // left edge
                spawnScreenPos = new Vector3(
                    0, // left side
                    Random.Range(0, Screen.height),
                    Camera.main.nearClipPlane + 10f
                );
                break;

            case 1: // right edge
                spawnScreenPos = new Vector3(
                    Screen.width,
                    Random.Range(0, Screen.height),
                    Camera.main.nearClipPlane + 10f
                );
                break;

            case 2: // top edge
                spawnScreenPos = new Vector3(
                    Random.Range(0, Screen.width),
                    Screen.height, // top side
                    Camera.main.nearClipPlane + 10f
                );
                break;

            case 3: // bottom edge
                spawnScreenPos = new Vector3(
                    Random.Range(0, Screen.width),
                    0, // bottom side
                    Camera.main.nearClipPlane + 10f
                );
                break;
        }
        // Convert to world space
        Vector3 spawnWorldPos = Camera.main.ScreenToWorldPoint(spawnScreenPos);
        GameItem obj = Instantiate(item, spawnWorldPos, Quaternion.identity);

        // Tag with variant index if given
        if (variantIndex >= 0)
        {
            var tag = obj.GetComponent<VariantIndexTag>();
            if (!tag) tag = obj.gameObject.AddComponent<VariantIndexTag>();
            tag.index = variantIndex; // 0,1,2 for GREEN list variants
        }

        if (pulseOnSpawn)
        {
            if (!obj.GetComponent<PulseOnSpawn>()) obj.gameObject.AddComponent<PulseOnSpawn>();
        }

        // Disable gravity
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Target: world position of screen center
        Vector3 centerWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane + 10f)
        );

        // Direction toward center
        Vector3 dir = (centerWorld - spawnWorldPos).normalized;

        // Add random variation
        dir = Quaternion.Euler(
            Random.Range(-randomAngle, randomAngle),
            Random.Range(-randomAngle, randomAngle),
            Random.Range(-randomAngle, randomAngle)
        ) * dir;

        // Apply velocity
        rb.linearVelocity = dir * speed * Random.Range(0.65f, 1.35f);
    }

    public void AddToCollected(int type, GameItem item)
    {
        collectedLists[type].Add(item);

        //Check if there is a completed set
        CheckCompletedSet(type);
    }

    public void CheckCompletedSet(int type)
    {
        var list = collectedLists[type];

        switch (type)
        {
            case (int)TYPE.BLUE:
            case (int)TYPE.RED:
                if (list.Count >= 3) // allow >3 in case extra got added
                    MergeFirstTwoIntoThirdAndSpawn(type);
                break;

            case (int)TYPE.GREEN:
                if (CanMergeGreen(list))
                    MergeGreen(list);
                break;
        }
    }


    private Tween TweenMoveAndShrink(GameItem item, Vector3 targetPos)
    {
        var t = item.transform;

        // stop any pulses on this object so tweens don't fight
        t.DOKill(false);

        var rb = item.GetComponent<Rigidbody>();
        var col = item.GetComponent<Collider>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero; // Unity 6
        }
        if (col != null) col.enabled = false;

        float dist = Vector3.Distance(t.position, targetPos);
        float dur = Mathf.Clamp(dist / Mathf.Max(mergeUnitsPerSecond, 0.001f), mergeMinDuration, mergeMaxDuration);

        return DOTween.Sequence()
            .Append(t.DOMove(targetPos, dur).SetEase(mergeMoveEase))
            .Append(t.DOScale(Vector3.zero, 0.2f).SetEase(mergeScaleEase))
            .OnComplete(() =>
            {
                if (item != null) Destroy(item.gameObject);
            });
    }

    private void MergeFirstTwoIntoThirdAndSpawn(int type)
    {
        var list = collectedLists[type];
        if (list.Count < 3) return;

        GameItem target = list[2];
        if (target == null) return;

        Transform targetT = target.transform;
        Vector3 targetPos = targetT.position;
        Quaternion targetRot = targetT.rotation;

        // Kick off the two merge tweens (only if they exist)
        var seq = DOTween.Sequence();
        if (list[0] != null) 
            seq.Join(TweenMoveAndShrink(list[0], targetPos));
        if (list[1] != null) 
            seq.Join(TweenMoveAndShrink(list[1], targetPos));

        // When both have finished merging:
        seq.OnComplete(() =>
        {
            // Optional: quick feedback on the target before it becomes the full antibody
            DOTween.Sequence()
                .Append(targetT.DOScale(1.25f, 0.12f))
                .Append(targetT.DOScale(0f, 0.18f))
                .OnComplete(() =>
                {
                    if (target != null) 
                        Destroy(target.gameObject);

                    // Spawn the full antibody exactly where the 3rd was
                    SpawnFullAntiBodyAt(type, targetPos, targetRot);

                    // Score / cleanup
                    UpdateScore(10, targetPos);
                    list.Clear();
                });
        });
    }

    private void SpawnFullAntiBodyAt(int type, Vector3 pos, Quaternion rot)
    {
        var prefab = fullAntiBodyList != null && type >= 0 && type < fullAntiBodyList.Count
            ? fullAntiBodyList[type]
            : null;
        if (prefab == null) return;

        var spawned = Instantiate(prefab, pos, rot);
        var st = spawned.transform;

        // Pop-in animation
        st.localScale = Vector3.zero;
        st.DOScale(0.65f, 0.25f).SetEase(Ease.OutBack);

        // Initialize homing if the component exists on the prefab
        var homing = spawned.GetComponent<FullAntiBodyObject>();
        if (homing != null)
        {
            homing.Init(
                owner: this,
                seekSpeed: fullSeekSpeed,
                turnRate: fullTurnRate,
                detectRadius: fullDetectRadius,
                lifetime: fullLifetime,
                killScore: fullKillScore,
                virusMask: virusLayerMask,
                targetType: (TYPE)type
            );
            AudioManager.Instance?.PlaySFX("Antibody Fly");
        }

        AudioManager.Instance?.PlaySFX("Antibody Assembled");

    }

    // Call this from your Replay button
    public void ReplayGame()
    {
        // 1) Stop any running tweens/effects
        pulseTween?.Kill();
        vignetteTween?.Kill();
        timerText.transform.DOKill(false);

        // 2) Reset UI panels
        startPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gamePanel.SetActive(true);

        // 3) Reset flags & timers
        gameOver = false;
        gameStart = true;
        warningStarted = false;

        spawnVirusTimer = 0f;
        spawnAnitBodyTimer = 0f;
        currentVirusIndex = 0;
        currentAntiBodyIndex = 0;

        gameTimer = initialGameTimer;

        // 4) Reset score & UI text
        score = 0;
        scoreText.text = "Score: 0";

        // timer text visuals back to normal
        timerText.color = Color.white;
        timerText.transform.localScale = Vector3.one;

        // 5) Clear collected sets
        for (int i = 0; i < collectedLists.Count; i++)
            collectedLists[i].Clear();

        // 6) Remove any remaining spawned items in the scene
        DestroyAllSpawnedGameItems();

        // 7) Reset vignette baseline (turn it OFF)
        if (vignette != null)
        {
            vignetteTween?.Kill();
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;  // NOT vigMin
            vignette.active = false;        // keep disabled until warning time
        }

        // 8) Restart the gentle pulse
        StartNormalPulse();

        // 9) Clear any existing Game Over sequence
        gameOverSeq?.Kill();
        goodJobPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        goodJobRT.anchoredPosition = goodJobTargetPos;
        gameOverRT.anchoredPosition = gameOverTargetPos;

        // 10) Replay Music
        AudioManager.Instance?.PlayBackgroundMusic("Game BGM");
    }

    // Helper to nuke all GameItem instances currently spawned in the scene
    void DestroyAllSpawnedGameItems()
    {
        var items = Object.FindObjectsByType<GameItem>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            // Skip if this object is one of your prefab references (those won’t be in scene anyway)
            if (items[i] != null)
                Destroy(items[i].gameObject);
        }
    }

    public void ShrinkFadeAndDestroy(GameObject go, float duration = 0.35f)
    {
        if (!go) 
            return;

        // stop any existing tweens on this object
        var t = go.transform;
        t.DOKill(false);

        // fade all instance materials and scale down together
        var seq = DOTween.Sequence();
        var renderers = go.GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (!r) continue;
            // use instance materials so other objects aren't affected
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;

                // URP Lit uses _BaseColor, fallback to _Color if needed
                var prop = m.HasProperty("_BaseColor") ? "_BaseColor" :
                           m.HasProperty("_Color") ? "_Color" : null;
                if (prop == null) continue;

                // ensure material is transparent so alpha is visible (URP Lit)
                if (m.HasProperty("_Surface"))
                {
                    m.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }

                var c = m.GetColor(prop);
                var to = new Color(c.r, c.g, c.b, 0f);
                seq.Join(m.DOColor(to, prop, duration));
            }
        }

        seq.Join(t.DOScale(0f, duration).SetEase(Ease.InQuad))
           .OnComplete(() => { if (go) Destroy(go); });
    }

    private bool CanMergeGreen(List<GameItem> list)
    {
        if (list == null || list.Count < 3) return false;

        int count0 = 0, count1 = 0, count2 = 0;

        for (int i = 0; i < list.Count; i++)
        {
            var gi = list[i];
            if (!gi) continue;
            var tag = gi.GetComponent<VariantIndexTag>();
            if (!tag) continue;

            switch (tag.index)
            {
                case 0: count0++; break;
                case 1: count1++; break;
                case 2: count2++; break;
            }
        }

        // Must have at least one '2'
        if (count2 < 1) return false;

        // Valid if: (0+1+2) OR (0+0+2) OR (1+1+2)
        return (count0 >= 1 && count1 >= 1) || (count0 >= 2) || (count1 >= 2);
    }

    private void MergeGreen(List<GameItem> list)
    {
        if (list == null) return;

        // pick target = the first variant-2 present
        GameItem target = null;
        int targetIdx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            var gi = list[i];
            if (!gi) continue;
            var tag = gi.GetComponent<VariantIndexTag>();
            if (tag != null && tag.index == 2)
            {
                target = gi;
                targetIdx = i;
                break;
            }
        }
        if (!target) return;

        // Collect available indices for 0s and 1s (excluding chosen target)
        List<int> zeroIdx = new();
        List<int> oneIdx = new();

        for (int i = 0; i < list.Count; i++)
        {
            if (i == targetIdx) continue;
            var gi = list[i];
            if (!gi) continue;
            var tag = gi.GetComponent<VariantIndexTag>();
            if (tag == null) continue;

            if (tag.index == 0) zeroIdx.Add(i);
            else if (tag.index == 1) oneIdx.Add(i);
        }

        // Decide which pair to merge into the target (priority: one-of-each, else two 0s, else two 1s)
        // This matches the allowed sets: (0+1+2) OR (0+0+2) OR (1+1+2)
        int a = -1, b = -1;
        if (zeroIdx.Count >= 1 && oneIdx.Count >= 1)
        {
            a = zeroIdx[0];
            b = oneIdx[0];
        }
        else if (zeroIdx.Count >= 2)
        {
            a = zeroIdx[0];
            b = zeroIdx[1];
        }
        else if (oneIdx.Count >= 2)
        {
            a = oneIdx[0];
            b = oneIdx[1];
        }
        else
        {
            // Not actually mergable right now
            return;
        }

        // Cache refs because we'll remove them later from 'list'
        GameItem A = list[a];
        GameItem B = list[b];

        Transform targetT = target.transform;
        Vector3 targetPos = targetT.position;
        Quaternion targetRot = targetT.rotation;

        var seq = DOTween.Sequence();
        if (A) seq.Join(TweenMoveAndShrink(A, targetPos));
        if (B) seq.Join(TweenMoveAndShrink(B, targetPos));

        seq.OnComplete(() =>
        {
            // little pop on the target, then replace with full antibody
            DOTween.Sequence()
                .Append(targetT.DOScale(1.25f, 0.12f))
                .Append(targetT.DOScale(0f, 0.18f))
                .OnComplete(() =>
                {
                    if (target) Destroy(target.gameObject);

                    // Spawn full green at the target's spot
                    SpawnFullAntiBodyAt((int)TYPE.GREEN, targetPos, targetRot);

                    UpdateScore(10, targetPos);

                    // Remove ONLY the used items from the collected list (if still present)
                    // Use reference removal to avoid index drift issues
                    list.Remove(A);
                    list.Remove(B);
                    list.Remove(target);

                    // Also purge any nulls that may have resulted from Destroy()
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (!list[i]) list.RemoveAt(i);
                    }
                });
        });
    }

    public int GetScore()
    {
        return score;
    }

    Vector2 OffscreenBottom(RectTransform rt, Vector2 target)
    {
        // Use the nearest Canvas rect height to move it fully below the view.
        var canvasRT = rt.GetComponentInParent<Canvas>().transform as RectTransform;
        float h = canvasRT ? canvasRT.rect.height : Screen.height;
        return target + new Vector2(0f, -h);
    }
}

public class VariantIndexTag : MonoBehaviour
{
    public int index; // 0, 1, or 2
}
