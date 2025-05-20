using UnityEngine;

namespace DialogSystemTilin
{
    [System.Serializable]
    public class DialogueTurn
    {
        [field: SerializeField]
        public DialogueCharacterSO Character { get; private set; }

        [SerializeField, TextArea(2, 4)]
        private string dialogueLine = string.Empty;

        public string DialogueLine => dialogueLine;
    }
}
