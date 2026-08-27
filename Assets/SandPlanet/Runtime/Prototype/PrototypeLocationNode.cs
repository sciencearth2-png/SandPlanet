using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Thin world-view component. The 3D hub owns no gameplay rules;
    /// it only identifies which location was clicked.
    /// </summary>
    public sealed class PrototypeLocationNode : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private string displayName;

        public string LocationId => locationId;
        public string DisplayName => displayName;

        public void Configure(string id, string label)
        {
            locationId = id;
            displayName = label;
            gameObject.name = $"Location_{id}";
        }
    }
}
