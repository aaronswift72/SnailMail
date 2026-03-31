using UnityEngine;

public class MoonCycleManager : MonoBehaviour
{
    public static MoonCycleManager Instance;

    [Header("Moon")]
    public Transform moonObject;
    public Transform moonPivot;

    [Header("Orbit Settings")]
    public float orbitRadius = 500f;
    public float dayDuration = 300f; // 5 minutes per day

    [Header("Phase Settings")]
    public int totalDaysInCycle = 9;
    public int currentDay = 0;

    // How much to rotate the moon mesh per day to show phase change
    // 360 / 9 days = 40 degrees per day
    public float phaseRotationPerDay = 40f;

    private float timeOfDay = 0.55f;
    private int currentPhaseIndex = 0;

    public bool IsFullMoon => currentPhaseIndex == 4;
    public bool IsNewMoon => currentPhaseIndex == 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentPhaseIndex = currentDay % totalDaysInCycle;
        ApplyPhaseRotation();
    }

    void Update()
    {
        OrbitMoon();
        BillboardToCamera();
    }

    void OrbitMoon()
    {
        timeOfDay += Time.deltaTime / dayDuration;

        if (timeOfDay >= 1f)
        {
            timeOfDay = 0f;
            AdvanceDay();
        }

        // Full 360 degree orbit
        float angle = timeOfDay * 360f;
        float radians = angle * Mathf.Deg2Rad;

        if (moonPivot != null)
        {
            moonPivot.position = new Vector3(
                Mathf.Cos(radians) * orbitRadius,
                Mathf.Sin(radians) * orbitRadius,
                0f
            );
        }
    }

    void BillboardToCamera()
    {
        if (moonObject != null && Camera.main != null)
        {
            // Always face the camera
            Vector3 directionToCamera = 
                Camera.main.transform.position - moonObject.position;
            
            if (directionToCamera != Vector3.zero)
            {
                moonObject.rotation = Quaternion.LookRotation(-directionToCamera);
                
                // Apply phase rotation on top of billboard
                // This rotates the moon face to show current phase
                moonObject.rotation *= Quaternion.Euler(
                    0f, 
                    0f, 
                    currentPhaseIndex * phaseRotationPerDay
                );
            }
        }
    }

    void AdvanceDay()
    {
        currentDay++;
        currentPhaseIndex = currentDay % totalDaysInCycle;
        ApplyPhaseRotation();

        Debug.Log($"Day {currentDay} - Phase Index: {currentPhaseIndex} - {GetCurrentPhaseName()}");

        if (IsFullMoon) OnFullMoon();
        if (IsNewMoon) OnNewMoon();
    }

    void ApplyPhaseRotation()
    {
        // Called when day changes to update phase
        // Actual rotation applied in BillboardToCamera every frame
    }

    void OnFullMoon()
    {
        Debug.Log("Full Moon! Garden party at Grand Vizer's!");
    }

    void OnNewMoon()
    {
        Debug.Log("New Moon!");
    }

    public void PlayerSlept()
    {
        AdvanceDay();
    }

    public bool IsGardenPartyActive()
    {
        return IsFullMoon;
    }

    public int DaysUntilFullMoon()
    {
        int daysUntil = (totalDaysInCycle - currentPhaseIndex) % totalDaysInCycle;
        return daysUntil == 0 ? totalDaysInCycle : daysUntil;
    }

    public string GetCurrentPhaseName()
    {
        string[] phaseNames = {
            "New Moon",
            "Waxing Crescent",
            "Waxing Quarter",
            "Waxing Gibbous",
            "Full Moon",
            "Waning Gibbous",
            "Waning Quarter",
            "Waning Crescent",
            "Half Moon"
        };

        if (currentPhaseIndex < phaseNames.Length)
            return phaseNames[currentPhaseIndex];

        return "Unknown";
    }
    public float GetTimeOfDay()
    {
        return timeOfDay;
    }
}
