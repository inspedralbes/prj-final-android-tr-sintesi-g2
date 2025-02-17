using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogSystemTilin
{
    public class DialogueUI : MonoBehaviour
    {

        // Dialogue UI
        [SerializeField] private RectTransform dialogBox;
        [SerializeField] private Image characterPhoto;
        [SerializeField] private TextMeshProUGUI characterName;
        [SerializeField] private TextMeshProUGUI dialogArea;
        public void ShowDialogBox()
        {
            dialogBox.gameObject.SetActive(true);
        }
        public void HideDialogBox()
        {
            dialogBox.gameObject.SetActive(false);
        }
        public void SetCharacterInfo(DialogueCharacterSO character)
        {
            if (character == null) return;
            characterPhoto.sprite = character.ProfilePhoto;
            characterName.text = character.Name;
        }
        public void ClearDialogArea()
        {
            dialogArea.text = string.Empty;
        }
        public void SetDialogArea(string text)
        {
            dialogArea.text = text; 
        }
        public void AppendToDialogArea(char letter)
        {
            dialogArea.text += letter;
        }
    }
}
