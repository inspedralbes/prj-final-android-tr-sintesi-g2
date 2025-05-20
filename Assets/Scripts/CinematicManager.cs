using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Importa TextMeshPro
using UnityEngine.SceneManagement;

public class CinematicManager : MonoBehaviour
{
    [Header("Cinematic Elements")]
    public Image backgroundImage;       // Imagen de fondo
    public TMP_Text dialogueText;       // Texto del diálogo (TextMeshPro)
    public Image borderImage;           // Imagen del borde
    public AudioClip typingSound;       // Sonido al escribir
    public AudioClip backgroundMusic;   // Música de fondo
    public Sprite[] backgrounds;        // Fondos para cambiar
    [TextArea(3, 5)] public string[] dialogues; // Textos para cada fondo

    [Header("Configuración de Escena")]
    [SerializeField] private string sceneToLoad = "NombreDeLaEscena"; // Nombre de la escena a cargar

    private int currentIndex = 0;
    private AudioSource audioSource;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        ShowCurrentDialogue();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Si se hace clic
        {
            if (isTyping)
            {
                // Completa el texto si está escribiéndose
                CompleteText();
            }
            else
            {
                currentIndex++;
                if (currentIndex < backgrounds.Length)
                {
                    ShowCurrentDialogue();
                }
                else
                {
                    EndCinematic();
                }
            }
        }
        else if (Input.anyKeyDown) // Si se presiona cualquier tecla
        {
            if (currentIndex >= backgrounds.Length)
            {
                EndCinematic();
            }
        }
    }

    void ShowCurrentDialogue()
    {
        backgroundImage.sprite = backgrounds[currentIndex];
        dialogueText.text = ""; // Limpiar el texto actual

        // Mostrar/ocultar la imagen del borde
        borderImage.gameObject.SetActive(currentIndex < backgrounds.Length - 1);

        // Iniciar la escritura del texto
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            if (typingSound != null)
            {
                audioSource.PlayOneShot(typingSound);
            }
            yield return new WaitForSeconds(0.05f); // Ajusta la velocidad de escritura
        }
        isTyping = false;
    }

    void CompleteText()
    {
        // Detiene la escritura y muestra todo el texto de golpe
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        dialogueText.text = dialogues[currentIndex];
        isTyping = false;
    }

    void EndCinematic()
    {
        Debug.Log("Cinemática terminada.");
        
        // Verificar si se ha configurado un nombre de escena
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad); // Cargar la escena especificada
        }
        else
        {
            Debug.LogError("No se ha especificado una escena para cargar.");
        }
    }
}