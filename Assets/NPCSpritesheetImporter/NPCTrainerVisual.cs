
using UnityEngine;


[CreateAssetMenu(menuName = "Map/NPCTrainerVisual")]
public class NPCTrainerVisual : ScriptableObject {

    public string key;
    public string npc_name;
    public Sprite trainerSprite;
    public Sprite overworldTrainerSprite;
    public RuntimeAnimatorController animator;
    public bool usesDialogueColor;
    public Color dialogueFrameColor = Color.white;

    

   

}