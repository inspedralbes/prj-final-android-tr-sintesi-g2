using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace DialogSystemTilin
{
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private AudioSource typingAudioSource;

        public static DialogueManager Instance { get; private set; }
        private Queue<DialogueTurn> dialogueTurnsQueue;
        public bool IsDialogInProgress { get; private set; } = false;

        private void Awake()
        {
            Instance = this;
            dialogueUI.HideDialogBox();
        }
        public void StartDialogue(DialogueRoundSO dialogue)
        {
            if (IsDialogInProgress)
            {
                Debug.LogWarning($"Dialogue already in progress");
                return;
            }
            IsDialogInProgress= true;
            dialogueTurnsQueue = new Queue<DialogueTurn>(dialogue.DialogueTurnsList);
            StartCoroutine(DialogueCoroutine());
        }
        private IEnumerator DialogueCoroutine()
        {
            dialogueUI.ShowDialogBox();    
            while (dialogueTurnsQueue.Count > 0) 
            {
                var CurrentTurn = dialogueTurnsQueue.Dequeue();
                dialogueUI.SetCharacterInfo(CurrentTurn.Character);
                dialogueUI.ClearDialogArea();
                yield return StartCoroutine(TypeSentence(CurrentTurn));

                yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return));
                yield return null;
            }
            dialogueUI.HideDialogBox();
            IsDialogInProgress= false;

        }
        private IEnumerator TypeSentence(DialogueTurn dialogTurn)
        {
            var typingWaitSeconds = new WaitForSeconds(typingSpeed);
            
            foreach (char letter in dialogTurn.DialogueLine.ToCharArray())
            {
                dialogueUI.AppendToDialogArea(letter);
                if (!char.IsWhiteSpace(letter)) typingAudioSource.Play();
                yield return typingWaitSeconds;
            }
        }
    }
}
