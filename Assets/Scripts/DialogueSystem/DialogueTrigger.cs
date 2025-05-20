using UnityEngine;

namespace DialogSystemTilin
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueRoundSO dialogue;
        private bool isPlayerNearby = false; // Verifica si el jugador está cerca

        [ContextMenu("Trigger Dialogue")]
        public void TriggerDialogue()
        {
            DialogueManager.Instance.StartDialogue(dialogue);
        }

        private void Update()
        {
            // Detectar si el jugador está cerca y presiona la tecla "E"
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
            {
                TriggerDialogue();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                isPlayerNearby = true; // El jugador está cerca
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                isPlayerNearby = false; // El jugador se alejó
            }
        }
    }
}