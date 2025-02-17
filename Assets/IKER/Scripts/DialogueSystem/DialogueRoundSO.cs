using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace DialogSystemTilin
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Scriptable Objects/ Dialogue Round")]
    public class DialogueRoundSO : ScriptableObject
    {
        [SerializeField] private List<DialogueTurn> dialogueTurnsList;
        public List<DialogueTurn> DialogueTurnsList => dialogueTurnsList;
    }
}
