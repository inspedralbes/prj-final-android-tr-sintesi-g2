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

        private bool isTyping = false;
        private Coroutine typingCoroutine;
        private string currentFullLine = "";
        private int currentLetterIndex = 0;
        
        // Variable para controlar si se ha completado una línea y está esperando input
        private bool waitingForInput = false;

        private void Awake()
        {
            Instance = this;
            dialogueUI.HideDialogBox();
        }
        
      private void Update()
{
    if (IsDialogInProgress && Input.GetKeyDown(KeyCode.E))
    {
        SkipOrNextLine();
    }
}

        public void StartDialogue(DialogueRoundSO dialogue)
        {
            if (IsDialogInProgress)
            {
                Debug.LogWarning($"Dialogue already in progress");
                return;
            }

            IsDialogInProgress = true;
            dialogueTurnsQueue = new Queue<DialogueTurn>(dialogue.DialogueTurnsList);
            StartCoroutine(DialogueCoroutine());
        }

        private IEnumerator DialogueCoroutine()
        {
            dialogueUI.ShowDialogBox();

            while (dialogueTurnsQueue.Count > 0)
            {
                var currentTurn = dialogueTurnsQueue.Dequeue();
                dialogueUI.SetCharacterInfo(currentTurn.Character);
                dialogueUI.ClearDialogArea();

                currentFullLine = currentTurn.DialogueLine;
                currentLetterIndex = 0;
                waitingForInput = false;

                typingCoroutine = StartCoroutine(TypeSentence(currentFullLine));

                // Espera a que termine el tipeo
                yield return new WaitUntil(() => !isTyping);
                
                // Marca que estamos esperando input
                waitingForInput = true;
                
                // Espera a que se haga clic para continuar (ahora esto es manejado por SkipOrNextLine)
                yield return new WaitUntil(() => !waitingForInput);
            }

            dialogueUI.HideDialogBox();
            IsDialogInProgress = false;
        }

        private IEnumerator TypeSentence(string line)
        {
            isTyping = true;

            while (currentLetterIndex < line.Length)
            {
                char letter = line[currentLetterIndex];
                dialogueUI.AppendToDialogArea(letter);
                if (!char.IsWhiteSpace(letter)) typingAudioSource.Play();
                currentLetterIndex++;
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
        }

        public void SkipOrNextLine()
        {
            if (!IsDialogInProgress) return;

            if (isTyping)
            {
                // Mostrar la línea entera instantáneamente
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }

                dialogueUI.SetDialogText(currentFullLine);
                isTyping = false;
            }
            else if (waitingForInput)
            {
                // Si ya terminamos de mostrar el texto y estamos esperando input
                // marcamos que ya recibimos el input para avanzar al siguiente diálogo
                waitingForInput = false;
            }
        }
    }
}