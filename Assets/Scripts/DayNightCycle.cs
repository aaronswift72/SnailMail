using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Sun Object")]
    public Transform sunPivot;
    public Light sunLight;
    public Transform sunVisual;
    public float sunOrbitRadius = 500f;

    [Header("Sky Colors")]
    public Color daySkyColor = new Color(0.4f, 0.75f, 1f);
    public Color sunriseColor = new Color(1f, 0.6f, 0.3f);
    public Color sunsetColor = new Color(0.9f, 0.4f, 0.2f);
    public Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);

    [Header("Sun Light Colors")]
    public Color daySunColor = new Color(1f, 0.95f, 0.8f);
    public Color sunriseSunColor = new Color(1f, 0.6f, 0.3f);
    public Color sunsetSunColor = new Color(1f, 0.4f, 0.1f);

    [Header("Ambient Light")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f);

    [Header("Sun Light Intensity")]
    public float maxSunIntensity = 1.2f;

    private MoonCycleManager moonManager;
    private Camera mainCamera;

    void Start()
    {
        moonManager = MoonCycleManager.Instance;
        mainCamera = Camera.main;

        // Set camera to solid color sky
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    void Update()
    {
        if (moonManager == null) return;

        // Sun is always opposite the moon
        float sunTime = (moonManager.GetTimeOfDay() + 0.5f) % 1f;

        MoveSun(sunTime);
        UpdateSkyColor(sunTime);
        UpdateSunLight(sunTime);
        UpdateAmbientLight(sunTime);
    }

    void MoveSun(float sunTime)
    {
        if (sunPivot == null) return;

        float angle = sunTime * 360f;
        float radians = angle * Mathf.Deg2Rad;

        // Move sun pivot in full 360 orbit opposite moon
        sunPivot.position = new Vector3(
            Mathf.Cos(radians) * sunOrbitRadius,
            Mathf.Sin(radians) * sunOrbitRadius,
            0f
        );

        // Point directional light toward scene center
        if (sunLight != null)
        {
            sunLight.transform.LookAt(Vector3.zero);
        }
    }

    void UpdateSkyColor(float sunTime)
    {
        if (mainCamera == null) return;

        Color skyColor;

        if (sunTime < 0.2f)
        {
            // Night to sunrise
            float t = sunTime / 0.2f;
            skyColor = Color.Lerp(nightSkyColor, sunriseColor, t);
        }
        else if (sunTime < 0.3f)
        {
            // Sunrise to full day
            float t = (sunTime - 0.2f) / 0.1f;
            skyColor = Color.Lerp(sunriseColor, daySkyColor, t);
        }
        else if (sunTime < 0.7f)
        {
            // Full day
            skyColor = daySkyColor;
        }
        else if (sunTime < 0.8f)
        {
            // Day to sunset
            float t = (sunTime - 0.7f) / 0.1f;
            skyColor = Color.Lerp(daySkyColor, sunsetColor, t);
        }
        else
        {
            // Sunset to night
            float t = (sunTime - 0.8f) / 0.2f;
            skyColor = Color.Lerp(sunsetColor, nightSkyColor, t);
        }

        mainCamera.backgroundColor = skyColor;
        RenderSettings.ambientSkyColor = skyColor;
    }

    void UpdateSunLight(float sunTime)
    {
        if (sunLight == null) return;

        float angle = sunTime * 360f;
        float radians = angle * Mathf.Deg2Rad;
        float sunHeight = Mathf.Sin(radians);

        // Only light when sun is above horizon
        if (sunHeight <= 0f)
        {
            sunLight.intensity = 0f;
            return;
        }

        // Intensity based on sun height
        sunLight.intensity = Mathf.Lerp(0f, maxSunIntensity, sunHeight);

        // Color based on time of day
        if (sunTime < 0.25f)
        {
            // Sunrise color
            sunLight.color = sunriseSunColor;
        }
        else if (sunTime < 0.35f)
        {
            // Sunrise to white day
            float t = (sunTime - 0.25f) / 0.1f;
            sunLight.color = Color.Lerp(sunriseSunColor, daySunColor, t);
        }
        else if (sunTime < 0.65f)
        {
            // Full day white light
            sunLight.color = daySunColor;
        }
        else if (sunTime < 0.75f)
        {
            // Day to sunset
            float t = (sunTime - 0.65f) / 0.1f;
            sunLight.color = Color.Lerp(daySunColor, sunsetSunColor, t);
        }
        else
        {
            // Sunset color
            sunLight.color = sunsetSunColor;
        }
    }

    void UpdateAmbientLight(float sunTime)
    {
        float angle = sunTime * 360f;
        float radians = angle * Mathf.Deg2Rad;
        float sunHeight = Mathf.Sin(radians);

        // Blend ambient light between day and night
        float ambientT = Mathf.Clamp01((sunHeight + 0.1f) / 0.5f);
        RenderSettings.ambientLight = Color.Lerp(
            nightAmbientColor,
            dayAmbientColor,
            ambientT
        );
    }
}
