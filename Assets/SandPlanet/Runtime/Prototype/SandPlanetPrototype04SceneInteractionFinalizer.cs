using System.Reflection;
using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Ensures the new interaction-stage portrait/layering wins the final Canvas pass
    /// after the older dialogue layout has refreshed.
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public sealed class SandPlanetPrototype04SceneInteractionFinalizer : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private SandPlanetPrototype04SceneInteractionPolish polish;
        private MethodInfo refreshPortraitMethod;
        private MethodInfo enforcePresentationMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04SceneInteractionFinalizer>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04SceneInteractionFinalizer>();
        }

        private void Start()
        {
            polish = GetComponent<SandPlanetPrototype04SceneInteractionPolish>();
            if (polish == null)
            {
                enabled = false;
                return;
            }

            TypeInfo();
            Canvas.willRenderCanvases += FinalizeCanvas;
        }

        private void TypeInfo()
        {
            System.Type t = polish.GetType();
            refreshPortraitMethod = t.GetMethod("RefreshPortraitPresentation", PrivateInstance);
            enforcePresentationMethod = t.GetMethod("EnforcePresentation", PrivateInstance);
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= FinalizeCanvas;
        }

        private void FinalizeCanvas()
        {
            if (polish == null || !polish.enabled) return;
            try { refreshPortraitMethod?.Invoke(polish, null); } catch { }
            try { enforcePresentationMethod?.Invoke(polish, null); } catch { }
        }
    }
}
