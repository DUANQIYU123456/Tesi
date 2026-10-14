using UnityEngine;

/// <summary>Fades the liquid emission and a small real light as the player approaches.</summary>
[DisallowMultipleComponent]
public sealed class ProximityPotionGlow : MonoBehaviour
{
    public Transform player;
    public Renderer liquidRenderer;
    public Light glowLight;
    [Min(0.1f)] public float activationDistance = 4f;
    [Min(0f)] public float fullGlowDistance = 1.4f;
    [Min(0.05f)] public float fadeSeconds = 0.7f;
    [ColorUsage(false, true)] public Color glowColor = new Color(0.12f, 1f, 0.025f);
    [Min(0f)] public float idleEmission = 0.08f;
    [Min(0f)] public float nearbyEmission = 3.5f;
    [Min(0f)] public float nearbyLightIntensity = 1.2f;
    [Min(0.1f)] public float lightRange = 3f;
    [Tooltip("Optional slow breathing; zero means steady light.")]
    [Range(0f, 0.15f)] public float breathingAmount = 0f;

    static readonly int Emission = Shader.PropertyToID("_EmissionStrength");
    static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    MaterialPropertyBlock properties;
    float glow;
    float nextPlayerSearch;

    void OnEnable()
    {
        properties = new MaterialPropertyBlock();
        glow = 0f;
        nextPlayerSearch = 0f;
        Apply(0f);
    }

    void Update()
    {
        // ROOM1's player may arrive after ROOM2's objects start. Retry after scene travel.
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + 0.5f;
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        float target = 0f;
        if (player != null && player.gameObject.activeInHierarchy && liquidRenderer != null)
        {
            Vector3 playerCenter = player.position;
            if (player.TryGetComponent<CharacterController>(out var controller))
                playerCenter = player.TransformPoint(controller.center);
            float distance = Vector3.Distance(playerCenter, liquidRenderer.bounds.ClosestPoint(playerCenter));
            float span = Mathf.Max(0.01f, activationDistance - fullGlowDistance);
            target = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((activationDistance - distance) / span));
        }

        glow = Mathf.MoveTowards(glow, target, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        float breath = 1f + Mathf.Sin(Time.time * 1.7f) * breathingAmount;
        Apply(glow * breath);
    }

    void Apply(float amount)
    {
        if (liquidRenderer != null)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            liquidRenderer.GetPropertyBlock(properties);
            properties.SetColor(GlowColor, glowColor);
            properties.SetFloat(Emission, idleEmission + (nearbyEmission - idleEmission) * amount);
            liquidRenderer.SetPropertyBlock(properties);
        }
        if (glowLight != null)
        {
            glowLight.color = glowColor;
            glowLight.range = lightRange;
            glowLight.intensity = nearbyLightIntensity * amount;
            glowLight.enabled = amount > 0.005f;
        }
    }

    void OnDisable() => Apply(0f);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.15f, 1f, 0.05f, 0.5f);
        Gizmos.DrawWireSphere(liquidRenderer != null ? liquidRenderer.bounds.center : transform.position,
            activationDistance);
    }
}
