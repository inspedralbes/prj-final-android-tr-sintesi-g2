using UnityEngine;

namespace DialogSystemTilin
{
    [CreateAssetMenu(fileName = "New Dialogue Character", menuName = "Scriptable Objects/Dialogue Character")]
    public class DialogueCharacterSO : ScriptableObject
    {
        [Header("Character Info:")]
        [SerializeField] private string characterName;
        [SerializeField] private Sprite profilePhoto;
        public string Name => characterName;
        public Sprite ProfilePhoto => profilePhoto;
    }
}
