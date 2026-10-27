using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>
    /// The two clashing mindsets a level is about, how each world looks,
    /// and which one the rift chooses when you step through it.
    /// </summary>
    [CreateAssetMenu(menuName = "RIFT/Mindset Theme", fileName = "Mindset_")]
    public class MindsetTheme : ScriptableObject
    {
        [Header("The clash")]
        public string emberMindset = "DOUBT";
        public string staticMindset = "TRUST";
        [Tooltip("The mindset the rift resolves to at the end of the level.")]
        public RiftWorld healthyWorld = RiftWorld.Static;

        [Header("Look")]
        public Material emberMaterial;
        public Material staticMaterial;
        public Material backgroundMaterial;
        [ColorUsage(true, true)] public Color emberTextColor = new(1.3f, 1.1f, 0.9f, 1f);
        [ColorUsage(true, true)] public Color staticTextColor = new(0.4f, 1.8f, 2.2f, 1f);
        [Tooltip("Signature colour of this level: checkpoints, the rift gate, the soul's light.")]
        [ColorUsage(true, true)] public Color accentColor = new(2.2f, 1.2f, 0.5f, 1f);
        [Tooltip("Glowing TMP material for each voice's wall text.")]
        public Material emberTextMaterial;
        public Material staticTextMaterial;

        [Header("Resolution")]
        public string resolutionTitle = "TRUST";
        [TextArea] public string resolutionLine = "you couldn't see the ground. you stepped anyway.";

        public Color TextColorFor(RiftWorld voice) => voice == RiftWorld.Ember ? emberTextColor : staticTextColor;
        public Material TextMaterialFor(RiftWorld voice) => voice == RiftWorld.Ember ? emberTextMaterial : staticTextMaterial;
    }
}
