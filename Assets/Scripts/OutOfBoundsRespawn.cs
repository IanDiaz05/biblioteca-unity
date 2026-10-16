using System.Collections;
using UnityEngine;

public class OutOfBoundsRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Punto de reaparicion. Si es null se usa (0, 1, 0).")]
    public Transform spawnPoint;

    [Header("UI WASTED")]
    [Tooltip("CanvasGroup del panel WASTED (Alpha inicial 0).")]
    public CanvasGroup wastedCanvasGroup;

    [Header("Tiempos")]
    [Tooltip("Tiempo de animacion del fade (ej. 1.0s).")]
    public float fadeDuration = 1.0f;
    [Tooltip("Tiempo que permanece en pantalla el WASTED (ej. 1.5s).")]
    public float displayDuration = 1.5f;

    private static readonly Vector3 DefaultSpawnPosition = new Vector3(0f, 1f, 0f);
    private bool isRespawning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other.gameObject))
            return;
        TryRespawn(other.gameObject);
    }

    public bool IsPlayer(GameObject go)
    {
        if (go == null) return false;
        if (go.CompareTag("Player")) return true;
        if (go.name == "Player_CJ") return true;
        Transform t = go.transform.parent;
        while (t != null)
        {
            if (t.CompareTag("Player") || t.name == "Player_CJ") return true;
            t = t.parent;
        }
        return false;
    }

    public void TryRespawn(GameObject player)
    {
        if (isRespawning || player == null) return;
        StartCoroutine(RespawnRoutine(player));
    }

    public void HandleBoundaryEnter(GameObject player)
    {
        TryRespawn(player);
    }

    private IEnumerator RespawnRoutine(GameObject player)
    {
        isRespawning = true;

        // 1. Bloquear control de movimiento
        MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();
        System.Collections.Generic.List<MonoBehaviour> disabledControls = new System.Collections.Generic.List<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b == null || !b.enabled) continue;
            string typeName = b.GetType().Name;
            if (typeName == "SimpleFPSController" || typeName.Contains("Controller") && typeName != "CharacterController")
            {
                // SimpleFPSController u otro script de control personalizado
                if (b.GetType().Name == "SimpleFPSController")
                {
                    b.enabled = false;
                    disabledControls.Add(b);
                }
            }
        }
        // Desactivar CharacterController temporalmente para teleport seguro
        CharacterController cc = player.GetComponent<CharacterController>();
        // Tambien buscar en hijos/padres por si acaso
        if (cc == null) cc = player.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = player.GetComponentInParent<CharacterController>();

        // 2. Fade In (Alpha 0 -> 1)
        if (wastedCanvasGroup != null)
        {
            wastedCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvas(wastedCanvasGroup, 0f, 1f, fadeDuration));
            wastedCanvasGroup.blocksRaycasts = true;
        }

        // 3. Teletransportar a spawnPoint
        Vector3 targetPos = DefaultSpawnPosition;
        Quaternion targetRot = Quaternion.identity;
        bool hasSpawn = spawnPoint != null;
        if (hasSpawn)
        {
            targetPos = spawnPoint.position;
            targetRot = spawnPoint.rotation;
        }

        bool ccWasEnabled = false;
        if (cc != null)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        player.transform.position = targetPos;
        player.transform.rotation = targetRot;

        // Resetear velocidad si tiene Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Pequenyo wait para asegurar fisica
        yield return null;

        if (cc != null && ccWasEnabled)
            cc.enabled = true;
        else if (cc != null && !ccWasEnabled)
        {
            // Si estaba desactivado antes, lo reactivamos igualmente para dejar control funcional
            // salvo que el objeto este en estado invalido
            cc.enabled = true;
        }

        // 4. Mantener WASTED en pantalla
        yield return new WaitForSecondsRealtime(displayDuration);

        // 5. Fade Out (Alpha 1 -> 0) y devolver control
        if (wastedCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvas(wastedCanvasGroup, 1f, 0f, fadeDuration));
            wastedCanvasGroup.blocksRaycasts = false;
        }

        foreach (var b in disabledControls)
        {
            if (b != null) b.enabled = true;
        }

        isRespawning = false;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}

// Reenviador para triggers hijos de Environment_Boundaries.
// Anade este componente a cada pared/suelo trigger hijo para que
// el evento llegue al gestor OutOfBoundsRespawn del padre.
public class OutOfBoundsTriggerForwarder : MonoBehaviour
{
    private OutOfBoundsRespawn manager;

    private void Awake()
    {
        manager = GetComponentInParent<OutOfBoundsRespawn>();
        if (manager == null)
            manager = Object.FindFirstObjectByType<OutOfBoundsRespawn>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (manager == null)
        {
            manager = GetComponentInParent<OutOfBoundsRespawn>();
            if (manager == null)
                manager = Object.FindFirstObjectByType<OutOfBoundsRespawn>();
            if (manager == null) return;
        }
        if (manager.IsPlayer(other.gameObject))
            manager.HandleBoundaryEnter(other.gameObject);
    }
}
